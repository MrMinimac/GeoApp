using GeoAppCore.Services;
using GeoAppWpf.Services.Excel.Build.Data;
using System.Drawing;

namespace GeoAppWpf.Services.Excel.Build.Tables.BlocksReportTable
{
    public static class BlocksReportTableTemplate
    {
        public static TableDefinition Create()
        {
            return new TableDefinition
            {
                Name = "Блочная ведомость запасов",
                CanUserSortColumns = false,
                ShowGridLines = true,
                AutoFitColumns = true,
                HasHeader = true,
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

                HeaderMergeRules =
                [
                    new()
                    {
                        ColumnKey = "Impact10",
                        EndColumnKey = "Impact10_2"
                    },
                ],

                MergeRules =
                [
                    new TableMergeRule
                    {
                        ColumnKey = "Block",
                        Horizontal = true,
                        CanMerge = row => row.Values.TryGetValue("IsRegionHeader", out var isRegion) && isRegion is true
                    },
                    new()
                    {
                        ColumnKey = "Block",
                        Vertical = true,
                        SkipEmpty = true,

                        CanMerge = row =>
                        {
                            row.Values.TryGetValue("Block", out var value);

                            var text = value?.ToString();

                            return text != "Итого" && text != "Среднее";
                        }
                    },
                    new()
                    {
                        ColumnKey = "BoreholeLine",
                        Vertical = true,
                        SkipEmpty = true,

                        CanMerge = row =>
                        {
                            row.Values.TryGetValue("BoreholeLine", out var value);
                            var text = value?.ToString();
                            return text != "Итого" && text != "Среднее";
                        }
                    },
                    new()
                    {
                        ColumnKey = "Block",
                        EndColumnKey = "BoreholeNumber",
                        Horizontal = true,

                        CanMerge = row =>
                        {
                            row.Values.TryGetValue("Block", out var value);

                            var text = value?.ToString();

                            return text == "Итого" || text == "Среднее";
                        }
                    }
                ],

                RowStyleRules =
                [
                    new()
                    {
                        Condition = row =>
                        {
                            var text = row.Values.GetValueOrDefault("Block")?.ToString();
                            return text == "Итого" || text == "Среднее";
                        },

                        Bold = true,
                        BackgroundColor = Color.FromArgb(255, 226, 239, 218),
                        Border = ExcelStyles.DefaultBorder
                    },

                    ExcelStyles.DefaultHighlightedCellStyle("Impact10"),
                ],

                Columns =
                [
                    new() { Header = "№ блока, категория запасов", Key = "Block" },
                    new() { Header = "№ Линии", Key = "BoreholeLine" },
                    new() { Header = "№ Скважины", Key = "BoreholeNumber" },
                    new() { Header = "Количество скважин", Key = "HolesCount" },
                    new() { Header = "Мощность торфов, м", Key = "TorfLength", Format = "F1" },
                    new() { Header = "Мощность песков, м", Key = "SandLength", Format = "F1" },
                    new() { Header = "Ср. сод-е на пласт Au х/ч, г/м³", Key = "Grade", Format = "F3" },
                    new() { Header = "Верт. запас х/ч, г/м²", Key = "VertReserv", Format = "F3" },
                    new() { Header = "С учетом нивелировки 10%, Ср. сод. Au х/ч, г/м³", Key = "Impact10", Format = "F3" },
                    new() { Header = "С учетом нивелировки 10%, Ср. сод. Au х/ч, г/м³", Key = "Impact10_2", Format = "F3" },
                ]
            };
        }
    }

    public class BlocksReportTableBuilder
    {
        public static TableDefinition Build(IEnumerable<Block> blocks, IEnumerable<LevelingBoreholeGroup> levelingGroups)
        {
            var table = BlocksReportTableTemplate.Create();

            var allLevelingHoles = levelingGroups.SelectMany(x => x.Items).ToList();

            string regionCurrent = "";

            double totalVertReserv = blocks
                .SelectMany(x => x.Boreholes)
                .Sum(h => h.OreInterval?.PureVertReserve ?? 0);

            foreach (var block in blocks)
            {
                if (block.Boreholes.Count == 0)
                    continue;

                var newRegion = block.Boreholes
                    .FirstOrDefault()?
                    .Source.Atributes
                    .GetValueOrDefault("Участок")?
                    .ToString() ?? "";

                if (regionCurrent != newRegion)
                {
                    table.Rows.Add(new TableRow
                    {
                        Values =
                        {
                            ["Block"] = newRegion,
                            ["IsRegionHeader"] = true
                        }
                    });

                    regionCurrent = newRegion;
                }

                int holeCounter = 1;

                double torfLengthCounter = 0;
                double sandLengthCounter = 0;
                double pureVertReservCounter = 0;

                foreach (var hole in block.Boreholes)
                {
                    var curTorfLength = hole.OreInterval?.From ?? 0;
                    var curSandLength = hole.OreInterval?.Thickness ?? 0;
                    var curGrade = hole.OreInterval?.PureAvgGrade ?? 0;
                    var curVertReserv = hole.OreInterval?.PureVertReserve ?? 0;

                    var levelingHole = allLevelingHoles.Where(x => x.SectionBorehole.Source == hole.Source)
                        .FirstOrDefault();

                    table.Rows.Add(new TableRow
                    {
                        Values =
                        {
                            ["Block"] = block.Id,
                            ["BoreholeLine"] = hole.Source.BoreholeLineId,
                            ["BoreholeNumber"] = hole.Source.Id,
                            ["HolesCount"] = holeCounter,
                            ["TorfLength"] = curTorfLength,
                            ["SandLength"] = curSandLength,
                            ["Grade"] = curGrade,
                            ["VertReserv"] = curVertReserv,

                            ["Impact10"] = levelingHole?.ImpactLeveling10,
                            ["Impact10_2"] = levelingHole?.ImpactLeveling10 != null ? levelingHole?.Group?.PureVertReserveLeveling10 : null,
                        }
                    });

                    torfLengthCounter += curTorfLength;
                    sandLengthCounter += curSandLength;
                    pureVertReservCounter += curVertReserv;

                    holeCounter++;
                }

                int holesCount = block.Boreholes.Count;

                // Итого
                table.Rows.Add(new TableRow
                {
                    Values =
                    {
                        ["Block"] = "Итого",
                        ["HolesCount"] = holesCount,
                        ["TorfLength"] = torfLengthCounter,
                        ["SandLength"] = sandLengthCounter,
                        ["VertReserv"] = pureVertReservCounter,
                    }
                });

                // Среднее
                table.Rows.Add(new TableRow
                {
                    Values =
                    {
                        ["Block"] = "Среднее",

                        ["TorfLength"] = holesCount > 0
                            ? torfLengthCounter / holesCount
                            : 0,

                        ["SandLength"] = holesCount > 0
                            ? sandLengthCounter / holesCount
                            : 0,

                        ["Grade"] = sandLengthCounter != 0
                            ? pureVertReservCounter / sandLengthCounter
                            : 0,
                    }
                });
            }

            return table;
        }

    }
}
