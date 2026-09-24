using GeoAppCore.Services;

namespace GeoAppCore.Ore
{
    public static class BoreholeOreIntervalExtensions
    {
        public static List<OreInterval> BuildOreIntervals(this Borehole borehole, IOreCondition condition)
        {
            var samples = borehole.Samples
                .OrderBy(s => s.From)
                .ToList();

            var result = new List<OreInterval>();
            int i = 0;

            while (i < samples.Count)
            {
                // Пласт может начаться только с рудной пробы
                if (!condition.IsSampleOre(samples[i]))
                {
                    i++;
                    continue;
                }

                // Мощность торфов — расстояние от начала скважины до устья пласта
                double peatThickness = samples[i].From;

                double sumLen = 0;
                double sumGradeLen = 0;
                double wasteRun = 0;

                int lastGoodEnd = -1;

                for (int k = i; k < samples.Count; k++)
                {
                    var s = samples[k];
                    bool isOre = condition.IsSampleOre(s);

                    sumLen += s.Length;
                    // Накопление метр-граммов по чистому значению
                    sumGradeLen += Math.Max(s.PureAvgGrade, 0) * s.Length;

                    if (isOre)
                    {
                        wasteRun = 0;
                    }
                    else
                    {
                        wasteRun += s.Length;

                        // Если превышен лимит пустых пород внутри пласта — останавливаем расширение
                        if (condition.MaxWasteThickness.HasValue && wasteRun > condition.MaxWasteThickness.Value)
                            break;
                    }

                    // Фиксируем границу, если текущая проба рудная и весь интервал проходит по кондициям
                    if (isOre && condition.IsIntervalValid(sumLen, sumGradeLen, peatThickness))
                    {
                        lastGoodEnd = k;
                    }
                }

                // Защита: проверяем, сформировался ли валидный пласт
                if (lastGoodEnd != -1)
                {
                    var oreSamples = samples.GetRange(i, lastGoodEnd - i + 1);
                    result.Add(new OreInterval(oreSamples));

                    // Переходим к поиску следующего пласта за пределами найденного
                    i = lastGoodEnd + 1;
                }
                else
                {
                    // Если пласт не прошел по кондициям, сдвигаемся на 1 пробу вперед
                    i++;
                }
            }

            return result;
        }
    }
}
