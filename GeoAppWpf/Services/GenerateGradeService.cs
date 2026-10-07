using GeoAppCore;
using GeoAppCore.Models;
using GeoAppCore.Services;
using GeoAppWpf.InputDalogBuilders;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;

namespace GeoAppWpf.Services
{
    internal class GenerateGradeService
    {
        private const double ReserveToleranceKg = 0.0001;
        private const double Epsilon = 1e-9;

        // --------------------------------------------------------
        // Параметры разброса содержаний (сигмы лог-нормальных множителей).
        // Чем больше сигма - тем сильнее различаются блоки / скважины / пробы.
        // --------------------------------------------------------

        /// <summary>Разброс между блоками (контурами).</summary>
        private const double BlockSigma = 0.45;

        /// <summary>Разброс между скважинами внутри блока.</summary>
        private const double BoreholeSigma = 0.35;

        /// <summary>Разброс между пробами внутри скважины.</summary>
        private const double SampleSigma = 0.55;

        /// <summary>Ограничение |z| у нормального шума, чтобы не было экстремальных выбросов.</summary>
        private const double MaxZ = 2.5;

        /// <summary>
        /// Если заданный запас нельзя набрать при содержаниях в [MinGrade, MaxGrade],
        /// разрешаем превышать MaxGrade (содержания масштабируются с сохранением разброса).
        /// При false - во всех пробах остаётся MaxGrade и пишется предупреждение.
        /// </summary>
        private const bool AllowExceedMaxGrade = true;

        public static void Generate(IEnumerable<BoreholeLine> lines, GenerateGradeProperties properties)
        {
            if (properties.OreReserve <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(properties.OreReserve),
                    "OreReserve должен быть больше 0.");

            if (properties.MinOreThickness <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(properties.MinOreThickness));

            if (properties.MaxOreThickness < properties.MinOreThickness)
                throw new ArgumentException(
                    "MaxOreThickness должен быть больше или равен MinOreThickness.");

            if (properties.MinGrade < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(properties.MinGrade));

            if (properties.MaxGrade < properties.MinGrade)
                throw new ArgumentException(
                    "MaxGrade должен быть больше или равен MinGrade.");

            if (properties.MaxRkpSamples < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(properties.MaxRkpSamples));

            var blocks = BlockBuilder.Build(lines, properties.Boundaries);

            // Скважина может входить в несколько контуров - берём каждую только один раз,
            // иначе мощность для неё выбиралась бы несколько раз и объединялась.
            var boreholes = blocks
                .SelectMany(x => x.Boreholes.Select(cbh => cbh.Source))
                .Distinct<Borehole>(ReferenceEqualityComparer.Instance)
                .ToList();

            // --------------------------------------------------------
            // 1. Очищаем старые содержания
            // --------------------------------------------------------

            ClearSamplesGradeInBoreholes(lines.SelectMany(x => x.Boreholes));

            var random = new Random();

            // --------------------------------------------------------
            // 2. Выбираем существующие пробы,
            //    которые потенциально станут рудными.
            // --------------------------------------------------------

            var selectedSamples = SelectOreSamples(
                boreholes,
                properties,
                random);

            if (selectedSamples.Count == 0)
            {
                Debug.WriteLine("Не удалось подобрать ни одной пробы для генерации.");
                return;
            }

            // --------------------------------------------------------
            // 3. Первоначально присваиваем случайные содержания.
            //    Лог-нормальное распределение с тремя уровнями разброса:
            //    блок x скважина x проба.
            // --------------------------------------------------------

            var randomGrades = GenerateInitialGrades(
                blocks,
                boreholes,
                selectedSamples,
                properties,
                random);

            foreach (var pair in randomGrades)
                pair.Key.Grade = pair.Value;

            // --------------------------------------------------------
            // 4. Проверяем получившийся запас.
            // --------------------------------------------------------

            double totalReserveKg = blocks.Sum(x => x.ReserveKg);

            Debug.WriteLine(
                $"Initial reserve = {totalReserveKg:F6} kg, " +
                $"target = {properties.OreReserve:F6} kg");

            // --------------------------------------------------------
            // 5. Подгоняем содержания под общий запас.
            // --------------------------------------------------------

            FitReserve(
                blocks,
                properties,
                selectedSamples,
                randomGrades);

            // --------------------------------------------------------
            // 6. Финальный пересчёт.
            // --------------------------------------------------------

            totalReserveKg = blocks.Sum(x => x.ReserveKg);

            Debug.WriteLine(
                $"Final reserve = {totalReserveKg:F6} kg, " +
                $"target = {properties.OreReserve:F6} kg, " +
                $"difference = {totalReserveKg - properties.OreReserve:F6} kg");

            Debug.WriteLine(
                $"Selected samples: {selectedSamples.Count}, " +
                $"grade range: {selectedSamples.Min(x => x.Grade):F4} .. {selectedSamples.Max(x => x.Grade):F4}");

            foreach (var block in blocks)
            {
                Debug.WriteLine(
                    $"Builded block: {block.Id}\n" +
                    $"TotalPureVertReserve: {block.TotalPureVertReserve}\n" +
                    $"TotalThickness: {block.TotalThickness}\n" +
                    $"AvgThickness: {block.AvgThickness}\n" +
                    $"PureAvgGrade: {block.PureAvgGrade}\n" +
                    $"Area: {block.Area}\n" +
                    $"Volume: {block.Volume}\n" +
                    $"GoldReserveG: {block.ReserveG}\n" +
                    $"GoldReserveKG: {block.ReserveKg}\n");
            }

            // Уникальные выбранные пробы (без дублей из пересекающихся контуров).
            Debug.WriteLine("\nGenerated samples:\n" +
                $"{string.Join("\n", selectedSamples.Select(x => x.Grade.ToString("F4")))}");
        }

