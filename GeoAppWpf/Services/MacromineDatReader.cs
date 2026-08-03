using GeoAppWpf.Models;
using netDxf;
using netDxf.Entities;
using System.Globalization;
using System.IO;

namespace GeoAppWpf.Services
{
    public class MacromineDatReader
    {
        public DxfDocument ReadToDxf(string filePath)
        {
            var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            var dxf = new DxfDocument();
            using var reader = new StreamReader(stream);

            var lines = new List<string>();
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                lines.Add(line);
            }

            // 1. Извлекаем структуру заголовка (колонки и их ширину)
            var (contentStartIndex, columns) = ParseHeader(lines);

            // 2. Парсим все точки из файла
            var points = ParsePoints(lines, contentStartIndex, columns);

            // 3. Группируем точки по JOIN (или STRING) в полилинии
            var polylineGroups = points
                .Where(p => !string.IsNullOrEmpty(p.JoinId))
                .GroupBy(p => p.JoinId);

            foreach (var group in polylineGroups)
            {
                // Преобразуем точки группы в Vector3 для netDxf
                var vertices = group
                    .Select(p => new Vector3(p.X, p.Y, p.Z))
                    .ToList();

                if (vertices.Count < 2)
                    continue; // Полилиния должна иметь минимум 2 точки

                var polyline = new Polyline3D(vertices);

                // Опционально: создаем отдельный слой для каждого JOIN/STRING, 
                // чтобы в CAD было удобно управлять видимостью
                var layerName = $"Line_{group.Key}";
                if (!dxf.Layers.Contains(layerName))
                {
                    dxf.Layers.Add(new netDxf.Tables.Layer(layerName));
                }
                polyline.Layer = dxf.Layers[layerName];

                dxf.Entities.Add(polyline);
            }

            return dxf;
        }

        private (int contentStartIndex, List<(string Name, int Width)> columns) ParseHeader(List<string> lines)
        {
            int variablesCount = -1;
            int variablesIndex = -1;

            for (int i = 0; i < lines.Count; i++)
            {
                var l = lines[i].Trim();
                if (l.ToUpper().Contains("VARIABLES"))
                {
                    var parts = l.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (int.TryParse(parts[0], out int count))
                    {
                        variablesCount = count;
                        variablesIndex = i + 1;
                        break;
                    }
                }
            }

            if (variablesIndex == -1 || variablesCount == -1)
                throw new InvalidDataException("Не найден блок VARIABLES в заголовке DAT файла.");

            var columns = new List<(string Name, int Width)>();

            for (int i = variablesIndex; i < variablesIndex + variablesCount; i++)
            {
                var parts = lines[i].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                string name = parts[0];
                int width = int.Parse(parts[2]);
                columns.Add((name, width));
            }

            int contentStartIndex = variablesIndex + variablesCount;
            return (contentStartIndex, columns);
        }

        private List<MacrominePoint> ParsePoints(List<string> lines, int startIndex, List<(string Name, int Width)> columns)
        {
            var points = new List<MacrominePoint>();

            // Индексы ключевых колонок
            int eastIdx = columns.FindIndex(c => c.Name.Equals("EAST", StringComparison.OrdinalIgnoreCase));
            int northIdx = columns.FindIndex(c => c.Name.Equals("NORTH", StringComparison.OrdinalIgnoreCase));
            int rlIdx = columns.FindIndex(c => c.Name.Equals("RL", StringComparison.OrdinalIgnoreCase));
            int stringIdx = columns.FindIndex(c => c.Name.Equals("STRING", StringComparison.OrdinalIgnoreCase));
            int joinIdx = columns.FindIndex(c => c.Name.Equals("JOIN", StringComparison.OrdinalIgnoreCase));

            for (int i = startIndex; i < lines.Count; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                var point = new MacrominePoint();
                int currentOffset = 0;

                for (int colIdx = 0; colIdx < columns.Count; colIdx++)
                {
                    var col = columns[colIdx];
                    if (currentOffset >= line.Length) break;

                    int lengthToRead = Math.Min(col.Width, line.Length - currentOffset);
                    string rawValue = line.Substring(currentOffset, lengthToRead).Trim();

                    if (colIdx == eastIdx)
                        point.X = ParseDouble(rawValue);
                    else if (colIdx == northIdx)
                        point.Y = ParseDouble(rawValue);
                    else if (colIdx == rlIdx)
                        point.Z = ParseDouble(rawValue);
                    else if (colIdx == stringIdx)
                        point.StringCode = rawValue;
                    else if (colIdx == joinIdx)
                        point.JoinId = rawValue;

                    currentOffset += col.Width;
                }

                points.Add(point);
            }

            return points;
        }

        private double ParseDouble(string val)
        {
            if (double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out double res))
                return res;
            return 0.0;
        }
    }
}
