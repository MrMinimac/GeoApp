using GeoAppCore;
using GeoAppCore.Abstractions.Document;
using netDxf.Entities;
using OfficeOpenXml;
using System.Globalization;
using System.IO;
using System.Windows;

namespace GeoAppWpf.Services
{
    public enum TableType
    {
        Unknown,
        BoreholesAndSamplesData,
        BoreholesData,
    }

    public class ExcelReader
    {
        public static IDocument ReadExcelDocument(string path)
        {
            var document = new ExcelDocument();
            document.FilePath = path;
            document.Name = Path.GetFileNameWithoutExtension(path);

            try
            {
                using var package = new ExcelPackage(new FileInfo(path));

                foreach (var ws in package.Workbook.Worksheets)
                {
                    if (ws.Dimension == null)
                        continue;

                    // 1. Формируем карту колонок по первой строке (Заголовок -> Индекс)
                    var columnMap = GetColumnMap(ws);

                    // 2. Определяем тип таблицы на основе найденных колонок
                    var tableType = DetectTableType(columnMap);

                    switch (tableType)
                    {
                        case TableType.BoreholesAndSamplesData:
                            {
                                var boreholes = LoadBoreholesWithSamples(ws, columnMap);

                                if (boreholes == null || !boreholes.Any())
                                    continue;

                                var lines = boreholes
                                    .GroupBy(x => x.BoreholeLineId)
                                    .Select(x => new BoreholeLine
                                    {
                                        Id = x.Key,
                                        Boreholes = x.OrderBy(b => b.Id).ToList()
                                    })
                                    .ToList();

                                document.Objects.AddRange(lines);
                                break;
                            }
                        case TableType.BoreholesData:
                            {
                                var boreholes = LoadBoreholes(ws, columnMap);

                                if (boreholes == null || !boreholes.Any())
                                    continue;

                                var lines = boreholes
                                    .GroupBy(x => x.BoreholeLineId)
                                    .Select((x) => new BoreholeLine
                                    {
                                        Id = x.Key,
                                        Boreholes = x.OrderBy(b => b.Id).ToList(),
                                    })
                                    .ToList();

                                document.Objects.AddRange(lines);
                                break;
                            }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            return document;
        }

        private static Dictionary<string, int> GetColumnMap(ExcelWorksheet ws)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int colCount = ws.Dimension.End.Column;

            for (int i = 1; i <= colCount; i++)
            {
                var headerText = ws.Cells[1, i].Text?.Trim();
                if (!string.IsNullOrWhiteSpace(headerText) && !map.ContainsKey(headerText))
                {
                    map[headerText] = i;
                }
            }

            return map;
        }

        private static TableType DetectTableType(Dictionary<string, int> columns)
        {
            // Если есть колонки, характерные для проб
            if (columns.ContainsKey("От") || columns.ContainsKey("До") || columns.ContainsKey("Содержание"))
                return TableType.BoreholesAndSamplesData;

            // Если есть хотя бы Ключ и базовые координаты X, Y
            if (columns.ContainsKey("БЛ") && (columns.ContainsKey("X") || columns.ContainsKey("Y")))
                return TableType.BoreholesData;

            return TableType.Unknown;
        }

        private static List<Borehole>? LoadBoreholes(ExcelWorksheet worksheet, Dictionary<string, int> cols)
        {
            var boreholes = new List<Borehole>();

            int lineCol = GetColIndex(cols, "БЛ", "Key");
            int idCol = GetColIndex(cols, "№ Скв", "ID", "Скважина");
            int xCol = GetColIndex(cols, "X");
            int yCol = GetColIndex(cols, "Y");
            int zCol = GetColIndex(cols, "Z");
            int depthCol = GetColIndex(cols, "Длина", "Глубина", "Depth");
            int regionName = GetColIndex(cols, "Участок");
            int sediments = GetColIndex(cols, "Наносы");
            int rkp = GetColIndex(cols, "РКП");
            int pkp = GetColIndex(cols, "ПКП");

            var descAtributes = GetDescriptionsAtributes(worksheet, cols);

            if (lineCol == -1) return boreholes; // Без ключа чтение невозможно

            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                string line = worksheet.Cells[row, lineCol].Text;
                if (string.IsNullOrWhiteSpace(line)) continue;

                var currentBorehole = new Borehole
                {
                    BoreholeLineId = line,
                    Id = TryGetIntSafe(worksheet, row, idCol) ?? 0,
                    X = ParseDoubleSafe(worksheet, row, xCol),
                    Y = ParseDoubleSafe(worksheet, row, yCol),
                    Z = ParseDoubleSafe(worksheet, row, zCol),
                    Deapth = ParseDoubleSafe(worksheet, row, depthCol),
                };

                addAttribute("Участок", GetCellTextSafe(worksheet, row, regionName), regionName);
                addAttribute("Наносы", GetCellTextSafe(worksheet, row, sediments), sediments);
                addAttribute("РКП", GetCellTextSafe(worksheet, row, rkp), rkp);
                addAttribute("ПКП", GetCellTextSafe(worksheet, row, pkp), pkp);

                var intervals = GetIntervalAttributes(worksheet, cols, row);

                AddAttributes(currentBorehole.Atributes, intervals);
                AddAttributes(currentBorehole.Atributes, descAtributes);

                boreholes.Add(currentBorehole);

                void addAttribute(string key, object? obj, int col)
                {
                    if (col == -1)
                        return;

                    currentBorehole.Atributes.Add(key, obj);
                }
            }

            return boreholes;
        }

        private static List<Borehole>? LoadBoreholesWithSamples(ExcelWorksheet worksheet, Dictionary<string, int> cols)
        {
            Borehole currentBorehole = null;
            var boreholes = new List<Borehole>();
            string oldkey = "";

            int lineCol = GetColIndex(cols, "БЛ", "Линия");
            int idCol = GetColIndex(cols, "№ Скв");
            int xCol = GetColIndex(cols, "X");
            int yCol = GetColIndex(cols, "Y");
            int zCol = GetColIndex(cols, "Z");

            int fromCol = GetColIndex(cols, "От");
            int toCol = GetColIndex(cols, "До");
            int lengthCol = GetColIndex(cols, "Длина");
            int valueCol = GetColIndex(cols, "Содержание");
            int lithoCol = GetColIndex(cols, "Литология");

            int diametrCol = GetColIndex(cols, "Диаметр");
            int finenessCol = GetColIndex(cols, "Пробность");
            int scaleCol = GetColIndex(cols, "Масштаб");

            int regionName = GetColIndex(cols, "Участок");
            int sediments = GetColIndex(cols, "Наносы");
            int rkp = GetColIndex(cols, "РКП");
            int pkp = GetColIndex(cols, "ПКП");

            // Единичные значения для всего файла (если нужно считывать только со 2 строки)
            var diametr = TryGetDoubleSafe(worksheet, 2, diametrCol);
            var fineness = TryGetDoubleSafe(worksheet, 2, finenessCol);
            var vScale = TryGetDoubleSafe(worksheet, 2, scaleCol);

            var descAtributes = GetDescriptionsAtributes(worksheet, cols);

            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                string key = $"{worksheet.Cells[row, lineCol].Text}_{worksheet.Cells[row, idCol].Text}";

                if (!string.IsNullOrWhiteSpace(key) && key != oldkey)
                {
                    oldkey = key;
                    currentBorehole = new Borehole
                    {
                        BoreholeLineId = worksheet.Cells[row, lineCol].Text,
                        Id = TryGetIntSafe(worksheet, row, idCol) ?? 0,
                        X = ParseDoubleSafe(worksheet, row, xCol),
                        Y = ParseDoubleSafe(worksheet, row, yCol),
                        Z = ParseDoubleSafe(worksheet, row, zCol),
                    };

                    addAttribute("Участок", GetCellTextSafe(worksheet, row, regionName), regionName);
                    addAttribute("Наносы", GetCellTextSafe(worksheet, row, sediments), sediments);
                    addAttribute("РКП", GetCellTextSafe(worksheet, row, rkp), rkp);
                    addAttribute("ПКП", GetCellTextSafe(worksheet, row, pkp), pkp);

                    var intervals = GetIntervalAttributes(worksheet, cols, row);

                    AddAttributes(currentBorehole.Atributes, intervals);
                    AddAttributes(currentBorehole.Atributes, descAtributes);

                    boreholes.Add(currentBorehole);

                    void addAttribute(string key, object? obj, int col)
                    {
                        if (col == -1)
                            return;

                        currentBorehole.Atributes.Add(key, obj);
                    }
                }

                if (currentBorehole != null)
                {
                    Sample sample = new Sample();

                    // Если у пробы нет своих X,Y,Z (ячейки пустые), fallback берет координаты скважины
                    sample.X = ParseDoubleSafe(worksheet, row, xCol);
                    sample.Y = ParseDoubleSafe(worksheet, row, yCol);
                    sample.Z = ParseDoubleSafe(worksheet, row, zCol);

                    sample.From = TryGetDoubleSafe(worksheet, row, fromCol) ?? 0;
                    sample.To = TryGetDoubleSafe(worksheet, row, toCol) ?? 0;
                    sample.Length = TryGetDoubleSafe(worksheet, row, lengthCol) ?? 0;
                    sample.Grade = ParseDoubleSafe(worksheet, row, valueCol);

                    sample.Diametr = diametr ?? 0;
                    sample.Fineness = fineness ?? 0;

                    if (lithoCol != -1)
                    {
                        sample.Lithologies = ParseLithology(worksheet, row, lithoCol);
                    }

                    currentBorehole.Samples.Add(sample);
                }
            }

            return boreholes;
        }

