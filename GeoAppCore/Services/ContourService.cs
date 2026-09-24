using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoAppCore.Services
{
    /// <summary>
    /// Рудный интервал (пересечение пласта) по одной скважине — 
    /// результат оконтуривания по бортовому содержанию.
    /// </summary>
    public class OreInterval
    {
        public IReadOnlyList<Sample> Samples { get; }

        public double From => Samples.First().From;
        public double To => Samples.Last().To;
        public double Length => Math.Round(To - From, 3);

        /// <summary>
        /// Средневзвешенное по мощности содержание в пласте.
        /// </summary>
        public double AvgGrade
        {
            get
            {
                double len = Samples.Sum(s => s.Length);
                if (len <= 0) return 0;

                double weighted = Samples.Sum(s => Math.Max(s.Grade, 0) * s.Length);
                return Math.Round(weighted / len, 3);
            }
        }

        public OreInterval(IEnumerable<Sample> samples)
        {
            Samples = samples.OrderBy(s => s.From).ToList();
        }
    }

    public static class BoreholeOreIntervalExtensions
    {
        /// <summary>
        /// Оконтуривает скважину по бортовому содержанию и возвращает список
        /// пластопересечений (по одной скважине их может быть несколько).
        ///
        /// Правила:
        ///  - пласт может НАЧИНАТЬСЯ и ЗАКАНЧИВАТЬСЯ только на пробе,
        ///    содержание которой само по себе >= minGrade;
        ///  - пробы ниже борта внутри пласта допускаются (как внутреннее
        ///    разубоживание), пока средневзвешенное содержание всего
        ///    накопленного интервала остаётся >= minGrade;
        ///  - если проба с содержанием ниже борта не помогает "вытянуть"
        ///    среднее обратно выше борта (и так до конца скважины / до
        ///    превышения maxWasteThickness), она в пласт не попадает —
        ///    пласт закрывается на последней "хорошей" точке, а поиск
        ///    следующего пласта продолжается со следующей пробы после неё
        ///    (крайняя рудная проба, с которой не получилось,
        ///    в этом случае образует свой собственный однопробный пласт).
        /// </summary>
        /// <param name="minGrade">
        /// Минимальное (бортовое) содержание для пласта. Параметр, который
        /// можно менять (в примере пользователя — 0.15).
        /// </param>
        /// <param name="maxWasteThickness">
        /// Максимально допустимая суммарная мощность подряд идущих
        /// "пустых" (ниже борта) проб внутри пласта. Если null — прослой
        /// ничем, кроме итогового среднего содержания, не ограничен
        /// (пласт может тянуться сколь угодно долго, пока где-то дальше
        /// не найдётся руда, вытягивающая среднее обратно выше борта).
        /// На практике этот параметр стоит задавать, иначе один богатый
        /// интервал далеко впереди может "притянуть" к себе весь пустой
        /// массив между ним и предыдущим пластом.
        /// </param>
        public static List<OreInterval> BuildOreIntervals(
            this Borehole borehole,
            double minGrade,
            double? maxWasteThickness = null)
        {
            // пробы "зн" (не опробовано, Grade == -1) в подсчёт не идут и
            // пласт через них не протягиваем — они всегда обрывают интервал
            var samples = borehole.Samples
                .Where(s => s.Grade != -1)
                .OrderBy(s => s.From)
                .ToList();

            var result = new List<OreInterval>();
            int i = 0;

            while (i < samples.Count)
            {
                // пласт может начаться только с рудной пробы
                if (samples[i].Grade < minGrade)
                {
                    i++;
                    continue;
                }

                double sumLen = 0;
                double sumGradeLen = 0;
                double wasteRun = 0;

                int lastGoodEnd = -1;      // индекс последней допустимой границы пласта
                int k = i;

                while (k < samples.Count)
                {
                    var s = samples[k];

                    sumLen += s.Length;
                    sumGradeLen += Math.Max(s.Grade, 0) * s.Length;

                    if (s.Grade >= minGrade)
                    {
                        wasteRun = 0;
                    }
                    else
                    {
                        wasteRun += s.Length;

                        if (maxWasteThickness.HasValue && wasteRun > maxWasteThickness.Value)
                            break; // прослой пустых проб превысил допустимую мощность
                    }

                    double avg = sumLen > 0 ? sumGradeLen / sumLen : 0;

                    // фиксируем точку как допустимую границу пласта, только если
                    // сама проба рудная И среднее по накопленному интервалу не ниже борта
                    if (s.Grade >= minGrade && avg >= minGrade)
                        lastGoodEnd = k;

                    k++;
                }

                result.Add(new OreInterval(samples.GetRange(i, lastGoodEnd - i + 1)));
                i = lastGoodEnd + 1; // продолжаем поиск следующего пласта после найденного
            }

            return result;
        }
    }
}
