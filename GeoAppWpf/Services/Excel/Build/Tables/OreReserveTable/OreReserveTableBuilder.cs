using GeoAppCore;
using GeoAppCore.Ore;
using GeoAppCore.Services;
using System.Drawing;

namespace GeoAppWpf.Services.Excel.Build.Tables.OreReserveTable
{
    public class OreReserveTableTemplate
    {
        public static TableDefinition Create()
        {
            var columns = new List<TableColumn>()
            {
                    new() { Key = "Number" },

                    new() { Key = "BlockNumber" },
                    new() { Key = "BoreholeLineNumber" },

                    new() { Key = "BlockLength", Format = "F1" },
                    new() { Key = "BlockWidth", Format = "F1" },
                    new() { Key = "BlockSquare", Format = "F1" },

                    new() { Key = "TorfLength", Format = "F1" },
                    new() { Key = "SandLength", Format = "F1" },

                    new() { Key = "TorfVolume", Format = "F1" },
                    new() { Key = "SandVolume", Format = "F1" },

                    new() { Key = "AvgGrade", Format = "F3" },
                    new() { Key = "LevelingAvgGrade", Format = "F3" },
                    new() { Key = "LevelingPureAvgGrade", Format = "F3" },

                    new() { Key = "AvgReserve", Format = "F1" },
                    new() { Key = "LevelingAvgReserve", Format = "F1" },
                    new() { Key = "LevelingPureAvgReserve", Format = "F1" },

                    new() { Key = "CoefPure", Format = "F1" },

                    new() { Key = "MinGrade", Format = "F3" },
            };

            var mergeRules = columns.Select(x => СreateMergeRuleForHeader(x.Key)).ToList();

            TableMergeRule СreateMergeRuleForHeader(string colKey)
            {
                return new TableMergeRule
                {
                    ColumnKey = colKey,
                    Vertical = true,
                    CanMerge = row => row.Values.TryGetValue("CustomHeader", out var value) && value is true
                };
            }

            return new TableDefinition
            {
                Name = "Ведомость ПЗ",
                CanUserSortColumns = false,
                ShowGridLines = true,
                AutoFitColumns = true,
                HasHeader = false,
                HeaderStyle = ExcelStyles.DefaultHeader,

                Style = new TableStyle
                {
                    Border = new TableBorderStyle
                    {
                        Top = true,
                        Bottom = true,
                        Left = true,
                        Right = true,
                        Color = Color.Black
                    }
                },

                MergeRules = mergeRules.Concat(new List<TableMergeRule>()
                {
                    new()
                    {
                        Horizontal = true,
                        MergeRepeatedHorizontal = true,

                        CanMerge = row =>
                            row.Values.TryGetValue("CustomHeader", out var value) &&
                            value is true
                    },
                    new()
                    {
                        ColumnKey = "Number",
                        Vertical = true,

                        CanMerge = row => !(row.Values.GetValueOrDefault("Number")?.ToString()?.Contains("Всего") ?? false)
                    },
                    new()
                    {
                        ColumnKey = "BlockNumber",
                        Vertical = true,

                        CanMerge = row => !(row.Values.GetValueOrDefault("Number")?.ToString()?.Contains("Всего") ?? false)
                    },
                    new()
                    {
                        ColumnKey = "Number",
                        EndColumnKey = "BoreholeLineNumber",
                        Horizontal = true,

                        CanMerge = row =>
                        {
                            row.Values.TryGetValue("Number", out var value);
                            var text = value?.ToString();
                            return text?.Contains("Всего", StringComparison.InvariantCultureIgnoreCase) ?? false;
                        }
                    }
                }).ToList(),

                RowStyleRules =
                [

                ],

                Columns = columns
            };
        }
    }

