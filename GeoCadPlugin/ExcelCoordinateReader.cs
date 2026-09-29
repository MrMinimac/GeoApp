using GeoAppCore.Services;
using OfficeOpenXml;
using System.Globalization;
using System.IO;

namespace GeoCadPlugin
{
    public class Coordinates
    {
        public List<(double X, double Y)> Points { get; set; } = new();
        public CoordinatesType Type;

        public Coordinates(List<(double X, double Y)> points, CoordinatesType type)
        {
            Points = points;
            Type = type;
        }
    }

    public enum CoordinatesType
    {
        // Столбцы: Lat deg, Lat min, Lat sec, Lon deg, Lon min, Lon sec
        GeographicDms,

        // Столбцы: Lat, Lon (в десятичных градусах)
        GeographicDecimal,

        // Столбцы: X, Y (прямоугольные координаты Гаусса-Крюгера)
        GaussKruger
    }

    public static class CoorditateHelper
    {
        public static Coordinates ConvertToGaussKruger(Coordinates coords)
        {
            if (coords.Type == CoordinatesType.GaussKruger)
                return coords;

            List<(double X, double Y)> points = new();

            foreach (var point in coords.Points)
            {
                // point.X = Longitude
                // point.Y = Latitude

                var result = GaussKrugerConverter.GeodeticToGK(
                    point.Y, // Latitude
                    point.X  // Longitude
                );

                // Для AutoCAD:
                // X = Восточная координата
                // Y = Северная координата
                points.Add((result.Y, result.X));
            }

            return new(points, CoordinatesType.GaussKruger);
        }
    }

    public static class ExcelCoordinateReader
    {
        static ExcelCoordinateReader()
        {
            ExcelPackage.License.SetNonCommercialPersonal("MrMinimac.GeoApp");
        }

        /// <summary>
        /// Читает координаты из Excel и возвращает их вместе с определенным типом системы координат.
        /// Возвращает кортеж: X (Долгота или X), Y (Широта или Y).
        /// </summary>
        public static Coordinates Read(string filePath)
        {
            var result = new List<(double X, double Y)>();

            using var package = new ExcelPackage(new FileInfo(filePath));
            var worksheet = package.Workbook.Worksheets[0];

            if (worksheet.Dimension == null)
                return new(result, CoordinatesType.GeographicDecimal); // По умолчанию пустой

            // Индексы столбцов для всех 3-х вариантов
            int latDegCol = 0, latMinCol = 0, latSecCol = 0;
            int lonDegCol = 0, lonMinCol = 0, lonSecCol = 0;

            int latCol = 0, lonCol = 0;
            int xCol = 0, yCol = 0;

            // 1. Поиск столбцов по заголовкам
            for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
            {
                string header = worksheet.Cells[1, col].Text.Trim();

                // Проверка на DMS
                if (header.Equals("Lat deg", StringComparison.OrdinalIgnoreCase)) latDegCol = col;
                else if (header.Equals("Lat min", StringComparison.OrdinalIgnoreCase)) latMinCol = col;
                else if (header.Equals("Lat sec", StringComparison.OrdinalIgnoreCase)) latSecCol = col;
                else if (header.Equals("Lon deg", StringComparison.OrdinalIgnoreCase)) lonDegCol = col;
                else if (header.Equals("Lon min", StringComparison.OrdinalIgnoreCase)) lonMinCol = col;
                else if (header.Equals("Lon sec", StringComparison.OrdinalIgnoreCase)) lonSecCol = col;
                // Проверка на Decimal Degrees
                else if (header.Equals("Lat", StringComparison.OrdinalIgnoreCase)) latCol = col;
                else if (header.Equals("Lon", StringComparison.OrdinalIgnoreCase)) lonCol = col;
                // Проверка на Gauss-Kruger
                else if (header.Equals("X", StringComparison.OrdinalIgnoreCase)) xCol = col;
                else if (header.Equals("Y", StringComparison.OrdinalIgnoreCase)) yCol = col;
            }

            // 2. Определение формата таблицы
            CoordinatesType tableType;

            if (latDegCol > 0 && latMinCol > 0 && latSecCol > 0 && lonDegCol > 0 && lonMinCol > 0 && lonSecCol > 0)
            {
                tableType = CoordinatesType.GeographicDms;
            }
            else if (latCol > 0 && lonCol > 0)
            {
                tableType = CoordinatesType.GeographicDecimal;
            }
            else if (xCol > 0 && yCol > 0)
            {
                tableType = CoordinatesType.GaussKruger;
            }
            else
            {
                throw new Exception("Не удалось определить формат таблицы. Убедитесь, что заголовки соответствуют одному из форматов: (Lat deg, min, sec...), (Lat, Lon) или (X, Y).");
            }

            // 3. Чтение данных в зависимости от формата
            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                if (tableType == CoordinatesType.GeographicDms)
                {
                    if (TryParseCell(worksheet.Cells[row, latDegCol], out double latDeg) &&
                        TryParseCell(worksheet.Cells[row, latMinCol], out double latMin) &&
                        TryParseCell(worksheet.Cells[row, latSecCol], out double latSec) &&
                        TryParseCell(worksheet.Cells[row, lonDegCol], out double lonDeg) &&
                        TryParseCell(worksheet.Cells[row, lonMinCol], out double lonMin) &&
                        TryParseCell(worksheet.Cells[row, lonSecCol], out double lonSec))
                    {
                        double lat = latDeg + (latMin / 60.0) + (latSec / 3600.0);
                        double lon = lonDeg + (lonMin / 60.0) + (lonSec / 3600.0);
                        result.Add((lon, lat)); // X = Lon, Y = Lat
                    }
                }
                else if (tableType == CoordinatesType.GeographicDecimal)
                {
                    if (TryParseCell(worksheet.Cells[row, latCol], out double lat) &&
                        TryParseCell(worksheet.Cells[row, lonCol], out double lon))
                    {
                        result.Add((lon, lat)); // X = Lon, Y = Lat
                    }
                }
                else if (tableType == CoordinatesType.GaussKruger)
                {
                    if (TryParseCell(worksheet.Cells[row, xCol], out double x) &&
                        TryParseCell(worksheet.Cells[row, yCol], out double y))
                    {
                        result.Add((x, y)); // X = X, Y = Y
                    }
                }
            }

            return new(result, tableType);
        }

        private static bool TryParseCell(ExcelRangeBase cell, out double value)
        {
            string text = cell.Text?.Trim().Replace(',', '.');
            return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }
    }
}
