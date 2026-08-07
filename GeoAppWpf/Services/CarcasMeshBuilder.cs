using HelixToolkit.Geometry;
using HelixToolkit.Wpf;
using netDxf.Entities;
using System.Numerics;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace GeoAppWpf.Services
{
    public static class CarcasMeshBuilder
    {
        /// <summary>
        /// Создает 3D-сетку из готовых треугольников Face3D (из netDxf)
        /// </summary>
        public static GeometryModel3D BuildFromFaces(IEnumerable<Face3D> faces, Color color, double opacity = 0.8)
        {
            var builder = new MeshBuilder(generateNormals: true, generateTexCoords: false);

            foreach (var face in faces)
            {
                var p1 = ToNumerics(face.FirstVertex);
                var p2 = ToNumerics(face.SecondVertex);
                var p3 = ToNumerics(face.ThirdVertex);

                // В netDxf Face3D может быть 3-угольным или 4-угольным
                if (face.FourthVertex != null && face.FourthVertex != face.ThirdVertex)
                {
                    var p4 = ToNumerics(face.FourthVertex);
                    builder.AddQuad(p1, p2, p3, p4);
                }
                else
                {
                    builder.AddTriangle(p1, p2, p3);
                }
            }

            // Создаем Mesh и переводим в WPF
            var mesh = builder.ToMesh();
            var wpfMesh = mesh.ToWndMeshGeometry3D();

            // Задаем материал и прозрачность
            var materialGroup = new MaterialGroup();
            var brush = new SolidColorBrush(color) { Opacity = opacity };

            materialGroup.Children.Add(new DiffuseMaterial(brush));
            materialGroup.Children.Add(new SpecularMaterial(Brushes.White, 30));

            return new GeometryModel3D
            {
                Geometry = wpfMesh,
                Material = materialGroup,
                BackMaterial = materialGroup // Отображать с обеих сторон
            };
        }

        public static GeometryModel3D BuildSolidMesh(List<Polyline3D> contours, Color color, double opacity = 0.8)
        {
            var builder = new MeshBuilder(generateNormals: true, generateTexCoords: false);

            // 1. Натягиваем боковые грани между соседними сечениями
            for (int i = 0; i < contours.Count - 1; i++)
            {
                var current = contours[i].Vertexes;
                var next = contours[i + 1].Vertexes;

                int count = Math.Min(current.Count, next.Count);

                for (int j = 0; j < count; j++)
                {
                    int nextJ = (j + 1) % count; // Зацикливание для замкнутого контура

                    // AddQuad внутри вызывает AddTriangle и корректно генерирует нормали
                    builder.AddQuad(
                        ToNumerics(current[j]),
                        ToNumerics(current[nextJ]),
                        ToNumerics(next[nextJ]),
                        ToNumerics(next[j]));
                }
            }

            // 2. Закрываем торцы (используем безопасный метод AddPolygonCap вместо AddPolygon)
            if (contours.Count > 0)
            {
                // Первый торец (разворачиваем обход, чтобы нормаль смотрела наружу)
                var firstPoints = contours.First().Vertexes.Select(ToNumerics).Reverse().ToList();
                AddPolygonCap(builder, firstPoints);

                // Последний торец
                var lastPoints = contours.Last().Vertexes.Select(ToNumerics).ToList();
                AddPolygonCap(builder, lastPoints);
            }

            // 3. Создаем Mesh и конвертируем в WPF
            var mesh = builder.ToMesh();
            var wpfMesh = mesh.ToWndMeshGeometry3D();

            // 4. Задаем материал (цвет и полупрозрачность)
            var materialGroup = new MaterialGroup();
            var brush = new SolidColorBrush(color) { Opacity = opacity };

            materialGroup.Children.Add(new DiffuseMaterial(brush));
            materialGroup.Children.Add(new SpecularMaterial(Brushes.White, 30));

            return new GeometryModel3D
            {
                Geometry = wpfMesh,
                Material = materialGroup,
                BackMaterial = materialGroup // Чтобы объект прорисовывался и изнутри
            };
        }

        /// <summary>
        /// Безопасное закрытие плоского торца полигона через AddTriangle.
        /// Обходит ошибку с fanNormals в HelixToolkit.Geometry 3.x.
        /// </summary>
        private static void AddPolygonCap(MeshBuilder builder, List<Vector3> points)
        {
            if (points == null || points.Count < 3) return;

            // 1. Находим геометрический центр (центроид) контура
            Vector3 center = Vector3.Zero;
            foreach (var p in points)
            {
                center += p;
            }
            center /= points.Count;

            // 2. Соединяем точки веером треугольников вокруг центра
            for (int i = 0; i < points.Count; i++)
            {
                int next = (i + 1) % points.Count;
                builder.AddTriangle(center, points[i], points[next]);
            }
        }

        private static Vector3 ToNumerics(netDxf.Vector3 v)
        {
            return new Vector3((float)v.X, (float)v.Y, (float)v.Z);
        }
    }
}