    public class OreReserveTableBuilder
    {
        public static TableDefinition Build(IEnumerable<Block> blocks, IEnumerable<BoreholeLine> lines)
        {
            var table = OreReserveTableTemplate.Create();

            AddHeader(table);

            int blockIndex = 1;

            foreach (var block in blocks)
            {
                var boreholeLinesGroups = block.Boreholes
                    .GroupBy(x => x.Source.BoreholeLineId)
                    .ToList();

                foreach (var boreholeLinesGroup in boreholeLinesGroups)
                {
                    var line = lines.Where(x => x.Id == boreholeLinesGroup.Key).FirstOrDefault();

                    if (line == null)
                        continue;

                    var cbhs = line.BuildSections();

                    int holesCount = block.Boreholes.Count;

                    double torfLengthSum = cbhs.Sum(x => x.OreInterval?.From ?? 0);
                    double sandLengthSum = cbhs.Sum(x => x.OreInterval?.Thickness ?? 0);

                    double pureVertReserveSum = cbhs.Sum(x => x.OreInterval?.PureVertReserve ?? 0);
                    double pureAvgGrade = pureVertReserveSum / sandLengthSum;

                    table.Rows.Add(new()
                    {
                        Values =
                        {
                            ["Number"] = blockIndex,

                            ["BlockNumber"] = block.Id,
                            ["BoreholeLineNumber"] = line.Id,

                            ["BlockLength"] = null,
                            ["BlockWidth"] = null,
                            ["BlockSquare"] = null,

                            ["TorfLength"] = torfLengthSum / holesCount,
                            ["SandLength"] = sandLengthSum / holesCount,

                            ["TorfVolume"] = null,
                            ["SandVolume"] = null,

                            ["AvgGrade"] = pureAvgGrade,
                            ["LevelingAvgGrade"] = pureAvgGrade,
                            ["LevelingPureAvgGrade"] = pureAvgGrade,

                            ["AvgReserve"] = null,
                            ["LevelingAvgReserve"] = null,
                            ["LevelingPureAvgReserve"] = null,

                            ["CoefPure"] = line.Boreholes.FirstOrDefault()?.Samples.FirstOrDefault()?.Fineness ?? 1,

                            ["MinGrade"] = null,
                        }
                    });
                }

                blockIndex++;
            }

            return table;
        }

        private static void AddHeader(TableDefinition table)
        {
            var customHeaderStyle = new TableRowStyle
            {
                Bold = true,
                Border = ExcelStyles.AllSidesBorder,
                WrapText = true
            };

            table.Rows.Add(new()
            {
                Values =
                {
                    ["CustomHeader"] = true,

                    ["Number"] = "№ пп",

                    ["BlockNumber"] = "№ блока и категория запасов",
                    ["BoreholeLineNumber"] = "№ бур. линии ",

                    ["BlockLength"] = "Длина блока, м",
                    ["BlockWidth"] = "Ширина блока, м",
                    ["BlockSquare"] = "Площадь блока, тыс. м²",

                    ["TorfLength"] = "Мощность,м",
                    ["SandLength"] = "Мощность,м",

                    ["TorfVolume"] = "Объем, тыс. м3",
                    ["SandVolume"] = "Объем, тыс. м3",

                    ["AvgGrade"] = "Среднее содержание золота, г/м3",
                    ["LevelingAvgGrade"] = "Среднее содержание золота, г/м3",
                    ["LevelingPureAvgGrade"] = "Среднее содержание золота, г/м3",

                    ["AvgReserve"] = "Запас золота, кг",
                    ["LevelingAvgReserve"] = "Запас золота, кг",
                    ["LevelingPureAvgReserve"] = "Запас золота, кг",

                    ["CoefPure"] = "Коэф. х.ч.",

                    ["MinGrade"] = "Минимальное промышленное содержание золота (согласно УТЭР)",
                },
                Height = 40,
                Style = customHeaderStyle,
            });

            table.Rows.Add(new()
            {
                Values =
                {
                    ["CustomHeader"] = true,

                    ["Number"] = "№ пп",

                    ["BlockNumber"] = "№ блока и категория запасов",
                    ["BoreholeLineNumber"] = "№ бур. линии ",

                    ["BlockLength"] = "Длина блока, м",
                    ["BlockWidth"] = "Ширина блока, м",
                    ["BlockSquare"] = "Площадь блока, тыс. м²",

                    ["TorfLength"] = "Торфов",
                    ["SandLength"] = "Песков",

                    ["TorfVolume"] = "Торфов",
                    ["SandVolume"] = "Песков",

                    ["AvgGrade"] = "Шлих",
                    ["LevelingAvgGrade"] = "Шлих с учётом нивели-ровки",
                    ["LevelingPureAvgGrade"] = "х/ч с учётом нивели-ровки",

                    ["AvgReserve"] = "Шлих",
                    ["LevelingAvgReserve"] = "Шлих с учётом нивели-ровки",
                    ["LevelingPureAvgReserve"] = "х/ч с учётом нивели-ровки",

                    ["CoefPure"] = "Коэф. х.ч.",

                    ["MinGrade"] = "в блоке г/м³ х/ч",
                },
                Height = 40,
                Style = customHeaderStyle
            });
        }
    }
}
