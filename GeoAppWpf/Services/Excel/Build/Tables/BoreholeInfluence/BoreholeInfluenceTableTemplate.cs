using System.Drawing;

namespace GeoAppWpf.Services.Excel.Build.Tables.BoreholeInfluence
{
    public static class BoreholeInfluenceTableTemplate
    {
        public static TableDefinition Create()
        {
            return new TableDefinition
            {
                Name = "Линейный журнал с нивел.",
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
                    ExcelStyles.DefaultPartSpliterMergeRule,

                    new TableMergeRule
                    {
                        ColumnKey = "GroupsNumber",
                        Horizontal = true,
                        CanMerge = row => row.Values.TryGetValue("IsRegionHeader", out var isRegion) && isRegion is true
                    },

                    new()
                    {
                        ColumnKey = "GroupsNumber",
                        Vertical = true,
                        SkipEmpty = true,

                        CanMerge = row =>
                        {
                            row.Values.TryGetValue(
                                "GroupsNumber",
                                out var value);

                            var text = value?.ToString();

                            return text != "ВСЕГО ПО ГРУППЕ" && text != "ЛИМИТ 10%";
                        }
                    },

                    new()
                    {
                        ColumnKey = "GroupsNumber",
                        EndColumnKey = "BoreholeNumber",
                        Horizontal = true,

                        CanMerge = row =>
                        {
                            row.Values.TryGetValue(
                                "GroupsNumber",
                                out var value);

                            var text = value?.ToString();

                            return text == "ВСЕГО ПО ГРУППЕ" || text == "ЛИМИТ 10%";
                        }
                    }
                ],

                RowStyleRules =
                [
                    ExcelStyles.DefaultPartSpliterRule,
                    new()
                    {
                        Condition = row =>
                        {
                            var text = row.Values.GetValueOrDefault("GroupsNumber")?.ToString();
                            return text == "ВСЕГО ПО ГРУППЕ" || text == "ЛИМИТ 10%";
                        },

                        Bold = true,
                        BackgroundColor = Color.FromArgb(255, 226, 239, 218),
                        Border = ExcelStyles.DefaultBorder
                    },

                    new()
                    {
                        Condition = row =>
                        {
                            var text = row.Values.GetValueOrDefault("BoreholeLine")?.ToString();
                            return text == "Итого по линии";
                        },

                        Bold = true,
                        Border = ExcelStyles.DefaultBorder
                    },

                    ExcelStyles.DefaultHighlightedCellStyle("Impact10"),
                ],

                Columns =
                [
                    new() { Header = "№ Группы", Key = "GroupsNumber" },
                    new() { Header = "№ Линии", Key = "BoreholeLine" },
                    new() { Header = "№ Скважины", Key = "BoreholeNumber" },
                    new() { Header = "Количество скважин", Key = "HolesCount" },
                    new() { Header = "Мощность, м", Key = "Length", Format = "F1" },
                    new() { Header = "Ср. сод-е Au х/ч, г/м³", Key = "Grade", Format = "F3" },
                    new() { Header = "Верт. запас х/ч, г/м²", Key = "VertReserv", Format = "F3" },
                    new() { Header = "Влияние скважины, %", Key = "Impact", Format = "F2" },
                    new() { Header = "С учетом нивелировки 10%, Ср. сод. Au х/ч, г/м³", Key = "Impact10", Format = "F3" },
                    new() { Header = "С учетом нивелировки 10%, Ср. сод. Au х/ч, г/м³", Key = "Impact10_2", Format = "F3" },
                ]
            };
        }
    }
}
