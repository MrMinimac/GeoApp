using GeoAppWpf.Interfaces;
using GeoAppWpf.Services;
using netDxf;
using netDxf.Entities;
using netDxf.Tables;
using System.Diagnostics;

namespace GeoAppWpf.Operations
{

    public class RenameCopositesOperation : IUndoableCommand
    {
        private class LayerInfo
        {
            public required Layer Layer { get; init; }

            public required List<Face3D> Faces { get; init; }

            public required Vector3 Min { get; init; }

            public required Vector3 Max { get; init; }
        }

        private readonly MacromineDatResult _datResult;
        private readonly List<Face3D> _entities;
        private bool _initialized;

        public RenameCopositesOperation(MacromineDatResult datResult, IEnumerable<Face3D> faces)
        {
            _datResult = datResult;
            _entities = faces.ToList();
        }

        public void Execute()
        {
            if (!_initialized)
            {
                Initialize();
                _initialized = true;
            }
        }

        public void Undo()
        {
            throw new NotImplementedException();
        }

        private void Initialize()
        {
            foreach (var sample in _datResult.Samples)
            {
                sample.Code = string.Empty;
            }

            // ============================================================
            // 1. Получаем все каркасы, сгруппированные по Layer
            // ============================================================

            var allCarcasses = _entities
                .Where(f => f.Layer != null)
                .GroupBy(f => f.Layer)
                .Select(group =>
                {
                    var faces = group.ToList();

                    var minX = faces
                        .SelectMany(GetFaceVertices)
                        .Min(v => v.X);

                    var minY = faces
                        .SelectMany(GetFaceVertices)
                        .Min(v => v.Y);

                    var minZ = faces
                        .SelectMany(GetFaceVertices)
                        .Min(v => v.Z);

                    var maxX = faces
                        .SelectMany(GetFaceVertices)
                        .Max(v => v.X);

                    var maxY = faces
                        .SelectMany(GetFaceVertices)
                        .Max(v => v.Y);

                    var maxZ = faces
                        .SelectMany(GetFaceVertices)
                        .Max(v => v.Z);

                    return new LayerInfo
                    {
                        Layer = group.Key,
                        Faces = faces,

                        Min = new Vector3(
                            minX,
                            minY,
                            minZ),

                        Max = new Vector3(
                            maxX,
                            maxY,
                            maxZ)
                    };
                })
                .ToList();


            // ============================================================
            // 2. Ищем пробы внутри каждого каркаса
            // ============================================================

            foreach (var carcass in allCarcasses)
            {
                string carcassName = carcass.Layer.Name;

                foreach (var sample in _datResult.Samples)
                {
                    var point = new Vector3(
                        sample.X,
                        sample.Y,
                        sample.Z);

                    // Сначала быстрый BoundingBox
                    if (!IsInsideBounds(point, carcass))
                        continue;

                    // Затем точная проверка
                    if (!IsPointInsideCarcass(point, carcass.Faces))
                        continue;

                    // ====================================================
                    // Проба находится внутри каркаса
                    // ====================================================

                    sample.Code = carcassName;
                }
            }
        }

        private IEnumerable<Vector3> GetFaceVertices(Face3D face)
        {
            yield return face.FirstVertex;

            yield return face.SecondVertex;

            yield return face.ThirdVertex;

            if (face.FourthVertex != null)
                yield return face.FourthVertex;
        }

        private bool IsInsideBounds(Vector3 point, LayerInfo carcass)
        {
            const double tolerance = 0.001;

            return
                point.X >= carcass.Min.X - tolerance &&
                point.X <= carcass.Max.X + tolerance &&

                point.Y >= carcass.Min.Y - tolerance &&
                point.Y <= carcass.Max.Y + tolerance &&

                point.Z >= carcass.Min.Z - tolerance &&
                point.Z <= carcass.Max.Z + tolerance;
        }

        private bool IsPointInsideCarcass(Vector3 point, List<Face3D> faces)
        {
            int intersections = 0;

            // Луч идёт строго по X
            Vector3 direction = new Vector3(1, 0, 0);

            foreach (var face in faces)
            {
                var vertices = GetFaceVertices(face).ToList();

                if (vertices.Count < 3)
                    continue;

                // Первая грань
                if (RayIntersectsTriangle(
                    point,
                    direction,
                    vertices[0],
                    vertices[1],
                    vertices[2]))
                {
                    intersections++;
                }

                // Если есть четвертая вершина,
                // Face3D может представлять четырехугольник.
                if (vertices.Count == 4)
                {
                    if (RayIntersectsTriangle(
                        point,
                        direction,
                        vertices[0],
                        vertices[2],
                        vertices[3]))
                    {
                        intersections++;
                    }
                }
            }

            return intersections % 2 == 1;
        }