        private static Dictionary<string, object?> GetIntervalAttributes(ExcelWorksheet worksheet, Dictionary<string, int> cols, int row)
        {
            var atributes = new Dictionary<string, object?>();

            int prsRange = GetColIndex(cols, "ПРС Интервал");
            int delRange = GetColIndex(cols, "Делювий Интервал");
            int torfRange = GetColIndex(cols, "Торф Интервал");
            int alRange = GetColIndex(cols, "Аллювий Интервал");
            int rkpRange = GetColIndex(cols, "РКП Интервал");
            int pkpRange = GetColIndex(cols, "ПКП Интервал");

            add("ПРС Интервал", prsRange);
            add("Делювий Интервал", delRange);
            add("Торф Интервал", torfRange);
            add("Аллювий Интервал", alRange);
            add("РКП Интервал", rkpRange);
            add("ПКП Интервал", pkpRange);

            void add(string key, int col)
            {
                if (col == -1)
                    return;

                atributes.Add(key, GetCellTextSafe(worksheet, row, col));
            }

            return atributes;
        }

        private static Dictionary<string, object?> GetDescriptionsAtributes(ExcelWorksheet worksheet, Dictionary<string, int> cols)
        {
            var atributes = new Dictionary<string, object?>();

            int prsDesc = GetColIndex(cols, "ПРС Описание");
            int delDesc = GetColIndex(cols, "Делювий Описание");
            int torfDesc = GetColIndex(cols, "Торф Описание");
            int alDesc = GetColIndex(cols, "Аллювий Описание");
            int rkpDesc = GetColIndex(cols, "РКП Описание");
            int pkpDesc = GetColIndex(cols, "ПКП Описание");

            var descriptions = new Dictionary<string, List<string>>
            {
                ["ПРС"] = [],
                ["Делювий"] = [],
                ["Торф"] = [],
                ["Аллювий"] = [],
                ["РКП"] = [],
                ["ПКП"] = []
            };

            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                AddDescription(worksheet, row, prsDesc, descriptions["ПРС"]);
                AddDescription(worksheet, row, delDesc, descriptions["Делювий"]);
                AddDescription(worksheet, row, torfDesc, descriptions["Торф"]);
                AddDescription(worksheet, row, alDesc, descriptions["Аллювий"]);
                AddDescription(worksheet, row, rkpDesc, descriptions["РКП"]);
                AddDescription(worksheet, row, pkpDesc, descriptions["ПКП"]);
            }