        #region Initial grades

        /// <summary>
        /// Генерирует начальные содержания в диапазоне [MinGrade, MaxGrade].
        /// Содержание ~ множитель_блока * множитель_скважины * шум_пробы (все лог-нормальные),
        /// затем min-max нормировка в [0..1] и перевод в [MinGrade, MaxGrade].
        /// Распределение скошено: много низких значений, редкие богатые.
        /// </summary>
        private static Dictionary<Sample, double> GenerateInitialGrades(
            IReadOnlyList<Block> blocks,
            IReadOnlyList<Borehole> boreholes,
            IReadOnlyList<Sample> selectedSamples,
            GenerateGradeProperties properties,
            Random random)
        {
            // Множитель для каждого блока.
            var blockFactors = blocks
                .Select(_ => LogNormal(random, BlockSigma))
                .ToArray();

            // Для каждой скважины собираем множители блоков, в которые она попала.
            var boreholeBlockFactors = new Dictionary<Borehole, List<double>>(ReferenceEqualityComparer.Instance);

            for (int i = 0; i < blocks.Count; i++)
            {
                foreach (var cbh in blocks[i].Boreholes)
                {
                    if (!boreholeBlockFactors.TryGetValue(cbh.Source, out var list))
                    {
                        list = new List<double>();
                        boreholeBlockFactors[cbh.Source] = list;
                    }

                    list.Add(blockFactors[i]);
                }
            }

            // Итоговый множитель скважины = (среднее по её блокам) * собственный случайный.
            var boreholeFactors = new Dictionary<Borehole, double>(ReferenceEqualityComparer.Instance);

            foreach (var borehole in boreholes)
            {
                double blockPart = boreholeBlockFactors.TryGetValue(borehole, out var list) && list.Count > 0
                    ? list.Average()
                    : 1.0;

                boreholeFactors[borehole] = blockPart * LogNormal(random, BoreholeSigma);
            }

            // Какая выбранная проба в какой скважине лежит.
            var sampleOwner = new Dictionary<Sample, Borehole>(ReferenceEqualityComparer.Instance);

            foreach (var borehole in boreholes)
            {
                foreach (var sample in borehole.Samples)
                    sampleOwner[sample] = borehole;
            }

            // Сырые значения.
            var raw = new Dictionary<Sample, double>(ReferenceEqualityComparer.Instance);

            foreach (var sample in selectedSamples)
            {
                double bhFactor = sampleOwner.TryGetValue(sample, out var owner) &&
                                  boreholeFactors.TryGetValue(owner, out var f)
                    ? f
                    : 1.0;

                raw[sample] = bhFactor * LogNormal(random, SampleSigma);
            }

            double min = raw.Values.Min();
            double max = raw.Values.Max();
            double span = max - min;
            double range = properties.MaxGrade - properties.MinGrade;

            var result = new Dictionary<Sample, double>(ReferenceEqualityComparer.Instance);

            foreach (var sample in selectedSamples)
            {
                double n = span <= Epsilon
                    ? 0.5
                    : (raw[sample] - min) / span;

                result[sample] = properties.MinGrade + range * Math.Clamp(n, 0, 1);
            }

            return result;
        }

