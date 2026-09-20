using GeoAppCore;
using GeoAppCore.Abstractions.Document;
using OfficeOpenXml;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System;

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
                                    .GroupBy(x => x.LineNumber)
                                    .Select(x => new BoreholeLine
                                    {
                                        Number = x.Key,
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
                                    .GroupBy(x => x.Key)
                                    .Select(x => new BoreholeLine
                                    {
                                        Boreholes = x.OrderBy(b => b.Id).ToList()
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
            if (columns.ContainsKey("Ключ") && (columns.ContainsKey("X") || columns.ContainsKey("Y")))
                return TableType.BoreholesData;

            return TableType.Unknown;
        }

        public static List<Borehole>? LoadBoreholes(ExcelWorksheet worksheet, Dictionary<string, int> cols)
        {
            var boreholes = new List<Borehole>();

            int keyCol = GetColIndex(cols, "Ключ", "Key");
            int idCol = GetColIndex(cols, "№ Скв", "ID", "Скважина");
            int xCol = GetColIndex(cols, "X");
            int yCol = GetColIndex(cols, "Y");
            int zCol = GetColIndex(cols, "Z");
            int depthCol = GetColIndex(cols, "Длина", "Глубина", "Depth");

            int regionName = GetColIndex(cols, "Участок");
            int sediments = GetColIndex(cols, "Наносы");
            int rkp = GetColIndex(cols, "РКП");
            int pkp = GetColIndex(cols, "ПКП");

            if (keyCol == -1) return boreholes; // Без ключа чтение невозможно

            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                string key = worksheet.Cells[row, keyCol].Text;
                if (string.IsNullOrWhiteSpace(key)) continue;

                var currentBorehole = new Borehole
                {
                    Key = key,
                    Id = TryGetIntSafe(worksheet, row, idCol) ?? 0,
                    X = ParseDoubleSafe(worksheet, row, xCol),
                    Y = ParseDoubleSafe(worksheet, row, yCol),
                    Z = ParseDoubleSafe(worksheet, row, zCol),
                    Deapth = ParseDoubleSafe(worksheet, row, depthCol),
                    Atributes =
                    {
                        ["Участок"] = GetCellTextSafe(worksheet, row, regionName),
                        ["Наносы"] = GetCellTextSafe(worksheet, row, sediments),
                        ["РКП"] = GetCellTextSafe(worksheet, row, rkp),
                        ["ПКП"] = GetCellTextSafe(worksheet, row, pkp)
                    }
                };

                boreholes.Add(currentBorehole);
            }

            return boreholes;
        }

        public static List<Borehole>? LoadBoreholesWithSamples(ExcelWorksheet worksheet, Dictionary<string, int> cols)
        {
            Borehole currentBorehole = null;
            var boreholes = new List<Borehole>();
            string oldkey = "";

            int keyCol = GetColIndex(cols, "Ключ");
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
            int finenessCol = GetColIndex(cols, "Крупность");
            int scaleCol = GetColIndex(cols, "Масштаб");

            if (keyCol == -1) return boreholes;

            // Единичные значения для всего файла (если нужно считывать только со 2 строки)
            var diametr = TryGetDoubleSafe(worksheet, 2, diametrCol);
            var fineness = TryGetDoubleSafe(worksheet, 2, finenessCol);
            var vScale = TryGetDoubleSafe(worksheet, 2, scaleCol);

            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                string key = worksheet.Cells[row, keyCol].Text;

                if (!string.IsNullOrWhiteSpace(key) && key != oldkey)
                {
                    oldkey = key;
                    currentBorehole = new Borehole
                    {
                        Key = key,
                        LineNumber = TryGetIntSafe(worksheet, row, lineCol) ?? 0,
                        Id = TryGetIntSafe(worksheet, row, idCol) ?? 0,
                        X = ParseDoubleSafe(worksheet, row, xCol),
                        Y = ParseDoubleSafe(worksheet, row, yCol),
                        Z = ParseDoubleSafe(worksheet, row, zCol),
                    };
                    boreholes.Add(currentBorehole);
                }

                if (currentBorehole != null)
                {
                    Sample sample = new Sample();

                    // Если у пробы нет своих X,Y,Z (ячейки пустые), fallback берет координаты скважины
                    sample.X = ParseDoubleSafe(worksheet, row, xCol, currentBorehole.X);
                    sample.Y = ParseDoubleSafe(worksheet, row, yCol, currentBorehole.Y);
                    sample.Z = ParseDoubleSafe(worksheet, row, zCol, currentBorehole.Z);

                    sample.From = TryGetDoubleSafe(worksheet, row, fromCol) ?? 0;
                    sample.To = TryGetDoubleSafe(worksheet, row, toCol) ?? 0;
                    sample.Length = TryGetDoubleSafe(worksheet, row, lengthCol) ?? 0;
                    sample.Value = ParseDoubleSafe(worksheet, row, valueCol);

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

        // --- Вспомогательные безопасные методы ---

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

        public static List<Lithology> ParseLithology(ExcelWorksheet ws, int row, int column)
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

        public static double ParseDouble(string value, double fallback = 0)
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