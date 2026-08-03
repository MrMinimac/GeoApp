using HelixToolkit.Wpf;
using LegendDesignWpf.Controls;
using netDxf;
using netDxf.Entities;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace GeoAppWpf
{
    public interface IDrawer3D
    {
        void Draw(HelixViewport3D viewport);
        void Update();
        void ChangeScale(double scale);
    }

    public class DrawerObject
    {
        public EntityObject Entity { get; private set; }
        public LinesVisual3D Actual { get; set; }
        public LinesVisual3D LastVisual { get; set; }

        public DrawerObject(EntityObject entity)
        {
            Entity = entity;

            if (Entity is not Polyline3D pl)
                throw new Exception("Supported only polyline 3D");

            var dxfColor = pl.Color;
            var lineColor = Color.FromArgb(255, dxfColor.R, dxfColor.G, dxfColor.B);

            Actual = new LinesVisual3D
            {
                Color = lineColor
            };

            LastVisual = new LinesVisual3D
            {
                Color = GetDarkColor(lineColor)
            };
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

        public void ChangeScale(double scale)
        {
            if (Entity is not Polyline3D polyline3D)
                return;

            var vertexes = polyline3D.Vertexes.ToArray();

            Debug.WriteLine($"\nBEFORE:");
            for (int i = 0; i < vertexes.Length; i++)
            {
                var v = vertexes[i];
                Debug.WriteLine($"{i}. {v.X}, {v.Y}, {v.Z}\n");
            }

            // 1. Группируем индексы по дистанции (с допуском погрешности 0.01)
            var groups = new List<List<int>>();
            for (int i = 0; i < vertexes.Length; i++)
            {
                var vCurrent = vertexes[i];
                bool foundGroup = false;

                foreach (var group in groups)
                {
                    var vGroup = vertexes[group[0]];
                    double dx = vGroup.X - vCurrent.X;
                    double dy = vGroup.Y - vCurrent.Y;

                    // Если точки в плоскости XY находятся ближе чем на 0.01, это одна вертикаль
                    if (Math.Sqrt(dx * dx + dy * dy) < 0.05)
                    {
                        group.Add(i);
                        foundGroup = true;
                        break;
                    }
                }

                if (!foundGroup)
                {
                    groups.Add(new List<int> { i });
                }
            }

            // 2. СЖИМАЕМ МОЩНОСТЬ (ПО Z)
            foreach (var group in groups)
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

            // 3. ВЫКЛИНИВАНИЕ ПО ДЛИНЕ
            if (groups.Count >= 2)
            {
                var tipAGroup = groups[0];
                var tipBGroup = groups[0];
                double maxDistSq = -1;

                // Ищем самые удаленные группы
                for (int i = 0; i < groups.Count; i++)
                {
                    for (int j = i + 1; j < groups.Count; j++)
                    {
                        var vI = vertexes[groups[i][0]];
                        var vJ = vertexes[groups[j][0]];

                        double dx = vI.X - vJ.X;
                        double dy = vI.Y - vJ.Y;
                        double distSq = dx * dx + dy * dy;

                        if (distSq > maxDistSq)
                        {
                            maxDistSq = distSq;
                            tipAGroup = groups[i];
                            tipBGroup = groups[j];
                        }
                    }
                }

                double totalLength = Math.Sqrt(maxDistSq);
                double targetShiftDist = totalLength * (scale / 2.0);

                void ShiftTip(List<int> groupToShift)
                {
                    int N = vertexes.Length;
                    Vector3? adjacentPoint = null;
                    double minValidDistSq = maxDistSq * 0.0001;

                    foreach (int idx in groupToShift)
                    {
                        var vTip = vertexes[idx];

                        // Проверяем предыдущую
                        int prevIdx = (idx - 1 + N) % N;
                        var vPrev = vertexes[prevIdx];
                        double distSqPrev = (vTip.X - vPrev.X) * (vTip.X - vPrev.X) + (vTip.Y - vPrev.Y) * (vTip.Y - vPrev.Y);
                        if (distSqPrev > minValidDistSq)
                        {
                            adjacentPoint = vPrev;
                            break;
                        }

                        // Проверяем следующую
                        int nextIdx = (idx + 1) % N;
                        var vNext = vertexes[nextIdx];
                        double distSqNext = (vTip.X - vNext.X) * (vTip.X - vNext.X) + (vTip.Y - vNext.Y) * (vTip.Y - vNext.Y);
                        if (distSqNext > minValidDistSq)
                        {
                            adjacentPoint = vNext;
                            break;
                        }
                    }

                    if (!adjacentPoint.HasValue)
                    {
                        var otherGroup = (groupToShift == tipAGroup) ? tipBGroup : tipAGroup;
                        adjacentPoint = vertexes[otherGroup[0]];
                    }

                    if (adjacentPoint.HasValue)
                    {
                        var baseV = vertexes[groupToShift[0]];

                        double dirX = adjacentPoint.Value.X - baseV.X;
                        double dirY = adjacentPoint.Value.Y - baseV.Y;
                        double distToAdj = Math.Sqrt(dirX * dirX + dirY * dirY);

                        if (distToAdj > 0.001)
                        {
                            double actualShift = Math.Min(targetShiftDist, distToAdj * 0.99);
                            double normX = dirX / distToAdj;
                            double normY = dirY / distToAdj;

                            double newX = baseV.X + normX * actualShift;
                            double newY = baseV.Y + normY * actualShift;

                            foreach (var idx in groupToShift)
                            {
                                var oldV = vertexes[idx];
                                vertexes[idx] = new Vector3(newX, newY, oldV.Z);
                            }
                        }
                    }
                }

                ShiftTip(tipAGroup);
                ShiftTip(tipBGroup);
            }

            // 4. Копируем обратно
            for (int i = 0; i < vertexes.Length; i++)
            {
                polyline3D.Vertexes[i] = vertexes[i];
            }

            Debug.WriteLine($"\nAFTER:");
            for (int i = 0; i < polyline3D.Vertexes.Count; i++)
            {
                var v = polyline3D.Vertexes[i];
                Debug.WriteLine($"{i}. {v.X}, {v.Y}, {v.Z}\n");
            }
        }

        public void ChangeScalePolyline2(double scale)
        {
            if (Entity is not Polyline3D polyline3D)
                return;

            var vertexes = polyline3D.Vertexes.ToArray();

            // 1. Группируем индексы по дистанции в плоскости XY (с допуском погрешности 0.05)
            var groups = new List<List<int>>();

            for (int i = 0; i < vertexes.Length; i++)
            {
                var vCurrent = vertexes[i];
                bool foundGroup = false;

                foreach (var group in groups)
                {
                    var vGroup = vertexes[group[0]];
                    double dx = vGroup.X - vCurrent.X;
                    double dy = vGroup.Y - vCurrent.Y;

                    // Если точки лежат рядом (разница меньше 0.05), считаем их одной вертикалью
                    if (Math.Sqrt(dx * dx + dy * dy) < 0.05)
                    {
                        group.Add(i);
                        foundGroup = true;
                        break;
                    }
                }

                if (!foundGroup)
                {
                    groups.Add(new List<int> { i });
                }
            }

            // 2. Вычисляем геометрический центр (Центроид) контура по X и Y
            double sumX = 0;
            double sumY = 0;

            foreach (var group in groups)
            {
                var v = vertexes[group[0]]; // Берем базу группы
                sumX += v.X;
                sumY += v.Y;
            }

            if (groups.Count == 0) return; // Защита от пустых массивов

            double centerX = sumX / groups.Count;
            double centerY = sumY / groups.Count;

            // Множитель для X и Y
            double xyScaleFactor = 1.0 - scale;

            // 3. Обрабатываем каждую вертикаль (группу)
            foreach (var group in groups)
            {
                // --- ВЫЧИСЛЯЕМ НОВЫЕ X и Y ДЛЯ ВСЕХ ---
                var baseV = vertexes[group[0]];
                double dirX = baseV.X - centerX;
                double dirY = baseV.Y - centerY;

                double newX = centerX + (dirX * xyScaleFactor);
                double newY = centerY + (dirY * xyScaleFactor);

                // --- ПРОВЕРЯЕМ ОСЬ Z ---
                bool hasValidHeight = false;
                double minZ = double.MaxValue;
                double maxZ = double.MinValue;

                // Ищем высоту только если в группе больше одной точки
                if (group.Count >= 2)
                {
                    foreach (var idx in group)
                    {
                        double z = vertexes[idx].Z;
                        if (z < minZ) minZ = z;
                        if (z > maxZ) maxZ = z;
                    }

                    if (Math.Abs(maxZ - minZ) >= 0.001)
                    {
                        hasValidHeight = true;
                    }
                }

                // Если есть полноценная высота — сжимаем Z + сдвигаем X/Y
                if (hasValidHeight)
                {
                    double height = maxZ - minZ;
                    double offsetZ = height * (scale / 2.0);

                    double newMinZ = minZ + offsetZ;
                    double newMaxZ = maxZ - offsetZ;

                    foreach (var idx in group)
                    {
                        var v = vertexes[idx];
                        bool isLower = Math.Abs(v.Z - minZ) < Math.Abs(v.Z - maxZ);

                        vertexes[idx] = new Vector3(newX, newY, isLower ? newMinZ : newMaxZ);
                    }
                }
                else
                {
                    // Если точка одна или высота нулевая — просто применяем новые X и Y, Z не трогаем
                    foreach (var idx in group)
                    {
                        var v = vertexes[idx];
                        vertexes[idx] = new Vector3(newX, newY, v.Z);
                    }
                }
            }

            // 5. Записываем изменения
            for (int i = 0; i < vertexes.Length; i++)
            {
                polyline3D.Vertexes[i] = vertexes[i];
            }
        }
    }

    public class DXFDrawer : IDrawer3D
    {
        private List<DrawerObject> _visuals = new();
        private HelixViewport3D? _viewport;

        public DXFDrawer(List<EntityObject> entities)
        {
            foreach (var entity in entities)
                _visuals.Add(new DrawerObject(entity));
        }

        public void Draw(HelixViewport3D viewport)
        {
            foreach (var visual in _visuals)
            {
                if (visual.Entity is not Polyline3D polyline3D)
                    return;

                viewport.Children.Add(visual.Actual);
                viewport.Children.Add(visual.LastVisual);
            }

            Update();
        }


        public void ChangeScale(double scale)
        {
            foreach (var visual in _visuals)
                visual.ChangeScalePolyline2(scale);

            Update();
        }



        public void Update()
        {
            foreach (var visual in _visuals)
            {
                if (visual.Entity is not Polyline3D polyline3D)
                    continue;

                visual.LastVisual.Points.Clear();

                foreach (var p in visual.Actual.Points)
                    visual.LastVisual.Points.Add(p);

                visual.Actual.Points.Clear();

                var points = polyline3D.Vertexes
                    .Select(v => new Point3D(v.X, v.Y, v.Z))
                    .ToList();

                for (int i = 0; i < points.Count - 1; i++)
                {
                    visual.Actual.Points.Add(points[i]);
                    visual.Actual.Points.Add(points[i + 1]);
                }
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
            _drawer.ChangeScale(0.2);
        }
    }
}
