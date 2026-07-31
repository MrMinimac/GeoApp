using HelixToolkit.Wpf;
using LegendDesignWpf.Controls;
using netDxf;
using netDxf.Entities;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace GeoAppWpf
{
    public interface IDrawer3D
    {
        void Draw(HelixViewport3D viewport);
        void Update();
        void ChangeScalePolyline(double scale);
    }

    

    public class DXFDrawer : IDrawer3D
    {
        private EntityObject _entity;
        private LinesVisual3D? _visual;
        private LinesVisual3D? _lastVisual;
        private HelixViewport3D? _viewport;

        public DXFDrawer(EntityObject entity)
        {
            _entity = entity;
        }

        public void Draw(HelixViewport3D viewport)
        {
            if (_entity is not Polyline3D polyline3D)
                return;

            var dxfColor = polyline3D.Color;
            var lineColor = Color.FromArgb(255, dxfColor.R, dxfColor.G, dxfColor.B);
            _visual = new LinesVisual3D
            {
                Color = lineColor
            };

            _lastVisual = new LinesVisual3D
            {
                Color = GetDarkColor(lineColor)
            };

            viewport.Children.Add(_visual);
            viewport.Children.Add(_lastVisual);

            Update();
        }

        private Color GetDarkColor(Color color)
        {
            var R = (byte)(color.R - 50);
            var G = (byte)(color.G - 50);
            var B = (byte)(color.B - 50);

            if (R < 0) R = 0;
            if (G < 0) G = 0;
            if (B < 0) B = 0;

            return Color.FromArgb(100, R, G, B);
        }

        public void ChangeScalePolyline(double scale)
        {
            if (_entity is not Polyline3D polyline3D)
                return;

            var vertexes = polyline3D.Vertexes.ToArray();
            var pointsMap = new Dictionary<(long x, long y), List<int>>();

            // 1. Группируем индексы по координатам X и Y (как и было)
            for (int i = 0; i < vertexes.Length; i++)
            {
                var v = vertexes[i];
                long keyX = (long)Math.Round(v.X * 1000);
                long keyY = (long)Math.Round(v.Y * 1000);
                var key = (keyX, keyY);

                if (!pointsMap.TryGetValue(key, out var list))
                {
                    list = new List<int>(2);
                    pointsMap[key] = list;
                }
                list.Add(i);
            }

            // 2. СЖИМАЕМ МОЩНОСТЬ (ПО Z) — ваш оригинальный рабочий код
            foreach (var group in pointsMap.Values)
            {
                if (group.Count < 2) continue;

                double minZ = double.MaxValue;
                double maxZ = double.MinValue;

                foreach (var idx in group)
                {
                    double z = vertexes[idx].Z;
                    if (z < minZ) minZ = z;
                    if (z > maxZ) maxZ = z;
                }

                if (Math.Abs(maxZ - minZ) < 0.001) continue;

                double height = maxZ - minZ;
                double offset = height * (scale / 2.0);

                double newMinZ = minZ + offset;
                double newMaxZ = maxZ - offset;

                foreach (var idx in group)
                {
                    var v = vertexes[idx];
                    bool isLower = Math.Abs(v.Z - minZ) < Math.Abs(v.Z - maxZ);
                    vertexes[idx] = new Vector3(v.X, v.Y, isLower ? newMinZ : newMaxZ);
                }
            }

            // 3. ВЫКЛИНИВАНИЕ ПО ДЛИНЕ (Сдвиг крайних точек вдоль линий)
            var uniqueKeys = pointsMap.Keys.ToList();

            if (uniqueKeys.Count >= 2)
            {
                // Ищем два конца линзы (две группы точек с максимальным расстоянием между ними)
                var tipA_Key = uniqueKeys[0];
                var tipB_Key = uniqueKeys[0];
                double maxDistSq = -1;

                for (int i = 0; i < uniqueKeys.Count; i++)
                {
                    for (int j = i + 1; j < uniqueKeys.Count; j++)
                    {
                        // Делим на 1000, так как ключи мы умножали
                        double dx = (uniqueKeys[i].x - uniqueKeys[j].x) / 1000.0;
                        double dy = (uniqueKeys[i].y - uniqueKeys[j].y) / 1000.0;
                        double distSq = dx * dx + dy * dy;

                        if (distSq > maxDistSq)
                        {
                            maxDistSq = distSq;
                            tipA_Key = uniqueKeys[i];
                            tipB_Key = uniqueKeys[j];
                        }
                    }
                }

                // Общая длина фигуры по прямой
                double totalLength = Math.Sqrt(maxDistSq);

                // На сколько сдвигать каждый конец (например, 20% scale означает 10% с каждой стороны)
                double targetShiftDist = totalLength * (scale / 2.0);

                // Локальная функция для сдвига одного острия
                void ShiftTip((long x, long y) tipKey)
                {
                    var group = pointsMap[tipKey];
                    int N = vertexes.Length;
                    Vector3? adjacentPoint = null;

                    // Ищем реальную соседнюю точку в полилинии, которая не лежит на этой же вертикали
                    foreach (int idx in group)
                    {
                        var vTip = vertexes[idx];

                        // Проверяем предыдущую точку в массиве
                        int prevIdx = (idx - 1 + N) % N;
                        var vPrev = vertexes[prevIdx];
                        if (Math.Abs(vTip.X - vPrev.X) > 0.001 || Math.Abs(vTip.Y - vPrev.Y) > 0.001)
                        {
                            adjacentPoint = vPrev;
                            break;
                        }

                        // Проверяем следующую точку в массиве
                        int nextIdx = (idx + 1) % N;
                        var vNext = vertexes[nextIdx];
                        if (Math.Abs(vTip.X - vNext.X) > 0.001 || Math.Abs(vTip.Y - vNext.Y) > 0.001)
                        {
                            adjacentPoint = vNext;
                            break;
                        }
                    }

                    // Если нашли соседнюю точку, сдвигаем острие к ней
                    if (adjacentPoint.HasValue)
                    {
                        var baseV = vertexes[group[0]]; // Текущие координаты острия

                        // Вектор направления к соседней точке
                        double dirX = adjacentPoint.Value.X - baseV.X;
                        double dirY = adjacentPoint.Value.Y - baseV.Y;
                        double distToAdj = Math.Sqrt(dirX * dirX + dirY * dirY);

                        if (distToAdj > 0.001)
                        {
                            // Важная защита: чтобы не вывернуть контур наизнанку, 
                            // сдвигаем максимум на 99% расстояния до соседней точки
                            double actualShift = Math.Min(targetShiftDist, distToAdj * (scale / 2));

                            // Нормализуем вектор и умножаем на дистанцию сдвига
                            double normX = dirX / distToAdj;
                            double normY = dirY / distToAdj;

                            double newX = baseV.X + normX * actualShift;
                            double newY = baseV.Y + normY * actualShift;

                            // Применяем новые X и Y ко ВСЕМ точкам на этой вертикали (и к верхней, и к нижней)
                            foreach (var idx in group)
                            {
                                var oldV = vertexes[idx];
                                vertexes[idx] = new Vector3(newX, newY, oldV.Z);
                            }
                        }
                    }
                }

                // Применяем сдвиг к обоим найденным краям контура
                ShiftTip(tipA_Key);
                ShiftTip(tipB_Key);
            }

            // 4. Копируем измененные данные обратно в полилинию
            for (int i = 0; i < vertexes.Length; i++)
            {
                polyline3D.Vertexes[i] = vertexes[i];
            }

            Update();
        }

        public void ChangeScalePolyline2(double scale)
        {
            if (_entity is not Polyline3D polyline3D)
                return;

            var vertexes = polyline3D.Vertexes.ToArray();

            // 1. Группируем индексы по координатам X и Y
            var pointsMap = new Dictionary<(long x, long y), List<int>>();

            for (int i = 0; i < vertexes.Length; i++)
            {
                var v = vertexes[i];
                long keyX = (long)Math.Round(v.X * 1000);
                long keyY = (long)Math.Round(v.Y * 1000);
                var key = (keyX, keyY);

                if (!pointsMap.TryGetValue(key, out var list))
                {
                    list = new List<int>(2);
                    pointsMap[key] = list;
                }
                list.Add(i);
            }

            // 2. Вычисляем геометрический центр (Центроид) контура по X и Y
            double sumX = 0;
            double sumY = 0;

            foreach (var group in pointsMap.Values)
            {
                // Берем первую точку из группы (ведь X и Y у них одинаковые)
                var v = vertexes[group[0]];
                sumX += v.X;
                sumY += v.Y;
            }

            double centerX = sumX / pointsMap.Count;
            double centerY = sumY / pointsMap.Count;

            // Множитель для X и Y. Если scale = 0.1 (10%), контур сожмется до 90% от оригинала
            double xyScaleFactor = 1.0 - scale;

            // 3. Обрабатываем каждую вертикаль (группу)
            foreach (var group in pointsMap.Values)
            {
                if (group.Count < 2)
                    continue;

                // --- ВЫЧИСЛЯЕМ НОВЫЕ X и Y ---
                var baseV = vertexes[group[0]];

                // Вектор от центра до текущей точки
                double dirX = baseV.X - centerX;
                double dirY = baseV.Y - centerY;

                // Новые координаты, сдвинутые к центру
                double newX = centerX + (dirX * xyScaleFactor);
                double newY = centerY + (dirY * xyScaleFactor);


                // --- ВЫЧИСЛЯЕМ НОВЫЕ Z ---
                double minZ = double.MaxValue;
                double maxZ = double.MinValue;

                foreach (var idx in group)
                {
                    double z = vertexes[idx].Z;
                    if (z < minZ) minZ = z;
                    if (z > maxZ) maxZ = z;
                }

                if (Math.Abs(maxZ - minZ) < 0.001)
                    continue;

                double height = maxZ - minZ;
                double offsetZ = height * (scale / 2.0);

                double newMinZ = minZ + offsetZ;
                double newMaxZ = maxZ - offsetZ;


                // 4. Применяем все изменения (X, Y, Z) ко всем точкам в группе
                foreach (var idx in group)
                {
                    var v = vertexes[idx];
                    bool isLower = Math.Abs(v.Z - minZ) < Math.Abs(v.Z - maxZ);

                    vertexes[idx] = new Vector3(
                        newX,
                        newY,
                        isLower ? newMinZ : newMaxZ);
                }
            }

            // 5. Записываем изменения
            for (int i = 0; i < vertexes.Length; i++)
            {
                polyline3D.Vertexes[i] = vertexes[i];
            }

            Update();
        }

        public void Update()
        {
            if (_entity is not Polyline3D polyline3D)
                return;

            if (_visual == null || _lastVisual == null)
                return;

            _lastVisual.Points.Clear();

            foreach (var p in _visual.Points)
                _lastVisual.Points.Add(p);

            _visual.Points.Clear();

            var points = polyline3D.Vertexes
                .Select(v => new Point3D(v.X, v.Y, v.Z))
                .ToList();

            for (int i = 0; i < points.Count - 1; i++)
            {
                _visual.Points.Add(points[i]);
                _visual.Points.Add(points[i + 1]);
            }

            _viewport?.UpdateLayout();
        }
    }

    public partial class Viewer3D : LDWindow
    {
        private readonly IDrawer3D _drawer;

        public Viewer3D(IDrawer3D drawer)
        {
            InitializeComponent();

            _drawer = drawer;

            drawer.Draw(Viewport);
            Viewport.ZoomExtentsWhenLoaded = true;
        }

        private void CameraCenter_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Viewport.ZoomExtents();
        }

        private void ChangeScale_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _drawer.ChangeScalePolyline(0.2);
        }
    }
}