        private bool RayIntersectsTriangle(Vector3 origin, Vector3 direction, Vector3 v0, Vector3 v1, Vector3 v2)
        {
            const double epsilon = 1e-9;

            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;

            Vector3 h = Cross(direction, edge2);

            double a = Dot(edge1, h);

            if (Math.Abs(a) < epsilon)
                return false;

            double f = 1.0 / a;

            Vector3 s = origin - v0;

            double u = f * Dot(s, h);

            if (u < 0.0 || u > 1.0)
                return false;

            Vector3 q = Cross(s, edge1);

            double v = f * Dot(direction, q);

            if (v < 0.0 || u + v > 1.0)
                return false;

            double t = f * Dot(edge2, q);

            return t > epsilon;
        }

        private static double Dot(Vector3 a, Vector3 b)
        {
            return
                a.X * b.X +
                a.Y * b.Y +
                a.Z * b.Z;
        }


        private static Vector3 Cross(Vector3 a, Vector3 b)
        {
            return new Vector3(
                a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z,
                a.X * b.Y - a.Y * b.X);
        }
    }

    public class SortAndRenameLayersOperation : IUndoableCommand
    {
        private readonly DxfDocument _document;
        private readonly Dictionary<Layer, string> _oldNames = new();
        private readonly Dictionary<Layer, string> _newNames = new();
        private bool _initialized;
        private readonly string _suffix;
        private readonly int _startNumber;

        // Допустимое отклонение для группировки в один "столбец"
        const double xyTolerance = 5.0;

        public SortAndRenameLayersOperation(DxfDocument document, string suffix, int startNumber)
        {
            _document = document;
            _suffix = suffix;
            _startNumber = startNumber;
        }

        public void Execute()
        {
            if (!_initialized)
            {
                Initialize();
                _initialized = true;
            }

            RenameLayers(_newNames);
        }

        public void Undo()
        {
            RenameLayers(_oldNames);
        }

        private void Initialize()
        {
            // 1. Получаем центры всех слоев (без предварительной сортировки)
            var allLayers = _document.Entities.All
                .OfType<Face3D>()
                .GroupBy(x => x.Layer)
                .Select(group => new LayerSortInfo
                {
                    Layer = group.Key,
                    Center = GetLayerCenter(group)
                })
                .ToList();

            // 2. Группируем каркасы в вертикальные столбцы
            var columns = new List<List<LayerSortInfo>>();
            var unassigned = allLayers.ToList();

            while (unassigned.Count > 0)
            {
                var current = unassigned[0];
                var column = new List<LayerSortInfo> { current };
                unassigned.RemoveAt(0);

                // Идем с конца, чтобы безопасно удалять элементы из списка
                for (int i = unassigned.Count - 1; i >= 0; i--)
                {
                    var candidate = unassigned[i];

                    // Проверяем, принадлежит ли каркас к текущему столбцу
                    if (IsSameColumn(current.Center, candidate.Center, xyTolerance))
                    {
                        column.Add(candidate);
                        unassigned.RemoveAt(i);
                    }
                }
                columns.Add(column);
            }

            // 3. Сортируем столбцы "змейкой" (алгоритм ближайшего соседа)
            var sortedColumns = new List<List<LayerSortInfo>>();

            if (columns.Count > 0)
            {
                // Подготавливаем центры столбцов для быстрых расчетов
                var unvisitedColumns = columns.Select(col => new
                {
                    Column = col,
                    X = col.Average(x => x.Center.X),
                    Y = col.Average(x => x.Center.Y)
                }).ToList();

                // 3.1 За стартовый столбец берем самый "левый-нижний" (минимальный X, затем Y)
                var currentColumn = unvisitedColumns
                    .OrderBy(c => c.Y)
                    .ThenBy(c => c.X)
                    .First();

                sortedColumns.Add(currentColumn.Column);
                unvisitedColumns.Remove(currentColumn);

                // 3.2 Всегда ищем геометрически ближайший столбец к предыдущему
                while (unvisitedColumns.Count > 0)
                {
                    var nextColumn = unvisitedColumns
                        .OrderBy(c =>
                            // Считаем квадрат расстояния (без Math.Sqrt для скорости работы)
                            (currentColumn.Y - c.Y) * (currentColumn.Y - c.Y) +
                            (currentColumn.X - c.X) * (currentColumn.X - c.X)
                        )
                        .First();

                    sortedColumns.Add(nextColumn.Column);
                    currentColumn = nextColumn;
                    unvisitedColumns.Remove(currentColumn);
                }
            }

            // 4. Сортируем внутри каждого столбца по Z и разворачиваем в плоский список
            var finalSortedLayers = sortedColumns
                .SelectMany(col => col.OrderByDescending(x => x.Center.Z)) // Замени на OrderByDescending, если нужно сверху вниз
                .ToList();

            // 5. Генерируем новые имена на основе финальной сортировки
            for (int i = 0; i < finalSortedLayers.Count; i++)
            {
                var number = i + _startNumber;
                var layer = finalSortedLayers[i].Layer;
                var oldName = layer.Name;
                var category = GetCategory(oldName);
                var newName = $"{number}-{category}{_suffix}";

                _oldNames[layer] = oldName;
                _newNames[layer] = newName;
            }
        }

