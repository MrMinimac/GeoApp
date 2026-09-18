using System.Drawing;

namespace GeoAppWpf.Services.Excel.Build.Tables.BoreholeReportTable
{
    public class BoreholeReportTableTemplate
    {
        public static TableDefinition Create()
        {
            string[] headerValues = ["Колонка скважины", "Участок", "Начало бурения", "Конец бурения", "Глубина скважины", "Диаметр", "Обсадка", "Коронка"];

            return new TableDefinition
            {
                Name = "Буровой журнал",
                ShowGridLines = true,
                AutoFitColumns = true,
                HasHeader = true,
                HeaderStyle = ExcelStyles.DefaultHeader,

                Columns =
                [
                    new() { Header = "№ Рейса", Key = "ReisNumber", Width = 10 },
                    new() { Header = "от", Key = "From", Format = "F2", Width = 10 },
                    new() { Header = "до", Key = "To", Format = "F2", Width = 10 },
                    new() { Header = "длина", Key = "Length", Format = "F2", Width = 10 },
                    new() { Header = "№", Key = "ProhodkaNum", Width = 10 },
                    new() { Header = "подошва, м", Key = "Podoshva", Format = "F2", Width = 10 },
                    new() { Header = "теор.", Key = "VolTeor", Width = 10 },
                    new() { Header = "факт.", Key = "VolFact", Width = 10 },
                    new() { Header = "% выхода керна", Key = "CoreRecovery", Format = "F1", Width = 10 },
                    new() { Header = "Визуальное определение золота", Key = "VisGold", Width = 10 },
                    new() { Header = "Геологич. Колонка", Key = "GeoColumn", Width = 10 },
                    new() { Key = "RockDesc", Width = 40 }
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
                            return text?.Contains("№ Рейса", StringComparison.InvariantCultureIgnoreCase) ?? false;
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

                            return text?.Contains("Объем пробы", StringComparison.InvariantCultureIgnoreCase) ?? false;
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
                            bool contains(string _text) => text?.Contains(_text, StringComparison.InvariantCultureIgnoreCase) ?? false;
                            return headerValues.Any(x => contains(x));
                        },
                        HorizontalAlignment = TableHorizontalAlignment.Left
                    },
                    new()
                    {
                        ColumnKey = "VolFact",
                        EndColumnKey = "VisGold",
                        Horizontal = true,

                        CanMerge = row =>
                        {
                            row.Values.TryGetValue("VolFact", out var value);
                            var text = value?.ToString();
                            return text?.Contains("Координаты", StringComparison.InvariantCultureIgnoreCase) ?? false;
                        }
                    },
                    new()
                    {
                        ColumnKey = "ProhodkaNum",
                        EndColumnKey = "Podoshva",
                        Horizontal = true,

                        CanMerge = row =>
                        {
                            row.Values.TryGetValue("From", out var value);
                            var text = value?.ToString();
                            return text?.Contains("Коронка", StringComparison.InvariantCultureIgnoreCase) ?? false;
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
                            return (text?.Contains("Глубина обсадки", StringComparison.InvariantCultureIgnoreCase) ?? false)
                             || (text?.Contains("Диаметр коронки", StringComparison.InvariantCultureIgnoreCase) ?? false);
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
                            return text?.Contains("рейс", StringComparison.InvariantCultureIgnoreCase) ?? false;
                        },

                        Bold = true,
                        BackgroundColor = Color.FromArgb(215, 215, 215),
                        Border = ExcelStyles.AllSidesBorder,
                        WrapText = true
                    },
                    //new()
                    //{
                    //    Condition = row =>
                    //    {
                    //        row.Values.TryGetValue("From", out var value);
                    //        var text = value?.ToString();
                    //        bool contains(string _text) => text?.Contains(_text, StringComparison.InvariantCultureIgnoreCase) ?? false;
                    //        return headerValues.Any(x => contains(x));
                    //    },

                    //    Bold = true,
                    //},
                ],
            };
        }
    }
}
