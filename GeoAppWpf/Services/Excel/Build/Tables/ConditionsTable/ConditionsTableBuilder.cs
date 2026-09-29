using GeoAppCore;
using GeoAppWpf.Helpers;
using netDxf.Collections;
using System.Drawing;

namespace GeoAppWpf.Services.Excel.Build.Tables.ConditionsTable
{
    public class ConditionsTableTemplate
    {
        public static TableDefinition Create()
        {
            var headerStyle = ExcelStyles.DefaultHeader;
            headerStyle.Height = 135;

            return new TableDefinition
            {
                Name = "Кондиц-сть выроботок",
                ShowGridLines = true,
                AutoFitColumns = true,
                HasHeader = true,
                HeaderStyle = headerStyle,

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

                Columns =
                [
                    new() { Header = "№ буровой линии, год проход-ки", Key = "BoreholeLine" },
                    new() { Header = "№ скважины", Key = "BoreholeNumber" },
                    new() { Header = "Мощ-ность торфов, м", Key = "TorfThickness", Format = "F1" },
                    new() { Header = "Мощ-ность песков, м", Key = "SandThickness", Format = "F1" },
                    new() { Header = "Среднее сод-е на пласт, г/м3 (шлих)", Key = "PlastAvgGrade", Format = "F3" },
                    new() { Header = "Коэф. вскрыши", Key = "StripRatio", Format = "F1" },
                    new() { Header = "Гради-ент на ед. вскрыши", Key = "WasteGradient", Format = "F3" },
                    new() { Header = "Пробность золота", Key = "Fineness", Format = "F3" },
                    new() { Header = "Среднее сод-е по скважине, выработке г/м3 (х/ч)", Key = "BhAvgGrade", Format = "F3" },
                    new() { Header = "Минимальное содержание для оконтурива-ющей выработки, г/м3 (х/ч)", Key = "MinGrade", Format = "F3" },
                    new() { Header = "Соответ-ствие конди-ционным параметрам", Key = "ConditionResult" },
                    new() { Header = "Морфологогенетический тип россыпи", Key = "MorfType" },
                ],

                MergeRules =
                [
                    new()
                    {
                        ColumnKey = "BoreholeLine",
                        Vertical = true,
                        SkipEmpty = true,

                        CanMerge = row =>
                        {
                            return true;
                        }
                    },
                ],

                RowStyleRules =
                [
                    new()
                    {
                        ColumnKey = "ConditionResult",
                        Condition = row =>
                        {
                            row.Values.TryGetValue("ConditionResult", out var value);
                            return value?.ToString()?.Contains("в подсчет", StringComparison.InvariantCultureIgnoreCase) ?? false;
                        },
                        BackgroundColor = Color.FromArgb(198, 239, 206),
                        Border = new TableBorderStyle
                        {
                            Top = true,
                            Bottom = true,
                            Left = true,
                            Right = true,
                            Color = Color.Black
                        }
                    }
                ],
            };
        }
    }

    public class ConditionsTableBuilder
    {
        public static TableDefinition Build(IEnumerable<BoreholeLine> boreholeLines)
        {
            var table = ConditionsTableTemplate.Create();

            var sortedBoreholeLines = boreholeLines
               .OrderBy(w => w.Id.Any(char.IsDigit) ? 0 : 1)
               .ThenBy(w => w.Id, new NaturalNameComparer())
               .ToList();

            foreach (var line in sortedBoreholeLines)
            {
                var cbhs = line.BuildSections();

                foreach (var cbh in cbhs)
                {
                    var valid = cbh.OreInterval?.ConditionResult.IsValid ?? false;

                    if (!valid) continue;

                    var torfThick = cbh.OreInterval?.From ?? 0;
                    var sandThick = cbh.OreInterval?.Thinkness ?? 0;
                    var stripRatio = torfThick / sandThick;
                    var fineness = cbh.Source.Samples.FirstOrDefault()?.Fineness ?? 1;

                    table.Rows.Add(new()
                    {
                        Values =
                        {
                            ["BoreholeLine"] = line.Id,
                            ["BoreholeNumber"] = cbh.Source.Id,
                            ["TorfThickness"] = torfThick,
                            ["SandThickness"] = sandThick,
                            ["PlastAvgGrade"] = cbh.OreInterval?.AvgGrade ?? 0,
                            ["StripRatio"] = stripRatio,
                            ["WasteGradient"] = cbh.OreCondition.WasteGradient,
                            ["Fineness"] = fineness,
                            ["BhAvgGrade"] = cbh.OreInterval?.PureAvgGrade ?? 0,
                            ["MinGrade"] = cbh.OreInterval?.ConditionResult.MinGradeRequired ?? 0,
                            ["ConditionResult"] =  "В подсчет",
                            ["MorfType"] = "",
                        }
                    });
                }
            }

            return table;
        }
    }
}
