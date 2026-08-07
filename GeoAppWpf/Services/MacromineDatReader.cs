using GeoAppWpf.Models;
using LegendDesignWpf.Core.WinApi;
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

            var (contentStartIndex, columns) = ParseHeader(lines);

            // Получаем список уже разделенных полилиний
            var parsedLines = ParseLines(lines, contentStartIndex, columns);

            var color = ColorHelper.GetRandomColor();

            foreach (var lineGroup in parsedLines)
            {
                // Отсекаем одиночные точки (если нужны точки - создавай Point3D)
                if (lineGroup.Count < 2)
                    continue;

                var vertices = lineGroup
                    .Select(p => new Vector3(p.X, p.Y, p.Z))
                    .ToList();

                var polyline = new Polyline3D(vertices);
                polyline.Color = new AciColor(color.R, color.G, color.B);

                // Берем JOIN из первой точки текущей линии для названия слоя
                string layerJoinId = lineGroup[0].JoinId;
                var layerName = string.IsNullOrEmpty(layerJoinId) ? "Default_Line" : $"Line_{layerJoinId}";

                if (!dxf.Layers.Contains(layerName))
                {
                    dxf.Layers.Add(new netDxf.Tables.Layer(layerName));
                }

                polyline.Layer = dxf.Layers[layerName];
                dxf.Entities.Add(polyline);
            }

            return dxf;
        }

        private List<List<MacrominePoint>> ParseLines(List<string> lines, int startIndex, List<(string Name, int Width)> columns)
        {
            var resultLines = new List<List<MacrominePoint>>();
            var currentLine = new List<MacrominePoint>();
            string currentJoinId = string.Empty;

            // Находим индексы нужных колонок
            int eastIdx = columns.FindIndex(c => c.Name.Equals("EAST", StringComparison.OrdinalIgnoreCase));
            int northIdx = columns.FindIndex(c => c.Name.Equals("NORTH", StringComparison.OrdinalIgnoreCase));
            int rlIdx = columns.FindIndex(c => c.Name.Equals("RL", StringComparison.OrdinalIgnoreCase));
            int stringIdx = columns.FindIndex(c => c.Name.Equals("STRING", StringComparison.OrdinalIgnoreCase));
            int joinIdx = columns.FindIndex(c => c.Name.Equals("JOIN", StringComparison.OrdinalIgnoreCase));

            for (int i = startIndex; i < lines.Count; i++)
            {
                var line = lines[i];

                // 1. Физический разрыв пустой строкой
                if (string.IsNullOrWhiteSpace(line))
                {
                    if (currentLine.Count > 0)
                    {
                        resultLines.Add(currentLine);
                        currentLine = new List<MacrominePoint>();
                    }
                    continue;
                }

                var point = new MacrominePoint();
                int currentOffset = 0;

                for (int colIdx = 0; colIdx < columns.Count; colIdx++)
                {
                    var col = columns[colIdx];
                    if (currentOffset >= line.Length) break;

                    int lengthToRead = Math.Min(col.Width, line.Length - currentOffset);
                    string rawValue = line.Substring(currentOffset, lengthToRead).Trim();

                    if (colIdx == eastIdx) point.X = ParseDouble(rawValue);
                    else if (colIdx == northIdx) point.Y = ParseDouble(rawValue);
                    else if (colIdx == rlIdx) point.Z = ParseDouble(rawValue);
                    else if (colIdx == stringIdx) point.StringCode = rawValue;
                    else if (colIdx == joinIdx) point.JoinId = rawValue;

                    currentOffset += col.Width;
                }

                // --- ЛОГИКА ОБРАБОТКИ ТИЛЬДЫ (~) ---
                bool isForceBreak = false;

                // Проверяем, есть ли флаг разрыва в JOIN (или STRING)
                if (point.JoinId.EndsWith("~"))
                {
                    isForceBreak = true;
                    // Очищаем ID от тильды ("11~" превращается в "11")
                    point.JoinId = point.JoinId.TrimEnd('~');
                }
                else if (point.StringCode.EndsWith("~"))
                {
                    isForceBreak = true;
                    point.StringCode = point.StringCode.TrimEnd('~');
                }

                // 2. Логический разрыв (сменился ID или встретили тильду)
                if (currentLine.Count > 0 && (isForceBreak || point.JoinId != currentJoinId))
                {
                    resultLines.Add(currentLine);
                    currentLine = new List<MacrominePoint>();
                }

                currentLine.Add(point);
                currentJoinId = point.JoinId;
            }

            if (currentLine.Count > 0)
            {
                resultLines.Add(currentLine);
            }

            return resultLines;
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