        /// <summary>
        /// Лог-нормальный множитель со средним логарифма 0 и заданной сигмой.
        /// </summary>
        private static double LogNormal(Random random, double sigma)
        {
            return Math.Exp(sigma * NextGaussian(random));
        }

        /// <summary>
        /// Стандартное нормальное N(0,1) (Box-Muller), ограниченное по модулю MaxZ.
        /// </summary>
        private static double NextGaussian(Random random)
        {
            double u1 = 1.0 - random.NextDouble();
            double u2 = random.NextDouble();

            double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);

            return Math.Clamp(z, -MaxZ, MaxZ);
        }

        #endregion

        private static List<Sample> SelectOreSamples(IEnumerable<Borehole> boreholes, GenerateGradeProperties properties, Random random)
        {
            var selected = new List<Sample>();

            foreach (var borehole in boreholes)
            {
                var samples = borehole.Samples
                    .OrderBy(x => x.From)
                    .ToList();

                if (samples.Count == 0)
                    continue;

                var rkp = GetRkp(borehole);

                if (rkp == null)
                    continue;

                double rkpTop = rkp.Value.Start;
                double rkpBottom = Math.Max(
                    rkp.Value.Start,
                    rkp.Value.End);

                double rkpThickness = rkpBottom - rkpTop;

                if (rkpThickness < 0)
                    continue;

                // ----------------------------------------------------
                // Пробы полностью выше РКП.
                // ----------------------------------------------------

                var samplesAboveRkp = samples
                    .Where(x =>
                        x.To <= rkpTop + Epsilon &&
                        x.From < rkpTop - Epsilon)
                    .OrderBy(x => x.From)
                    .ToList();

                // ----------------------------------------------------
                // Пробы верхней части РКП.
                // ----------------------------------------------------

                var rkpSamples = samples
                    .Where(x =>
                        x.From >= rkpTop - Epsilon &&
                        x.To <= rkpBottom + Epsilon)
                    .OrderBy(x => x.From)
                    .ToList();

                // ----------------------------------------------------
                // Решаем, будем ли захватывать верхушку РКП.
                // ----------------------------------------------------

                int rkpCount = 0;

                bool canUseRkp =
                    rkpThickness >= properties.MinRkpThicknessForUpperOre &&
                    rkpSamples.Count > 0 &&
                    random.NextDouble() * 100.0 <
                    properties.RkpUpperPartProbability;

                if (canUseRkp)
                {
                    int maxCount = Math.Min(
                        properties.MaxRkpSamples,
                        rkpSamples.Count);

                    rkpCount = random.Next(1, maxCount + 1);

                    double maxAllowedRkpThickness =
                        rkpThickness *
                        properties.MaxRkpOrePercent /
                        100.0;

                    while (rkpCount > 0)
                    {
                        double thickness = rkpSamples
                            .Take(rkpCount)
                            .Sum(x => x.Length);

                        if (thickness <= maxAllowedRkpThickness + Epsilon)
                            break;

                        rkpCount--;
                    }
                }

                // ----------------------------------------------------
                // Мощность, реально занятая верхушкой РКП.
                // ----------------------------------------------------

                double rkpOreThickness = rkpSamples
                    .Take(rkpCount)
                    .Sum(x => x.Length);

                // ----------------------------------------------------
                // Целевая мощность для данной скважины.
                // ----------------------------------------------------

                double targetThickness =
                    properties.MinOreThickness +
                    random.NextDouble() *
                    (properties.MaxOreThickness -
                     properties.MinOreThickness);

                // ----------------------------------------------------
                // Теперь ищем комбинацию существующих проб
                // выше РКП, наиболее близкую к targetThickness.
                //
                // Берём именно нижнюю часть проб перед РКП,
                // чтобы рудный интервал примыкал к РКП.
                // ----------------------------------------------------

                int bestStart = -1;
                double bestDifference = double.MaxValue;
                double bestThickness = 0;

                for (int start = 0;
                     start <= samplesAboveRkp.Count;
                     start++)
                {
                    double thickness =
                        samplesAboveRkp
                            .Skip(start)
                            .Sum(x => x.Length);

                    thickness += rkpOreThickness;

                    if (start == samplesAboveRkp.Count &&
                        rkpCount == 0)
                        continue;

                    double difference =
                        Math.Abs(thickness - targetThickness);

                    if (difference < bestDifference)
                    {
                        bestDifference = difference;
                        bestStart = start;
                        bestThickness = thickness;
                    }
                }

                if (bestStart == -1)
                    continue;

                // ----------------------------------------------------
                // Добавляем выбранные реальные пробы.
                // ----------------------------------------------------

                selected.AddRange(
                    samplesAboveRkp
                        .Skip(bestStart));

                selected.AddRange(
                    rkpSamples.Take(rkpCount));

                Debug.WriteLine(
                    $"BH {borehole.BoreholeLineId}-{borehole.Id}: " +
                    $"target={targetThickness:F3}, " +
                    $"selected={bestThickness:F3}, " +
                    $"RKP samples={rkpCount}");
            }

            return selected
                .Distinct<Sample>(ReferenceEqualityComparer.Instance)
                .ToList();
        }

