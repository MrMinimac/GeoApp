using GeoAppCore;
using GeoAppCore.Ore;
using GeoAppWpf.Helpers;

namespace GeoAppWpf.Services.Excel.Build.Tables.BoreholeInfluence
{
    public static class BoreholeInfluenceTableBuilder
    {
        public static TableDefinition Build(IEnumerable<BoreholeLine> boreholeLines)
        {
            var table = BoreholeInfluenceTableTemplate.Create();

            var macroGroupRows = BuildMacroGroupTable(boreholeLines);
            table.Rows.AddRange(macroGroupRows);

            table.CanUserSortColumns = false;
            return table;
        }

        private static List<TableRow> BuildMacroGroupTable(IEnumerable<BoreholeLine> boreholeLines, int minHoles = 25, int maxHoles = 30, int concatMaxHoles = 35)
        {
            var rows = new List<TableRow>();

            var resultGroups = BuildMacroGroups(boreholeLines);

            foreach (var group in resultGroups)
            {
                int totalHolesCount = 0;

                var allHoles = group.Items;

                double totalLength = allHoles.Sum(h => h.Deapth);
                double totalGrade = allHoles.Sum(h => h.Source.AvgValue);
                double totalGrade2 = allHoles.Sum(h => h.Source.AvgValue * h.Deapth);
                double totalImpact = 0;

                double sumOfAllGrade2 = allHoles.Sum(h => h.Source.AvgValue * h.Deapth);

                double limit10 = totalGrade2 * 0.1;
                double limit15 = totalGrade2 * 0.15;
                double limit20 = totalGrade2 * 0.2;

                string boreholeLineCurrent = allHoles.FirstOrDefault()?.Source.BoreholeLineId ?? "";

                foreach (var hole in allHoles)
                {
                    if (boreholeLineCurrent != hole.Source.BoreholeLineId)
                    {
                        var currentLineHoles = allHoles
                            .Where(x => x.Source.BoreholeLineId == boreholeLineCurrent)
                            .ToList();

                        if (currentLineHoles != null && currentLineHoles.Count > 0)
                        {
                            var allLength = currentLineHoles.Sum(x => x.Deapth);
                            var allVertReserve = currentLineHoles.Sum(x => x.Source.AvgValue * x.Deapth);

                            double lineImpact = (allVertReserve / sumOfAllGrade2) * 100;

                            rows.Add(new TableRow
                            {
                                Values =
                                {
                                    ["GroupsNumber"] = group.GroupNumber,
                                    ["BoreholeLine"] = "Итого по линии",
                                    ["HolesCount"] = allHoles.Count,
                                    ["Length"] = allLength,
                                    ["Grade"] = allVertReserve / allLength,
                                    ["Grade2"] = allVertReserve,
                                }
                            });
                        }
                    }

                    boreholeLineCurrent = hole.Source.BoreholeLineId;

                    double currentGrade2 = hole.Source.AvgValue * hole.Deapth;

                    double impact =
                        currentGrade2 != 0
                            ? (currentGrade2 / sumOfAllGrade2) * 100
                            : 0;

                    totalHolesCount++;
                    totalImpact += impact;

                    rows.Add(new TableRow
                    {
                        Values =
                        {
                            ["GroupsNumber"] = group.GroupNumber,
                            ["BoreholeLine"] = hole.Source.BoreholeLineId,
                            ["BoreholeNumber"] = hole.Id,
                            ["HolesCount"] = totalHolesCount,
                            ["Length"] = hole.Deapth,
                            ["Grade"] = hole.Source.AvgValue,
                            ["Grade2"] = currentGrade2,
                            ["Impact"] = impact,

                            ["Impact10"] = impact >= 10 ? limit10 / hole.Deapth : null,
                            ["Impact10_2"] = impact >= 10 ? limit10 : null,

                            ["Impact15"] = impact >= 15 ? limit15 / hole.Deapth : null,
                            ["Impact15_2"] = impact >= 15 ? limit15 : null,

                            ["Impact20"] = impact >= 20 ? limit20 / hole.Deapth : null,
                            ["Impact20_2"] = impact >= 20 ? limit20 : null,
                        }
                    });
                }

                rows.Add(new TableRow
                {
                    Values =
                    {
                        ["GroupsNumber"] = "ВСЕГО ПО ГРУППЕ",
                        ["HolesCount"] = totalHolesCount,
                        ["Length"] = totalLength,
                        ["Grade"] = totalGrade2 / totalLength,
                        ["Grade2"] = totalGrade2,
                        ["Impact"] = totalImpact
                    },
                    Highlight = true
                });
                rows.Add(new TableRow
                {
                    Values =
                    {
                        ["GroupsNumber"] = "ЛИМИТ 10%",
                        ["Grade2"] = limit10,
                    },
                    Highlight = true
                });
                rows.Add(new TableRow
                {
                    Values =
                            {
                                ["GroupsNumber"] = "ЛИМИТ 15%",
                                ["Grade2"] = limit15,
                            },
                    Highlight = true
                });
                rows.Add(new TableRow
                {
                    Values =
                            {
                                ["GroupsNumber"] = "ЛИМИТ 20%",
                                ["Grade2"] = limit20,
                            },
                    Highlight = true
                });
            }

            return rows;
        }

