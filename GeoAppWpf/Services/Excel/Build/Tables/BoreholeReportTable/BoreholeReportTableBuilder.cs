using GeoAppCore;
using GeoAppWpf.Helpers;
using GeoAppWpf.Services.Excel.Render;
using System.Diagnostics;
using System.Globalization;

namespace GeoAppWpf.Services.Excel.Build.Tables.BoreholeReportTable
{
    public static class BoreholeReportTableBuilder
    {
        private record GeoLayer(
            string Name,
            double Start,
            double End,
            HatchConfig Hatch);

        public static TableDefinition Build(Borehole borehole, string name)
        {
            int globalReisNumber = 1;

            var table = BoreholeReportTableTemplate.Create();
            table.Name = name.Length >= 2
                ? name[..2].ToUpper() + name[2..]
                : name.ToUpper();

            table.HasHeader = false;

            int prohodkaNumber = 1;

            table.Rows.Add(new TableRow
            {
                Values =
                {
                    ["From"] = $"Колонка скважины: № {table.Name}",
                    ["VolTeor"] = "Координаты ГСК 2011",
                }
            });

            table.Rows.Add(new TableRow
            {
                Values =
                {
                    ["From"] = $"Участок: {borehole.Atributes.GetValueOrDefault("Участок")?.ToString() ?? "-"}",
                    ["VolTeor"] = "Широта",
                    ["VolFact"] = "Долгота",
                    ["CoreRecovery"] = "Высота",
                }
            });

            var coords = GaussKrugerConverter.GKToGeodetic(borehole.Y, borehole.X);

            table.Rows.Add(new TableRow
            {
                Values =
                {
                    ["VolTeor"] = $"{coords.Latitude:F4}",
                    ["VolFact"] = $"{coords.Longitude:F4}",
                    ["CoreRecovery"] = $"{borehole.Z:F1}",
                }
            });

            table.Rows.Add(new TableRow { Values = { } });

            table.Rows.Add(new TableRow
            {
                Values =
                {
                    ["From"] = $"Глубина скважины, м: {borehole.Deapth}",
                }
            });

            table.Rows.Add(new TableRow
            {
                Values =
                {
                    ["VolFact"] = "Пройдено наносами",
                    ["GeoColumn"] = borehole.Atributes.GetValueOrDefault("Наносы"),
                    ["RockDesc"] = "м",
                }
            });

            table.Rows.Add(new TableRow
            {
                Values =
                {
                    ["From"] = $"Диаметр бурения",
                    ["ProhodkaNum"] = $"начальный:",
                    ["Podoshva"] = "151 мм",

                    ["VolFact"] = "Разрушен. коренными породами",
                    ["GeoColumn"] = borehole.Atributes.GetValueOrDefault("РКП"),
                    ["RockDesc"] = "м",
                },
            });

            table.Rows.Add(new TableRow
            {
                Values =
                {
                    ["ProhodkaNum"] = $"конечный: ",
                    ["Podoshva"] = "132 мм",

                    ["VolFact"] = "Плотными коренными породами",
                    ["GeoColumn"] = borehole.Atributes.GetValueOrDefault("ПКП"),
                    ["RockDesc"] = "м",
                }
            });

            table.Rows.Add(new TableRow { Values = { } });

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

            var desc = GetRockDesc(borehole);

            // Подготавливаем интервалы слоев заранее
            var geoLayers = ParseGeoLayers(borehole);

            double minDepth = Convert.ToDouble(borehole.Samples.First().From);
            double maxDepth = Convert.ToDouble(borehole.Samples.Last().To);

            var bands = geoLayers
                    .OrderBy(l => l.Start)
                    .Select(l => (l.Start, l.End, l.Hatch))
                    .ToList();

            var columnHatch = new GeoColumnHatchConfig(bands, minDepth, maxDepth);

            foreach (var sample in borehole.Samples)
            {
                double coreRecovery = 90.0 + (random.NextDouble() * 5.0);
                double volTeor = 5471;
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
                        ["GeoColumn"] = columnHatch,
                        ["RockDesc"] = desc,
                    },
                    Style = new TableRowStyle
                    {
                        Border = ExcelStyles.AllSidesBorder,
                    },
                });
            }

            var emptyLines = 27 - borehole.Samples.Count;

            for (int i = 0; i < emptyLines; i++)
            {
                table.Rows.Add(new TableRow
                {
                    Values = { },
                    Style = new TableRowStyle
                    {
                        Border = ExcelStyles.AllSidesBorder,
                    },
                });
            }

            table.Rows.Add(new TableRow
            {
                Values =
                {
                    ["GeoColumn"] = "Геолог:",
                },
            });

            return table;
        }

        private static string GetRockDesc(Borehole borehole)
        {
            var result = new List<(double From, string Text)>();

            AddDescription("ПРС", "ПРС Интервал");
            AddDescription("Делювий", "Делювий Интервал");
            AddDescription("Торф", "Торф Интервал");
            AddDescription("Аллювий", "Аллювий Интервал");
            AddDescription("РКП", "РКП Интервал");
            AddDescription("ПКП", "ПКП Интервал");

            return string.Join(
                "\n",
                result
                    .OrderBy(x => x.From)
                    .Select(x => x.Text));

            void AddDescription(string type, string intervalKey)
            {
                var interval = borehole.Atributes
                    .GetValueOrDefault(intervalKey)?
                    .ToString();

                if (string.IsNullOrWhiteSpace(interval))
                    return;

                var description = GetRandomDescription(
                    borehole,
                    $"{type} Описание");

                if (string.IsNullOrWhiteSpace(description))
                    return;

                var normalized = NormalizeInterval(interval);

                // Берём начало интервала
                if (!TryGetIntervalStart(normalized, out var from))
                    return;

                result.Add((from, $"{normalized}: {description}"));
            }

            static bool TryGetIntervalStart(string interval, out double from)
            {
                from = 0;

                var parts = interval.Split('-', StringSplitOptions.TrimEntries);

                var firstPart = parts[0].Replace(",", ".");

                Debug.WriteLine(firstPart);

                return parts.Length >= 1 &&
                       double.TryParse(
                           firstPart,
                           System.Globalization.NumberStyles.Float,
                           System.Globalization.CultureInfo.InvariantCulture,
                           out from);
            }
        }

        private static List<GeoLayer> ParseGeoLayers(Borehole borehole)
        {
            var layers = new List<GeoLayer>();

            var layerKeys = new[]
            {
                ("ПРС", "ПРС Интервал"),
                ("Делювий", "Делювий Интервал"),
                ("Торф", "Торф Интервал"),
                ("Аллювий", "Аллювий Интервал"),
                ("РКП", "РКП Интервал"),
                ("ПКП", "ПКП Интервал")
            };

            foreach (var (type, key) in layerKeys)
            {
                var intervalStr =
                    borehole.Atributes.GetValueOrDefault(key)?.ToString();

                if (!TryParseInterval(
                        intervalStr,
                        out double start,
                        out double end))
                {
                    continue;
                }

                if (!GeometriesData.LayerHatches.TryGetValue(type, out var hatch))
                    continue;

                layers.Add(
                    new GeoLayer(
                        type,
                        start,
                        end,
                        hatch));
            }

            return layers;
        }

        private static string NormalizeInterval(string value)
        {
            if (TryParseInterval(value, out double start, out double end))
            {
                return $"{start:0.0}-{end:0.0} м.";
            }
            return value?.Trim() ?? string.Empty;
        }

        private static bool TryParseInterval(string value, out double start, out double end)
        {
            start = 0;
            end = 0;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            var parts = value.Split('-', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
                return false;

            return TryParseDepth(parts[0], out start) && TryParseDepth(parts[1], out end);
        }

        private static bool TryParseDepth(string value, out double depth)
        {
            value = value.Replace(',', '.');

            var number = new string(
                value
                    .Trim()
                    .TakeWhile(c => char.IsDigit(c) || c == '.')
                    .ToArray());

            return double.TryParse(
                number,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out depth);
        }

        private static readonly Random Random = new();

        private static string GetRandomDescription(Borehole borehole, string key)
        {
            if (!borehole.Atributes.TryGetValue(key, out var value))
                return string.Empty;

            if (value is not List<string> descriptions || descriptions.Count == 0)
                return string.Empty;

            return descriptions[Random.Next(descriptions.Count)];
        }
    }
}