            add("ПРС Описание", descriptions["ПРС"], prsDesc);
            add("Делювий Описание", descriptions["Делювий"], delDesc);
            add("Торф Описание", descriptions["Торф"], torfDesc);
            add("Аллювий Описание", descriptions["Аллювий"], alDesc);
            add("РКП Описание", descriptions["РКП"], rkpDesc);
            add("ПКП Описание", descriptions["ПКП"], pkpDesc);

            return atributes;

            void add(string key, object obj, int column)
            {
                if (column == -1)
                    return;

                atributes.Add(key, obj);
            }
        }

        private static void AddAttributes(Dictionary<string, object?> target, Dictionary<string, object?> source)
        {
            foreach (var pair in source)
                target[pair.Key] = pair.Value;
        }

        private static void AddDescription(ExcelWorksheet worksheet, int row, int column, List<string> descriptions)
        {
            if (column == -1)
                return;

            var value = GetCellTextSafe(worksheet, row, column);

            if (!string.IsNullOrWhiteSpace(value) && !descriptions.Contains(value))
                descriptions.Add(value);
        }

        private static int GetColIndex(Dictionary<string, int> map, params string[] possibleNames)
        {
            foreach (var name in possibleNames)
            {
                if (map.TryGetValue(name, out int index))
                    return index;
            }
            return -1; // Если ни одно из возможных имен колонок не найдено
        }