        private static bool IsSameColumn(Vector3 a, Vector3 b, double tolerance)
        {
            return Math.Abs(a.X - b.X) <= tolerance &&
                   Math.Abs(a.Y - b.Y) <= tolerance;
        }

        private void RenameLayers(IEnumerable<KeyValuePair<Layer, string>> names)
        {
            var pairs = names.ToList();

            // 1. Сначала всем переименовываемым слоям даем временные имена, 
            // чтобы избежать конфликтов внутри нашей целевой группы
            for (int i = 0; i < pairs.Count; i++)
            {
                var oldName = pairs[i].Key.Name;
                var newName = $"__RENAME_TEMP_{i}";

                try
                {
                    pairs[i].Key.Name = newName;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Ошибка переименования слоя \"{oldName}\" -> \"{newName}\".\n\nПодробности:\n{ex}");
                }
            }

            // 2. Теперь раздаем финальные имена
            foreach (var pair in pairs)
            {
                var oldName = pair.Key.Name; // Сейчас тут __RENAME_TEMP_...
                var targetName = pair.Value;

                try
                {
                    // ПРОВЕРКА КОНФЛИКТА: 
                    // Если в документе уже есть слой с таким именем (например, он был без Face3D)
                    if (_document.Layers.Contains(targetName))
                    {
                        var conflictingLayer = _document.Layers[targetName];

                        // Проверяем, что это не наш же слой на всякий случай
                        if (!ReferenceEquals(conflictingLayer, pair.Key))
                        {
                            // Переименовываем мешающий слой, добавляя суффикс и случайный ID, 
                            // чтобы гарантированно освободить имя targetName
                            string backupName = $"{targetName}_backup_{Guid.NewGuid().ToString("N").Substring(0, 4)}";
                            conflictingLayer.Name = backupName;
                        }
                    }

                    // Теперь имя точно свободно, переименовываем
                    pair.Key.Name = targetName;
                }
                catch (Exception ex)
                {
                    string message = $"Ошибка переименования слоя \"{oldName}\" -> \"{targetName}\".\n\nПодробности:\n";
                    Debug.WriteLine($"{message}{ex}");
                    throw new Exception($"{message}{ex.Message}");
                }
            }
        }

        private static string GetCategory(string layerName)
        {
            string decodedName = DecodeDxfUnicode(layerName);

            var name = decodedName.ToLowerInvariant();

            if (name.Contains("c1") || name.Contains("с1")) // Английская и русская 'С'
                return "C1";

            if (name.Contains("c2") || name.Contains("с2"))
                return "C2";

            return "Unknown";
        }

        private static string DecodeDxfUnicode(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            if (input.Contains("_U_042"))
                return input.Replace("_U_042", "C1");

            if (input.Contains("Unknown"))
                return input.Replace("Unknown", "C1");

            // Ищем паттерн \U+XXXX (где X - шестнадцатеричная цифра)
            return System.Text.RegularExpressions.Regex.Replace(
                input,
                (@"\\U\+([0-9A-Fa-f]{4})"),
                match =>
                {
                    // Получаем HEX-код символа (например, 0421)
                    string hexCode = match.Groups[1].Value;
                    int codePoint = Convert.ToInt32(hexCode, 16);

                    // Превращаем обратно в нормальный символ ('С')
                    return ((char)codePoint).ToString();
                },
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
        }

        private static Vector3 GetLayerCenter(IEnumerable<Face3D> faces)
        {
            var vertices = faces
                .SelectMany(GetVertices)
                .ToList();

            return new Vector3(
                vertices.Average(v => v.X),
                vertices.Average(v => v.Y),
                vertices.Average(v => v.Z));
        }

        private static IEnumerable<Vector3> GetVertices(Face3D face)
        {
            yield return face.FirstVertex;
            yield return face.SecondVertex;
            yield return face.ThirdVertex;

            if (face.FourthVertex != null)
                yield return face.FourthVertex;
        }

        private sealed class LayerSortInfo
        {
            public required Layer Layer { get; init; }
            public required Vector3 Center { get; init; }
        }
    }
}
