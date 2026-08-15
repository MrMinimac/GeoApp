using GeoAppWpf.Interfaces;
using netDxf;
using netDxf.Entities;
using netDxf.Tables;

namespace GeoAppWpf.Operations
{
    public class SortAndRenameLayersOperation : IUndoableCommand
    {
        private readonly DxfDocument _document;
        private readonly Dictionary<Layer, string> _oldNames = new();
        private readonly Dictionary<Layer, string> _newNames = new();
        private bool _initialized;

        // Допустимое отклонение для группировки в один "столбец"
        const double xyTolerance = 5.0;

        public SortAndRenameLayersOperation(DxfDocument document)
        {
            _document = document;
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

            // 3. Сортируем столбцы между собой (слева направо, затем вглубь)
            // В качестве координат столбца берем среднее значение X и Y его элементов
            var sortedColumns = columns
                .OrderBy(col => col.Average(x => x.Center.Y))
                .ThenBy(col => col.Average(x => x.Center.X))
                .ToList();

            // 4. Сортируем внутри каждого столбца по Z и разворачиваем в плоский список
            var finalSortedLayers = sortedColumns
                .SelectMany(col => col.OrderByDescending(x => x.Center.Z)) // Замени на OrderByDescending, если нужно сверху вниз
                .ToList();

            // 5. Генерируем новые имена на основе финальной сортировки
            for (int i = 0; i < finalSortedLayers.Count; i++)
            {
                var layer = finalSortedLayers[i].Layer;
                var oldName = layer.Name;
                var category = GetCategory(oldName);
                var newName = $"{i + 1}-{category}";

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

            for (int i = 0; i < pairs.Count; i++)
            {
                pairs[i].Key.Name = $"__RENAME_TEMP_{i}";
            }

            foreach (var pair in pairs)
            {
                pair.Key.Name = pair.Value;
            }
        }

        private static string GetCategory(string layerName)
        {
            var name = layerName.ToLowerInvariant();

            if (name.Contains("c1") || name.Contains("с1")) // Английская и русская 'С'
                return "C1";

            if (name.Contains("c2") || name.Contains("с2"))
                return "C2";

            return "Unknown";
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