        private static string? GetCellTextSafe(ExcelWorksheet worksheet, int row, int column)
        {
            if (column < 1)
                return null;

            return worksheet.Cells[row, column].Text;
        }

        private static double ParseDoubleSafe(ExcelWorksheet ws, int row, int col, double fallback = 0)
        {
            if (col == -1) return fallback;
            return ParseDouble(ws.Cells[row, col].Text, fallback);
        }

        private static double? TryGetDoubleSafe(ExcelWorksheet ws, int row, int col)
        {
            if (col == -1) return null;

            var text = ws.Cells[row, col].Text;
            if (string.IsNullOrWhiteSpace(text)) return null;

            text = text.Replace(",", ".");
            if (double.TryParse(text, CultureInfo.InvariantCulture, out double result))
                return result;

            return null;
        }

        private static int? TryGetIntSafe(ExcelWorksheet ws, int row, int col)
        {
            if (col == -1) return null;

            var text = ws.Cells[row, col].Text;
            if (string.IsNullOrWhiteSpace(text)) return null;

            if (int.TryParse(text, out int result))
                return result;

            return null;
        }

        private static List<Lithology> ParseLithology(ExcelWorksheet ws, int row, int column)
        {
            var lithologies = new List<Lithology>();
            if (column == -1) return lithologies;

            var text = ws.Cells[row, column].Text;
            if (string.IsNullOrWhiteSpace(text)) return lithologies;

            var ids = text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var id in ids)
            {
                if (int.TryParse(id.Trim(), out int value) && Enum.IsDefined(typeof(Lithology), value))
                {
                    lithologies.Add((Lithology)value);
                }
            }

            return lithologies;
        }

        private static double ParseDouble(string value, double fallback = 0)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            string lowerValue = value.ToLower().Trim();
            if (lowerValue == "зн")
                return -1;
            if (lowerValue == "пс")
                return 0;

            value = value.Replace(",", ".");
            if (double.TryParse(value, CultureInfo.InvariantCulture, out double result))
                return result;

            return fallback;
        }
    }
}