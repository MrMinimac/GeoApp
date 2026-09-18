using GeoAppCore;
using GeoAppWpf.Helpers;

namespace GeoAppWpf.Services.Excel.Build.Tables.BoreholeReportTable
{
    public static class BoreholeReportTableBuilder
    {
        public static TableDefinition Build(Borehole borehole)
        {
            int globalReisNumber = 1;

            var table = BoreholeReportTableTemplate.Create();
            table.HasHeader = false;

            int prohodkaNumber = 1;

            table.Rows.Add(new TableRow
            {
                Values = { ["From"] = $"Колонка скважины: № {borehole.Key}", }
            });

            table.Rows.Add(new TableRow
            {
                Values = { 
                    ["From"] = "Участок: р. Нюкжа", 
                    ["VolFact"] = "Координаты",
                }
            });

            table.Rows.Add(new TableRow
            {
                Values = 
                { 
                    //["From"] = "Начало бурения: 13.11.2025",

                    ["VolFact"] = "Широта",
                    ["CoreRecovery"] = "Долгота",
                    ["VisGold"] = "Высота",
                }
            });

            var coords = GaussKrugerConverter.GKToGeodetic(borehole.Y, borehole.X);

            table.Rows.Add(new TableRow
            {
                Values =
                { 
                    //["From"] = "Конец бурения: -", }

                    ["VolFact"] = $"{coords.Latitude:F4}",
                    ["CoreRecovery"] = $"{coords.Longitude:F4}",
                    ["VisGold"] = $"{borehole.Z:F1}",
                }
            });

            table.Rows.Add(new TableRow
            {
                Values = { ["From"] = $"Глубина скважины, м: {borehole.Deapth}", }
            });

            table.Rows.Add(new TableRow { Values = { } });

            table.Rows.Add(new TableRow
            {
                Values = 
                { 
                    ["From"] = $"Диаметр бурения", 
                    ["ProhodkaNum"] = $"начальный:",
                    ["Podoshva"] = "151 мм",
                },
            });

            table.Rows.Add(new TableRow
            {
                Values = 
                { 
                    ["ProhodkaNum"] = $"конечный: ", 
                    ["Podoshva"] = "132 мм",
                }
            });

            table.Rows.Add(new TableRow { Values = { } });

            table.Rows.Add(new TableRow
            {
                Values =
                {
                    ["From"] = $"Обсадка: диаметр, мм - ",
                    ["ProhodkaNum"] = $"151",
                    ["VolTeor"] = $"Глубина обсадки, м:",
                    ["CoreRecovery"] = "0.8",
                },
            });

            table.Rows.Add(new TableRow
            {
                Values =
                {
                    ["From"] = "Коронка, тип - ",
                    ["ProhodkaNum"] = "Твердосплавная СМ-6",
                    ["VolTeor"] = $"Диаметр коронки:",
                    ["CoreRecovery"] = "151",
                },
            });

            table.Rows.Add(new TableRow
            {
                Values =
                {
                    ["ReisNumber"] = "№ Рейса",

                    ["From"] = "Рейс, м",
                    ["To"] = "Рейс, м",
                    ["Length"] = "Рейс, м",

                    ["ProhodkaNum"] = "Проходка",
                    ["Podoshva"] = "Проходка",

                    ["VolTeor"] = "Объем пробы, см3",
                    ["VolFact"] = "Объем пробы, см3",
                }
            });

            table.Rows.Add(new TableRow
            {
                Values =
                {
                    ["ReisNumber"] = "№ Рейса",

                    ["From"] = "От",
                    ["To"] = "До",
                    ["Length"] = "Длина",

                    ["ProhodkaNum"] = "№",
                    ["Podoshva"] = "Подошва, м",

                    ["VolTeor"] = "Теор.",
                    ["VolFact"] = "Факт.",
                    ["CoreRecovery"] = "% выхода керна",

                    ["VisGold"] = "Визуальное определение золота",
                    ["GeoColumn"] = "Геологич. Колонка",
                    ["RockDesc"] = "Описание горных пород",
                }
            });

            Random random = new Random();

            foreach (var sample in borehole.Samples)
            {
                // Генерируем случайное число от 0.0 до 1.0, умножаем на 5 и прибавляем 90
                // Получаем диапазон от 90.0 до 95.0
                double coreRecovery = 90.0 + (random.NextDouble() * 5.0);

                double volTeor = 5471;

                // Вычисляем фактический объем (процент от теоретического)
                double volFact = volTeor * (coreRecovery / 100.0);

                table.Rows.Add(new TableRow
                {
                    Values =
                    {
                        ["ReisNumber"] = globalReisNumber++,
                        ["From"] = sample.From,
                        ["To"] = sample.To,
                        ["Length"] = sample.Length,

                        ["ProhodkaNum"] = prohodkaNumber++,
                        ["Podoshva"] = sample.To,

                        ["VolTeor"] = volTeor,
                        ["VolFact"] = Math.Truncate(volFact / 10.0) * 10.0,
                        ["CoreRecovery"] = coreRecovery,

                        ["VisGold"] = "пс",
                        ["GeoColumn"] = string.Empty,
                        ["RockDesc"] = string.Empty,
                    }
                });
            }

            return table;
        }
    }
}