        private static List<(int GroupNumber, List<SectionBorehole> Items)> BuildMacroGroups(IEnumerable<BoreholeLine> boreholeLines, int minHoles = 25, int maxHoles = 30, int concatMaxHoles = 35)
        {
            var remainingObjects =
                boreholeLines
                .OrderBy(w => w.Id.Any(char.IsDigit) ? 0 : 1)
                .ThenBy(w => w.Id, new NaturalNameComparer())
                .Select(x => x.BuildSections().Where(x => x.OreInterval?.ConditionResult.IsValid ?? false).ToList())
                .ToList();

            var restPart = new List<SectionBorehole>();

            var resultGroups = new List<(int GroupNumber, List<SectionBorehole> Items)>();
            int groupCounter = 1;

            while (remainingObjects.Any())
            {
                var currentGroupItems = new List<SectionBorehole>();
                int currentGroupHolesCount = 0;

                for (int i = 0; i < remainingObjects.Count; i++)
                {
                    var section = remainingObjects[i];

                    if (currentGroupHolesCount == 0 && section.Count > maxHoles)
                    {
                        var taken = section.Take(maxHoles).ToList();

                        restPart.AddRange(section.Skip(maxHoles));

                        currentGroupItems.AddRange(taken);
                        currentGroupHolesCount += taken.Count;

                        remainingObjects.RemoveAt(i);
                        i--;

                        break;
                    }

                    if (restPart.Count > 0)
                    {
                        var available = maxHoles - currentGroupHolesCount;

                        var taken = restPart.Take(available).ToList();

                        currentGroupItems.AddRange(taken);
                        restPart.RemoveRange(0, taken.Count);

                        currentGroupHolesCount += taken.Count;

                        if (currentGroupHolesCount >= maxHoles)
                            break;

                        continue;
                    }

                    if (currentGroupHolesCount + section.Count <= maxHoles)
                    {
                        currentGroupItems.AddRange(section);
                        currentGroupHolesCount += section.Count;

                        remainingObjects.RemoveAt(i);
                        i--;
                    }

                    if (currentGroupHolesCount >= minHoles)
                        break;
                }

                resultGroups.Add((groupCounter, currentGroupItems));
                groupCounter++;
            }

            if (resultGroups.Count >= 2)
            {
                int lastIdx = resultGroups.Count - 1;
                int preLastIdx = resultGroups.Count - 2;

                var lastGroup = resultGroups[lastIdx];
                var preLastGroup = resultGroups[preLastIdx];

                int holesSum = lastGroup.Items.Count + preLastGroup.Items.Count;

                // Если в сумме не больше 35
                if (holesSum <= concatMaxHoles)
                {
                    // 1. Добавляем каркасы из последней группы в предпоследнюю
                    preLastGroup.Items.AddRange(lastGroup.Items);

                    // 2. Удаляем последнюю группу
                    resultGroups.RemoveAt(lastIdx);

                    // 3. Откатываем счетчик групп на 1 назад, так как одну группу мы уничтожили
                    groupCounter--;
                }
            }

            return resultGroups;
        }
    }
}