        #region Reserve fitting

        private static void FitReserve(IReadOnlyList<Block> blocks, GenerateGradeProperties properties, IReadOnlyList<Sample> selectedSamples, IReadOnlyDictionary<Sample, double> initialGrades)
        {
            if (selectedSamples.Count == 0)
                return;

            double target = properties.OreReserve;
            double range = properties.MaxGrade - properties.MinGrade;

            // Нормированные случайные значения [0..1] - сохраняют "рисунок" случайности.
            var normalized = new Dictionary<Sample, double>(ReferenceEqualityComparer.Instance);

            foreach (var sample in selectedSamples)
            {
                double value = range <= 0
                    ? 0.5
                    : (initialGrades[sample] - properties.MinGrade) / range;

                normalized[sample] = Math.Clamp(value, 0, 1);
            }

            double ReserveWithPower(double x)
            {
                ApplyPowerGrades(selectedSamples, normalized, properties, Math.Exp(x));
                return CalculateTotalReserveKg(blocks, properties.Boundaries);
            }

            double ReserveWithScale(double k)
            {
                ApplyScaledGrades(selectedSamples, initialGrades, k);
                return CalculateTotalReserveKg(blocks, properties.Boundaries);
            }

            // --------------------------------------------------------
            // Границы достижимого запаса при содержаниях в [Min, Max].
            // x = ln(p): малый p -> почти все пробы MaxGrade,
            //            большой p -> почти все пробы MinGrade.
            // Запас убывает с ростом x.
            // --------------------------------------------------------

            const double xLow = -6.0;
            const double xHigh = 6.0;

            double reserveMax = ReserveWithPower(xLow);
            double reserveMin = ReserveWithPower(xHigh);

            Debug.WriteLine(
                $"Reachable reserve within [MinGrade, MaxGrade]: " +
                $"{reserveMin:F6} .. {reserveMax:F6} kg, target={target:F6}");

            // ---------------- Режим A: цель достижима в пределах границ ----------------
            if (target >= reserveMin - ReserveToleranceKg &&
                target <= reserveMax + ReserveToleranceKg)
            {
                var (x, reserve) = SolveMonotonic(
                    ReserveWithPower, xLow, xHigh, target, increasing: false);

                ReserveWithPower(x);

                Debug.WriteLine(
                    $"Reserve fitting (power): target={target:F6}, " +
                    $"result={reserve:F6}, error={Math.Abs(reserve - target):F6}, p={Math.Exp(x):F6}");

                return;
            }

            // ---------------- Цель ниже минимума ----------------
            if (target < reserveMin)
            {
                ReserveWithPower(xHigh);

                Debug.WriteLine(
                    $"WARNING: target {target:F6} kg is below the minimum reachable " +
                    $"{reserveMin:F6} kg (all samples at MinGrade).");

                return;
            }

            // ---------------- Режим B: цель выше максимума ----------------
            if (!AllowExceedMaxGrade)
            {
                ReserveWithPower(xLow);

                Debug.WriteLine(
                    $"WARNING: target {target:F6} kg is above the maximum reachable " +
                    $"{reserveMax:F6} kg (all samples at MaxGrade). " +
                    $"Increase MaxGrade or ore thickness / samples count.");

                return;
            }

            Debug.WriteLine(
                $"Target {target:F6} kg is above the reachable maximum {reserveMax:F6} kg: " +
                $"grades will exceed MaxGrade.");

            // Ищем верхнюю границу множителя k (запас растёт с k).
            double kLow = 1.0;
            double kHigh = 2.0;

            for (int i = 0; i < 60 && ReserveWithScale(kHigh) < target; i++)
            {
                kLow = kHigh;
                kHigh *= 2.0;
            }

            var (k, scaledReserve) = SolveMonotonic(
                ReserveWithScale, kLow, kHigh, target, increasing: true);

            ReserveWithScale(k);

            Debug.WriteLine(
                $"Reserve fitting (scale): target={target:F6}, " +
                $"result={scaledReserve:F6}, error={Math.Abs(scaledReserve - target):F6}, k={k:F6}");
        }

