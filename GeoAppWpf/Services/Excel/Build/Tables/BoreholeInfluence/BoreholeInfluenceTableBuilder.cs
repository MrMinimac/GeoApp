using GeoAppWpf.Services.Excel.Build.Data;

namespace GeoAppWpf.Services.Excel.Build.Tables.BoreholeInfluence
{
    public static partial class BoreholeInfluenceTableBuilder
    {
        public static TableDefinition Build(IEnumerable<LevelingBoreholeGroup> levelingGroups)
        {
            var table = BoreholeInfluenceTableTemplate.Create();

            var macroGroupRows = BuildMacroGroupTable(levelingGroups);
            table.Rows.AddRange(macroGroupRows);

            table.CanUserSortColumns = false;
            return table;
        }

        private static List<TableRow> BuildMacroGroupTable(IEnumerable<LevelingBoreholeGroup> levelingGroups, int minHoles = 25, int maxHoles = 30, int concatMaxHoles = 35)
        {
            var rows = new List<TableRow>();

            foreach (var group in levelingGroups)
            {
                int totalHolesCount = 0;

                string boreholeLineCurrent = group.Items.FirstOrDefault()?.SectionBorehole.Source.BoreholeLineId ?? "";
                string regionCurrent = "";

                void AddLineSummaryRow(string lineId)
                {
                    var currentLineHoles = group.Items
                        .Where(x => x.SectionBorehole.Source.BoreholeLineId == lineId)
                        .ToList();

                    if (currentLineHoles.Count > 0)
                    {
                        var allLength = currentLineHoles.Sum(x => x.Thickness);
                        var allVertReserve = currentLineHoles.Sum(x => x.PureVertReserve);

                        rows.Add(new TableRow
                        {
                            Values =
                            {
                                ["GroupsNumber"] = group.GroupNumber,
                                ["BoreholeLine"] = "Итого по линии",
                                ["HolesCount"] = currentLineHoles.Count,
                                ["Length"] = allLength,
                                ["Grade"] = allLength != 0 ? allVertReserve / allLength : 0,
                                ["VertReserv"] = allVertReserve,
                                ["Impact"] = currentLineHoles.Sum(x => x.Impact)
                            }
                        });
                    }
                }

                foreach (var hole in group.Items)
                {
                    var newRegion = hole.SectionBorehole.Source.Atributes
                        .GetValueOrDefault("Участок")?
                        .ToString() ?? "";

                    if (regionCurrent != newRegion)
                    {
                        rows.Add(new TableRow
                        {
                            Values =
                            {
                                ["GroupsNumber"] = newRegion,
                                ["IsRegionHeader"] = true
                            }
                        });

                        regionCurrent = newRegion;
                    }

                    // Если линия изменилась — подводим итого по предыдущей линии
                    if (boreholeLineCurrent != hole.SectionBorehole.Source.BoreholeLineId)
                    {
                        AddLineSummaryRow(boreholeLineCurrent);
                        boreholeLineCurrent = hole.SectionBorehole.Source.BoreholeLineId;
                    }


                    totalHolesCount++;

                    rows.Add(new TableRow
                    {
                        Values =
                        {
                            ["GroupsNumber"] = group.GroupNumber,
                            ["BoreholeLine"] = hole.SectionBorehole.Source.BoreholeLineId,
                            ["BoreholeNumber"] = hole.SectionBorehole.Id,
                            ["HolesCount"] = totalHolesCount,
                            ["Length"] = hole.Thickness,
                            ["Grade"] = hole.PureAvgGrade,
                            ["VertReserv"] = hole.PureVertReserve,
                            ["Impact"] = hole.Impact,

                            ["Impact10"] = hole.ImpactLeveling10,
                            ["Impact10_2"] = hole.ImpactLeveling10 != null ? group.PureVertReserveLeveling10 : null,
                        }
                    });
                }

                // Подводим итого по последней линии после завершения цикла
                if (!string.IsNullOrEmpty(boreholeLineCurrent))
                {
                    AddLineSummaryRow(boreholeLineCurrent);
                }

                // Добавляем строки итогов по группе
                rows.Add(new TableRow
                {
                    Values =
                    {
                        ["GroupsNumber"] = "ВСЕГО ПО ГРУППЕ",
                        ["HolesCount"] = totalHolesCount,
                        ["Length"] = group.Thickness,
                        ["Grade"] = group.PureAvgGrade,
                        ["VertReserv"] = group.PureVertReserve,
                        ["Impact"] = group.Impact
                    },
                    Highlight = true
                });

                rows.Add(new TableRow
                {
                    Values =
                    {
                        ["GroupsNumber"] = "ЛИМИТ 10%",
                        ["VertReserv"] = group.PureVertReserveLeveling10,
                    },
                    Highlight = true
                });
            }

            return rows;
        }
        
    }
}
