using GeoAppCore;
using GeoAppCore.Ore;

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

        // --- Новая логика: если список пуст, ищем резервный интервал ---
        if (result.Count == 0 && samples.Count > 0)
        {
            int lowestIndex = -1;

            // 1. Идем с конца, чтобы найти самую нижнюю подходящую пробу
            for (int j = samples.Count - 1; j >= 0; j--)
            {
                double grade = samples[j].PureAvgGrade;
                if (grade > 0 || grade == -1)
                {
                    lowestIndex = j;
                    break;
                }
            }

            // --- Новая логика: если список пуст, ищем резервный интервал ---
            if (result.Count == 0 && samples.Count > 0)
            {
                // ЭТАП 1: Ищем интервалы весовых проб (PureAvgGrade > 0)
                var weightIntervals = new List<(int Start, int End)>();
                int currentStart = -1;

                for (int j = 0; j < samples.Count; j++)
                {
                    if (samples[j].PureAvgGrade > 0)
                    {
                        if (currentStart == -1) currentStart = j;
                    }
                    else
                    {
                        if (currentStart != -1)
                        {
                            weightIntervals.Add((currentStart, j - 1));
                            currentStart = -1;
                        }
                    }
                }
                // Закрываем интервал, если он дошел до конца списка
                if (currentStart != -1)
                {
                    weightIntervals.Add((currentStart, samples.Count - 1));
                }

                if (weightIntervals.Count > 0)
                {
                    // Нашли хотя бы один интервал весовых проб.
                    // Ищем тот, у которого среднее содержание (взвешенное на мощность) самое большое.
                    double maxGrade = -1;
                    int bestStart = -1;
                    int bestEnd = -1;

                    foreach (var interval in weightIntervals)
                    {
                        double sumLen = 0;
                        double sumGradeLen = 0;
                        for (int k = interval.Start; k <= interval.End; k++)
                        {
                            sumLen += samples[k].Length;
                            sumGradeLen += samples[k].PureAvgGrade * samples[k].Length;
                        }
                        
                        double avgGrade = sumLen > 0 ? (sumGradeLen / sumLen) : 0;

                        if (avgGrade > maxGrade)
                        {
                            maxGrade = avgGrade;
                            bestStart = interval.Start;
                            bestEnd = interval.End;
                        }
                    }

                    var fallbackSamples = samples.GetRange(bestStart, bestEnd - bestStart + 1);
                    result.Add(new OreInterval(fallbackSamples, null));
                }
                else
                {
                    // ЭТАП 2: Весовых проб нет. Ищем интервалы ЗН проб (PureAvgGrade == -1)
                    var znIntervals = new List<(int Start, int End)>();
                    currentStart = -1;

                    for (int j = 0; j < samples.Count; j++)
                    {
                        if (samples[j].PureAvgGrade == -1)
                        {
                            if (currentStart == -1) currentStart = j;
                        }
                        else
                        {
                            if (currentStart != -1)
                            {
                                znIntervals.Add((currentStart, j - 1));
                                currentStart = -1;
                            }
                        }
                    }
                    if (currentStart != -1)
                    {
                        znIntervals.Add((currentStart, samples.Count - 1));
                    }

                    if (znIntervals.Count > 0)
                    {
                        // Если нашли несколько ЗН-интервалов, берем самый нижний (последний по списку).
                        // Если вам нужен первый (самый верхний), замените znIntervals.Last() на znIntervals.First()
                        var lastZnInterval = znIntervals[znIntervals.Count - 1]; 
                        
                        var fallbackSamples = samples.GetRange(lastZnInterval.Start, lastZnInterval.End - lastZnInterval.Start + 1);
                        result.Add(new OreInterval(fallbackSamples, null));
                    }
                }
            }
        }

        return result;
    }
}