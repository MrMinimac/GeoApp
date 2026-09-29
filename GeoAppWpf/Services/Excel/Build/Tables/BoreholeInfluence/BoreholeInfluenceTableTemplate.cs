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

                HeaderMergeRules =
                [
                    new()
                    {
                        ColumnKey = "Impact10",
                        EndColumnKey = "Impact10_2"
                    },

                    new()
                    {
                        ColumnKey = "Impact15",
                        EndColumnKey = "Impact15_2"
                    },

                    new()
                    {
                        ColumnKey = "Impact20",
                        EndColumnKey = "Impact20_2"
                    }
                ],

                MergeRules =
                [
                    ExcelStyles.DefaultPartSpliterMergeRule,
                    new()
                    {
                        ColumnKey = "Wireframe",
                        Vertical = true,
                        SkipEmpty = true,

                        CanMerge = row =>
                        {
                            row.Values.TryGetValue(
                                "GroupsNumber",
                                out var value);

                            var text = value?.ToString();

                            return text != "ВСЕГО ПО ГРУППЕ" &&
                                   text != "ЛИМИТ 10%" &&
                                   text != "ЛИМИТ 15%" &&
                                   text != "ЛИМИТ 20%";
                        }
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

                            return text != "ВСЕГО ПО ГРУППЕ" &&
                                   text != "ЛИМИТ 10%" &&
                                   text != "ЛИМИТ 15%" &&
                                   text != "ЛИМИТ 20%";
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

                            return text == "ВСЕГО ПО ГРУППЕ" ||
                                   text == "ЛИМИТ 10%" ||
                                   text == "ЛИМИТ 15%" ||
                                   text == "ЛИМИТ 20%";
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
                            row.Values.TryGetValue("GroupsNumber", out var value);
                            var text = value?.ToString();

                            return text == "ВСЕГО ПО ГРУППЕ" ||
                                   text == "ЛИМИТ 10%" ||
                                   text == "ЛИМИТ 15%" ||
                                   text == "ЛИМИТ 20%";
                        },

                        Bold = true,
                        BackgroundColor = Color.FromArgb(189, 215, 238),
                        Border = ExcelStyles.DefaultBorder
                    },

                    ExcelStyles.DefaultHighlightedCellStyle("Impact10"),
                    ExcelStyles.DefaultHighlightedCellStyle("Impact15"),
                    ExcelStyles.DefaultHighlightedCellStyle("Impact20"),
                ],

                Columns =
                [
                    new() { Header = "№ Группы", Key = "GroupsNumber" },
                    new() { Header = "№ Линии", Key = "BoreholeLine" },
                    new() { Header = "№ Скважины", Key = "BoreholeNumber" },
                    new() { Header = "Количество скважин", Key = "HolesCount" },
                    new() { Header = "Мощность", Key = "Length", Format = "F1" },
                    new() { Header = "Содержание", Key = "Grade", Format = "F3" },
                    new() { Header = "Метрограмм", Key = "Grade2", Format = "F3" },
                    new() { Header = "Влияние скважины", Key = "Impact", Format = "F2" },
                    new() { Header = "С учетом уравнения 10%", Key = "Impact10", Format = "F3" },
                    new() { Header = "С учетом уравнения 10%", Key = "Impact10_2", Format = "F2" },
                    new() { Header = "С учетом уравнения 15%", Key = "Impact15", Format = "F3" },
                    new() { Header = "С учетом уравнения 15%", Key = "Impact15_2", Format = "F2" },
                    new() { Header = "С учетом уравнения 20%", Key = "Impact20", Format = "F3" },
                    new() { Header = "С учетом уравнения 20%", Key = "Impact20_2", Format = "F2" },
                ]
            };
        }
    }
}
