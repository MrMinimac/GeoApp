using GeoAppCore;

namespace GeoAppWpf.Services.Excel.Build.Tables.BoreholeDataBaseTable
{
    public class BoreholeDataBaseTableBuilder
    {
        public static TableDefinition Build(IEnumerable<BoreholeLine> boreholeLines)
        {
            var table = new TableDefinition
            {
                Name = "База данных",
                ShowGridLines = true,
                AutoFitColumns = true,
                HasHeader = true,
                HeaderStyle = ExcelStyles.DefaultHeader,

                Columns =
                [
                    new() { Header = "БЛ", Key = "BoreholeLineId" },
                    new() { Header = "№ Скв", Key = "BoreholeId" },

                    new() { Header = "От", Key = "From", Format = "F2"},
                    new() { Header = "До", Key = "To", Format = "F2"},
                    new() { Header = "Длина", Key = "Length", Format = "F2" },

                    new() { Header = "Содержание", Key = "Grade", Format = "F3" },

                    new() { Header = "X", Key = "X", Format = "F3" },
                    new() { Header = "Y", Key = "Y", Format = "F3" },
                    new() { Header = "Z", Key = "Z", Format = "F3" },

                    new() { Header = "Участок", Key = "Region" },
                    new() { Header = "Наносы", Key = "Sediments" },
                    new() { Header = "РКП", Key = "RKP" },
                    new() { Header = "ПКП", Key = "PKP" },

                    new() { Key = "PRSRange", Header = "ПРС Интервал" },
                    new() { Key = "DelRange", Header = "Делювий Интервал" },
                    new() { Key = "TorfRange", Header = "Торф Интервал" },
                    new() { Key = "AlRange", Header = "Аллювий Интервал" },
                    new() { Key = "RKPRange", Header = "РКП Интервал" },
                    new() { Key = "PKPRange", Header = "ПКП Интервал" },

                    new() { Key = "PRSDesc", Header = "ПРС Описание" },
                    new() { Key = "DelDesc", Header = "Делювий Описание" },
                    new() { Key = "TorfDesc", Header = "Торф Описание" },
                    new() { Key = "AlDesc", Header = "Аллювий Описание" },
                    new() { Key = "RKPDesc", Header = "РКП Описание" },
                    new() { Key = "PKPDesc", Header = "ПКП Описание" },

                    new() { Header = "Диаметр", Key = "Diametr" },
                    new() { Header = "Пробность", Key = "Fineness" },
                ],
            };

            bool descriptionWriten = false;

            foreach (var line in boreholeLines)
            {
                foreach (var borehole in line.Boreholes)
                {
                    bool bhAtributesWriten = false;

                    foreach (var sample in borehole.Samples)
                    {
                        var row = new TableRow
                        {
                            Values =
                            {
                                ["BoreholeLineId"] = line.Id,
                                ["BoreholeId"] = borehole.Id,

                                ["From"] = sample.From,
                                ["To"] = sample.To,
                                ["Length"] = sample.Length,

                                ["Grade"] = sample.Grade,

                                ["X"] = sample.X,
                                ["Y"] = sample.Y,
                                ["Z"] = sample.Z,
                            },
                        };

                        if (!bhAtributesWriten)
                        {
                            row.Values.Add("Region", borehole.Atributes.GetValueOrDefault("Участок"));
                            row.Values.Add("Sediments", borehole.Atributes.GetValueOrDefault("Наносы"));

                            row.Values.Add("RKP", borehole.Atributes.GetValueOrDefault("РКП"));
                            row.Values.Add("PKP", borehole.Atributes.GetValueOrDefault("ПКП"));

                            row.Values.Add("PRSRange", borehole.Atributes.GetValueOrDefault("ПРС Интервал"));
                            row.Values.Add("DelRange", borehole.Atributes.GetValueOrDefault("Делювий Интервал"));
                            row.Values.Add("TorfRange", borehole.Atributes.GetValueOrDefault("Торф Интервал"));
                            row.Values.Add("AlRange", borehole.Atributes.GetValueOrDefault("Аллювий Интервал"));
                            row.Values.Add("RKPRange", borehole.Atributes.GetValueOrDefault("РКП Интервал"));
                            row.Values.Add("PKPRange", borehole.Atributes.GetValueOrDefault("ПКП Интервал"));

                            bhAtributesWriten = true;
                        }

                        if (!descriptionWriten)
                        {
                            row.Values.Add("PRSDesc", borehole.Atributes.GetValueOrDefault("ПРС Описание"));
                            row.Values.Add("DelDesc", borehole.Atributes.GetValueOrDefault("Делювий Описание"));
                            row.Values.Add("TorfDesc", borehole.Atributes.GetValueOrDefault("Торф Описание"));
                            row.Values.Add("AlDesc", borehole.Atributes.GetValueOrDefault("Аллювий Описание"));
                            row.Values.Add("RKPDesc", borehole.Atributes.GetValueOrDefault("РКП Описание"));
                            row.Values.Add("PKPDesc", borehole.Atributes.GetValueOrDefault("ПКП Описание"));

                            row.Values.Add("Diametr", sample.Diametr);
                            row.Values.Add("Fineness", sample.Fineness);

                            descriptionWriten = true;
                        }

                        table.Rows.Add(row);
                    }
                }
            }

            return table;
        }
    }
}
