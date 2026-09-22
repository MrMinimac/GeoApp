using GeoAppWpf.Services.Excel.Render;
using System.Drawing;

namespace GeoAppWpf.Services.Excel.Build.Tables.BoreholeReportTable
{
    public class BoreholeReportTableTemplate
    {
        public static TableDefinition Create()
        {
            string[] headerValues = ["Участок", "Начало бурения", "Конец бурения", "Глубина скважины", "Обсадка", "Диаметр"];

            return new TableDefinition
            {
                Name = "Буровой журнал",
                ShowGridLines = true,
                AutoFitColumns = true,
                HasHeader = true,
                HeaderStyle = ExcelStyles.DefaultHeader,

                Columns =
                [
                    new() { Header = "№ Рейса", Key = "ReisNumber", Width = 10.5 },
                    new() { Header = "от", Key = "From", Format = "F2", Width = 10.5 },
                    new() { Header = "до", Key = "To", Format = "F2", Width = 10.5 },
                    new() { Header = "длина", Key = "Length", Format = "F2", Width = 10.5 },
                    new() { Header = "№", Key = "ProhodkaNum", Width = 10.5 },
                    new() { Header = "подошва, м", Key = "Podoshva", Format = "F2", Width = 10.5 },
                    new() { Header = "теор.", Key = "VolTeor", Width = 10.5 },
                    new() { Header = "факт.", Key = "VolFact", Width = 10.5 },
                    new() { Header = "% выхода керна", Key = "CoreRecovery", Format = "F1", Width = 10.5 },
                    new() { Header = "Визуальное определение золота", Key = "VisGold", Width = 14 },
                    new() { Header = "Геологич. Колонка", Key = "GeoColumn", Format = "F1", Width = 10 },
                    new() { Key = "RockDesc", Width = 48 }
                ],

                MergeRules =
                [
                    new()
                    {
                        ColumnKey = "ReisNumber",
                        Vertical = true,
                        SkipEmpty = true,

                        CanMerge = row =>
                        {
                            row.Values.TryGetValue("ReisNumber", out var value);
                            var text = value?.ToString();
                            return Contains(text, "№ Рейса");
                        },

                    },
                    new()
                    {
                        ColumnKey = "RockDesc",
                        Vertical = true,
                        SkipEmpty = true,

                        CanMerge = row =>
                        {
                            row.Values.TryGetValue("RockDesc", out var value);
                            var text = value?.ToString();
                            return text != "Описание горных пород" && text != "м";
                        },
                        HorizontalAlignment = TableHorizontalAlignment.Left,
                        VerticalAlignment = TableVerticalAlignment.Top,
                    },
                    new()
                    {
                        ColumnKey = "From",
                        EndColumnKey = "Length",
                        Horizontal = true,

                        CanMerge = row =>
                        {
                            row.Values.TryGetValue("From", out var value);
                            var text = value?.ToString();
                            return text == "Рейс, м";
                        }
                    },
                    new()
                    {
                        ColumnKey = "ProhodkaNum",
                        EndColumnKey = "Podoshva",
                        Horizontal = true,

                        CanMerge = row =>
                        {
                            row.Values.TryGetValue("ProhodkaNum", out var value);
                            var text = value?.ToString();
                            return text == "Проходка";
                        }
                    },
                    new()
                    {
                        ColumnKey = "VolTeor",
                        EndColumnKey = "VolFact",
                        Horizontal = true,

                        CanMerge = row =>
                        {
                            row.Values.TryGetValue("VolTeor", out var value);
                            var text = value?.ToString();
                            return Contains(text, "Объем пробы");
                        }
                    },
                    new()
                    {
                        ColumnKey = "From",
                        EndColumnKey = "Length",
                        Horizontal = true,

                        CanMerge = row =>
                        {
                            row.Values.TryGetValue("From", out var value);
                            var text = value?.ToString();
                            return headerValues.Any(x => Contains(text, x));
                        },
                        HorizontalAlignment = TableHorizontalAlignment.Left
                    },
                    new()
                    {
                        ColumnKey = "From",
                        EndColumnKey = "Length",
                        Horizontal = true,

                        CanMerge = row =>
                        {
                            row.Values.TryGetValue("From", out var value);
                            var text = value?.ToString();
                            return Contains(text, "Коронка");
                        },
                        HorizontalAlignment = TableHorizontalAlignment.Right
                    },
                    new()
                    {
                        ColumnKey = "From",
                        EndColumnKey = "ProhodkaNum",
                        Horizontal = true,

                        CanMerge = row =>
                        {
                            row.Values.TryGetValue("From", out var value);
                            var text = value?.ToString();
                            return Contains(text, "Колонка");
                        },
                        HorizontalAlignment = TableHorizontalAlignment.Left
                    },
                    new()
                    {
                        ColumnKey = "VolTeor",
                        EndColumnKey = "CoreRecovery",
                        Horizontal = true,

                        CanMerge = row =>
                        {
                            row.Values.TryGetValue("VolTeor", out var value);
                            var text = value?.ToString();
                            return Contains(text, "Координаты");
                        }
                    },
                    new()
                    {
                        ColumnKey = "ProhodkaNum",
                        EndColumnKey = "Podoshva",
                        Horizontal = true,
                        HorizontalAlignment = TableHorizontalAlignment.Right,

                        CanMerge = row =>
                        {
                            row.Values.TryGetValue("From", out var value);
                            var text = value?.ToString();
                            return Contains(text, "Коронка");
                        }
                    },
                    new()
                    {
                        ColumnKey = "VolTeor",
                        EndColumnKey = "VolFact",
                        Horizontal = true,
                        HorizontalAlignment = TableHorizontalAlignment.Right,

                        CanMerge = row =>
                        {
                            row.Values.TryGetValue("VolTeor", out var value);
                            var text = value?.ToString();
                            return Contains(text, "Глубина обсадки") || Contains(text, "Диаметр коронки");
                        }
                    },
                    new()
                    {
                        ColumnKey = "VolFact",
                        EndColumnKey = "VisGold",
                        Horizontal = true,
                        HorizontalAlignment = TableHorizontalAlignment.Right,

                        CanMerge = row =>
                        {
                            row.Values.TryGetValue("VolFact", out var value);
                            var text = value?.ToString();
                            return Contains(text, "Пройдено") || Contains(text, "корен");
                        }
                    },
                    new()
                    {
                        ColumnKey = "GeoColumn",
                        Vertical = true,
                        SkipEmpty = true,

                        CanMerge = row =>
                        {
                            row.Values.TryGetValue(
                                "GeoColumn",
                                out var value);

                            return value is HatchConfig;
                        }
                    }
                ],

                RowStyleRules =
                [
                    new()
                    {
                        Condition = row =>
                        {
                            row.Values.TryGetValue("ReisNumber", out var value);
                            var text = value?.ToString();
                            return Contains(text, "рейс");
                        },

                        Bold = true,
                        BackgroundColor = Color.FromArgb(215, 215, 215),
                        Border = ExcelStyles.AllSidesBorder,
                        WrapText = true
                    },
                    new()
                    {
                        ColumnKey = "RockDesc",
                        Condition = row =>
                        {
                            row.Values.TryGetValue("RockDesc", out var value);
                            var text = value?.ToString();

                            return text == "м";
                        },
                        HorizontalAlignment = TableHorizontalAlignment.Left
                    },
                    new()
                    {
                        ColumnKey = "RockDesc",
                        Condition = row =>
                        {
                            row.Values.TryGetValue("RockDesc", out var value);
                            var text = value?.ToString();
                            return text != "Описание горных пород" && text != "м";
                        },
                        WrapText = true,
                        HorizontalAlignment = TableHorizontalAlignment.Left,
                        VerticalAlignment = TableVerticalAlignment.Top,
                    },
                ],
            };
        }

        static bool Contains(string? text, string _text) => text?.Contains(_text, StringComparison.InvariantCultureIgnoreCase) ?? false;
    }
}