        /// <summary>
        /// Бисекция по монотонной функции запаса. Запоминает лучший найденный параметр.
        /// </summary>
        private static (double Param, double Reserve) SolveMonotonic(
            Func<double, double> reserveOf,
            double lo,
            double hi,
            double target,
            bool increasing)
        {
            double bestParam = lo;
            double bestReserve = reserveOf(lo);
            double bestError = Math.Abs(bestReserve - target);

            for (int i = 0; i < 100 && bestError > ReserveToleranceKg; i++)
            {
                double mid = (lo + hi) / 2.0;
                double reserve = reserveOf(mid);
                double error = Math.Abs(reserve - target);

                if (error < bestError)
                {
                    bestError = error;
                    bestReserve = reserve;
                    bestParam = mid;
                }

                bool needLargerParam = increasing
                    ? reserve < target
                    : reserve > target;

                if (needLargerParam)
                    lo = mid;
                else
                    hi = mid;
            }

            return (bestParam, bestReserve);
        }

        /// <summary>
        /// grade = Min + range * n^power, где n - исходное нормированное случайное значение.
        /// Остаётся в [Min, Max], сохраняет разброс между пробами.
        /// </summary>
        private static void ApplyPowerGrades(IReadOnlyList<Sample> samples, IReadOnlyDictionary<Sample, double> normalized, GenerateGradeProperties properties, double power)
        {
            double range = properties.MaxGrade - properties.MinGrade;

            foreach (var sample in samples)
            {
                double value = Math.Pow(normalized[sample], power);

                sample.Grade = properties.MinGrade + range * value;
            }
        }

        /// <summary>
        /// grade = исходное случайное содержание * k. Может превышать MaxGrade.
        /// </summary>
        private static void ApplyScaledGrades(IReadOnlyList<Sample> samples, IReadOnlyDictionary<Sample, double> initialGrades, double scale)
        {
            foreach (var sample in samples)
                sample.Grade = initialGrades[sample] * scale;
        }

        private static double CalculateTotalReserveKg(IEnumerable<Block> blocks, List<BoundaryData> boundaries)
        {
            return blocks.Sum(x => x.ReserveKg);
        }

        #endregion

        #region Boreholes Helpers

        private static void ClearSamplesGradeInBoreholes(IEnumerable<Borehole> boreholes)
        {
            foreach (var borehole in boreholes)
            {
                foreach (var sample in borehole.Samples)
                    sample.Grade = 0;
            }
        }

        #endregion

        private static (double Start, double End)? GetRkp(Borehole borehole)
        {
            var rkpInterval = borehole.Atributes
                .GetValueOrDefault("РКП Интервал")
                ?.ToString();

            if (string.IsNullOrWhiteSpace(rkpInterval))
                return null;

            var intervals = rkpInterval.Split('-', StringSplitOptions.RemoveEmptyEntries);

            var startText = intervals.FirstOrDefault()?.Replace(',', '.').Trim();
            var endText = intervals.LastOrDefault()?.Replace(',', '.').Trim();

            if (startText == null || endText == null)
                return null;

            if (!double.TryParse(startText, NumberStyles.Float, CultureInfo.InvariantCulture, out var start))
                return null;

            if (!double.TryParse(endText, NumberStyles.Float, CultureInfo.InvariantCulture, out var end))
                return null;

            return (start, end);
        }
    }
}