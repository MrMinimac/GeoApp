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

                double peatThickness = samples[i].From;

                double sumLen = 0;
                double sumGradeLen = 0;
                double wasteRun = 0;

                int lastGoodEnd = -1;

                IOreConditionResult tempConditionResult = null;

                for (int k = i; k < samples.Count; k++)
                {
                    var s = samples[k];
                    bool isOre = condition.IsSampleOre(s);

                    sumLen += s.Length;
                    sumGradeLen += Math.Max(s.PureAvgGrade, 0) * s.Length;

                    if (isOre)
                    {
                        wasteRun = 0;
                    }
                    else
                    {
                        wasteRun += s.Length;

                        if (condition.MaxWasteThickness.HasValue && wasteRun > condition.MaxWasteThickness.Value)
                            break;
                    }

                    var validResult = condition.IsIntervalValid(sumLen, sumGradeLen, peatThickness);

                    if (isOre && validResult.IsValid)
                    {
                        lastGoodEnd = k;
                        tempConditionResult = validResult;
                    }
                }

                if (lastGoodEnd != -1)
                {
                    var oreSamples = samples.GetRange(i, lastGoodEnd - i + 1);
                    result.Add(new OreInterval(oreSamples, tempConditionResult));

                    i = lastGoodEnd + 1;
                }
                else
                {
                    i++;
                }
            }

            return result;
        }
    }
}
