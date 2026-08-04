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
        void Build();
        void ChangeScale(double scale);
    }

    public class CarcasBuiler
    {
        public static List<Face3D>? Build(IEnumerable<Polyline3D> polylines)
        {
            // 1. Собираем все контуры
            List<List<Vector3>> allContours = new List<List<Vector3>>();

            foreach (var l in polylines)
            {
                allContours.Add(l.Vertexes.ToList());
            }

            if (allContours.Count < 2) return null;

            allContours = allContours.OrderBy(contour => contour.Average(v => v.X)).ToList();

            int targetPointsCount = 15;
            List<List<Vector3>> normalizedContours = new List<List<Vector3>>();

            // 2. Ресемплинг
            foreach (var contour in allContours)
            {
                normalizedContours.Add(ResampleContour(contour, targetPointsCount));
            }

            // 3. Синхронизация
            for (int i = 1; i < normalizedContours.Count; i++)
            {
                normalizedContours[i] = SynchronizeStart(normalizedContours[i - 1], normalizedContours[i]);
            }

            List<Face3D> meshTriangles = new List<Face3D>();

            //// 4. Построение боковых граней (между контурами)
            for (int i = 0; i < normalizedContours.Count - 1; i++)
            {
                var contourA = normalizedContours[i];
                var contourB = normalizedContours[i + 1];

                for (int j = 0; j < targetPointsCount; j++)
                {
                    int nextJ = (j + 1) % targetPointsCount;

                    meshTriangles.Add(new Face3D(contourA[j], contourB[j], contourA[nextJ]));
                    meshTriangles.Add(new Face3D(contourB[j], contourB[nextJ], contourA[nextJ]));
                }
            }

            // 5. ТРИАНГУЛЯЦИЯ ТОРЦОВ (закрытие первого и последнего контура)
            var startCap = TriangulateContourEarClipping(normalizedContours.First());
            var endCap = TriangulateContourEarClipping(normalizedContours.Last());

            meshTriangles.AddRange(startCap);
            meshTriangles.AddRange(endCap);

            return meshTriangles;
        }

        private static List<Face3D> TriangulateContourEarClipping(List<Vector3> contour)
        {
            List<Face3D> faces = new List<Face3D>();
            if (contour.Count < 3) return faces;

            // 1. ОПРЕДЕЛЯЕМ ПЛОСКОСТЬ КОНТУРА (ищем оси с наибольшим разбросом координат)
            double minX = contour.Min(p => p.X), maxX = contour.Max(p => p.X);
            double minY = contour.Min(p => p.Y), maxY = contour.Max(p => p.Y);
            double minZ = contour.Min(p => p.Z), maxZ = contour.Max(p => p.Z);

            double dx = maxX - minX;
            double dy = maxY - minY;
            double dz = maxZ - minZ;

            // Локальные функции для динамического выбора 2D-координат (U и V)
            Func<Vector3, double> getU;
            Func<Vector3, double> getV;

            if (dx <= dy && dx <= dz)
            {
                getU = p => p.Y; getV = p => p.Z; // Игнорируем X (плоскость YZ)
            }
            else if (dy <= dx && dy <= dz)
            {
                getU = p => p.X; getV = p => p.Z; // Игнорируем Y (плоскость XZ)
            }
            else
            {
                getU = p => p.X; getV = p => p.Y; // Игнорируем Z (плоскость XY)
            }

            // Встроенная локальная функция проверки точки в треугольнике (работает с U и V)
            bool IsPointInTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
            {
                double pU = getU(p), pV = getV(p);
                double aU = getU(a), aV = getV(a);
                double bU = getU(b), bV = getV(b);
                double cU = getU(c), cV = getV(c);

                double det = (bV - cV) * (aU - cU) + (cU - bU) * (aV - cV);
                if (Math.Abs(det) < 1e-9) return false;

                double alpha = ((bV - cV) * (pU - cU) + (cU - bU) * (pV - cV)) / det;
                double beta = ((cV - aV) * (pU - cU) + (aU - cU) * (pV - cV)) / det;
                double gamma = 1.0 - alpha - beta;

                // Небольшой допуск (-1e-9) спасает от багов с плавающей запятой на гранях
                return alpha >= -1e-9 && beta >= -1e-9 && gamma >= -1e-9;
            }

            // НОВОЕ: расстояние между двумя точками в той же 2D-проекции (U,V).
            // Нужно для масштабно-независимой (относительной) проверки угла в вершине.
            double Distance2D(Vector3 p1, Vector3 p2)
            {
                double du = getU(p2) - getU(p1);
                double dv = getV(p2) - getV(p1);
                return Math.Sqrt(du * du + dv * dv);
            }

            // НОВОЕ: расстояние от точки до отрезка (в той же 2D-проекции U,V).
            // Нужно, чтобы отлавливать вершины, которые лежат ВПЛОТНУЮ к диагонали уха,
            // но формально не попадают "строго внутрь" треугольника из-за погрешности.
            double DistancePointToSegment2D(Vector3 p, Vector3 segA, Vector3 segB)
            {
                double pU = getU(p), pV = getV(p);
                double aU = getU(segA), aV = getV(segA);
                double bU = getU(segB), bV = getV(segB);

                double abU = bU - aU, abV = bV - aV;
                double lenSq = abU * abU + abV * abV;
                if (lenSq < 1e-18) return Distance(p, segA);

                double t = ((pU - aU) * abU + (pV - aV) * abV) / lenSq;
                t = Math.Max(0.0, Math.Min(1.0, t));

                double projU = aU + t * abU;
                double projV = aV + t * abV;

                double du = pU - projU, dv = pV - projV;
                return Math.Sqrt(du * du + dv * dv);
            }

            List<int> V = Enumerable.Range(0, contour.Count).ToList();

            // 2. ВЫЧИСЛЯЕМ ПЛОЩАДЬ ДЛЯ ПРОВЕРКИ НАПРАВЛЕНИЯ (в 2D проекции)
            double area = 0;
            for (int i = 0; i < V.Count; i++)
            {
                var p1 = contour[V[i]];
                var p2 = contour[V[(i + 1) % V.Count]];
                area += (getU(p2) - getU(p1)) * (getV(p2) + getV(p1));
            }

            // Если area > 0, контур по часовой стрелке. Разворачиваем, чтобы сделать против часовой (CCW).
            if (area > 0) V.Reverse();

            int count = V.Count;

            // НОВОЕ: минимальный "коридор безопасности" вдоль диагонали уха.
            // Берём как долю от средней длины стороны контура, чтобы не завязываться на абсолютные единицы.
            // Если у вас есть характерный масштаб (например, шаг ресемплинга) — можно подставить его напрямую.
            double avgEdgeLen = 0;
            for (int i = 0; i < count; i++)
                avgEdgeLen += Distance(contour[V[i]], contour[V[(i + 1) % count]]);
            avgEdgeLen /= count;
            double clearance = avgEdgeLen * 0.01; // 1% от средней стороны — подберите под свои данные

            // Ищет лучшее (по длине диагонали) валидное ухо среди текущих вершин V.
            // requireClearance = true  -> строгий режим (запрет "срезов" рядом с зигзагом)
            // requireClearance = false -> классический режим
            // requireConvex    = false -> крайний fallback: не отбраковываем по углу вообще
            //                             (иначе вершина, у которой излом идёт в основном
            //                             по "отброшенной" при проекции оси, может НИКОГДА
            //                             не пройти проверку на выпуклость и остаться дырой)
            (int bestI, double bestDiagLenSq) FindBestEar(bool requireClearance, bool requireConvex)
            {
                int bestI = -1;
                double bestDiagLenSq = double.MaxValue;

                for (int i = 0; i < count; i++)
                {
                    int prevIdx = V[(i - 1 + count) % count];
                    int currIdx = V[i];
                    int nextIdx = V[(i + 1) % count];

                    Vector3 a = contour[prevIdx];
                    Vector3 b = contour[currIdx];
                    Vector3 c = contour[nextIdx];

                    if (requireConvex)
                    {
                        // Векторное произведение в выбранной плоскости
                        double crossProduct = (getU(b) - getU(a)) * (getV(c) - getV(a)) - (getV(b) - getV(a)) * (getU(c) - getU(a));

                        // ВАЖНО: нормируем на длины сторон (получаем аналог sin угла при b).
                        // Абсолютный допуск (1e-6) ошибался на длинных/растянутых контурах —
                        // реально выпуклый, но "мелкий" в этой проекции угол мог считаться коллинеарным.
                        double abLen = Distance2D(a, b);
                        double bcLen = Distance2D(b, c);
                        double denom = abLen * bcLen;
                        double normalizedCross = denom > 1e-15 ? crossProduct / denom : 0;

                        // Если угол вогнутый ИЛИ точки коллинеарны (лежат на прямой) - пропускаем
                        if (normalizedCross <= 1e-9) continue;
                    }

                    bool blocked = false;
                    for (int j = 0; j < count; j++)
                    {
                        int testIdx = V[j];
                        if (testIdx == prevIdx || testIdx == currIdx || testIdx == nextIdx) continue;

                        // 1) классическая проверка "точка строго внутри треугольника"
                        if (IsPointInTriangle(contour[testIdx], a, b, c))
                        {
                            blocked = true;
                            break;
                        }

                        // 2) НОВОЕ: точка слишком близко к диагонали a-c —
                        // запрещаем "срезать" мимо почти коллинеарных / зигзагующих вершин
                        if (requireClearance && DistancePointToSegment2D(contour[testIdx], a, c) < clearance)
                        {
                            blocked = true;
                            break;
                        }
                    }

                    if (!blocked)
                    {
                        double diagLenSq = (getU(a) - getU(c)) * (getU(a) - getU(c)) + (getV(a) - getV(c)) * (getV(a) - getV(c));
                        if (diagLenSq < bestDiagLenSq)
                        {
                            bestDiagLenSq = diagLenSq;
                            bestI = i;
                        }
                    }
                }

                return (bestI, bestDiagLenSq);
            }

            // 3. ОТСЕЧЕНИЕ УШЕЙ (лучшее ухо по длине диагонали, а не первое попавшееся)
            // Триер идёт от самого "аккуратного" варианта к гарантированному fallback-у.
            // ВАЖНО: цикл больше никогда не выходит, не покрыв все вершины треугольниками —
            // именно молчаливый выход раньше и оставлял дыры на углах.
            while (count > 2)
            {
                var (bestI, _) = FindBestEar(requireClearance: true, requireConvex: true);

                if (bestI < 0)
                {
                    // Ни одно ухо не прошло усиленную проверку коридора — пробуем без неё
                    (bestI, _) = FindBestEar(requireClearance: false, requireConvex: true);
                }

                if (bestI < 0)
                {
                    // Даже классическая проверка выпуклости не находит ухо (обычно значит,
                    // что в этой 2D-проекции угол выродился) — снимаем требование выпуклости
                    (bestI, _) = FindBestEar(requireClearance: false, requireConvex: false);
                }

                if (bestI < 0)
                {
                    // Совсем крайний случай (в норме сюда доходить не должны) —
                    // берём первую оставшуюся вершину принудительно, лишь бы не оставить дыру
                    bestI = 0;
                }

                int prevIdx = V[(bestI - 1 + count) % count];
                int currIdx = V[bestI];
                int nextIdx = V[(bestI + 1) % count];

                faces.Add(new Face3D(contour[prevIdx], contour[currIdx], contour[nextIdx]));
                V.RemoveAt(bestI);
                count--;
            }

            return faces;
        }


        private static List<Vector3> ResampleContour(List<Vector3> original, int targetCount)
        {
            if (original.Count == 0) return new List<Vector3>();

            // Если в исходном контуре точек уже больше или равно нужному количеству,
            // просто возвращаем нужное количество (обрезаем лишнее)
            if (original.Count >= targetCount)
            {
                return original.Take(targetCount).ToList();
            }

            // 1. Вычисляем длины всех сегментов и общую длину
            double totalLength = 0;
            List<double> segmentLengths = new List<double>();

            for (int i = 0; i < original.Count; i++)
            {
                int next = (i + 1) % original.Count;
                double dist = Distance(original[i], original[next]);
                segmentLengths.Add(dist);
                totalLength += dist;
            }

            // 2. Рассчитываем, сколько дополнительных точек нужно добавить на каждый сегмент
            int pointsToAdd = targetCount - original.Count; // Сколько точек не хватает до сотни
            int[] pointsPerSegment = new int[original.Count];
            double[] remainders = new double[original.Count];

            for (int i = 0; i < original.Count; i++)
            {
                // Пропорционально распределяем точки в зависимости от длины сегмента
                double exactPoints = (segmentLengths[i] / totalLength) * pointsToAdd;
                pointsPerSegment[i] = (int)Math.Floor(exactPoints);
                remainders[i] = exactPoints - pointsPerSegment[i];
            }

            // Распределяем "остатки", если из-за округления мы недобрали точек до targetCount
            int currentAdded = pointsPerSegment.Sum();
            int neededPoints = pointsToAdd - currentAdded;

            var sortedIndices = remainders
                .Select((val, idx) => new { Value = val, Index = idx })
                .OrderByDescending(x => x.Value)
                .ToList();

            for (int i = 0; i < neededPoints; i++)
            {
                pointsPerSegment[sortedIndices[i].Index]++;
            }

            // 3. Строим новый контур, включая ИСХОДНЫЕ углы и ДОБАВЛЕННЫЕ точки
            List<Vector3> resampled = new List<Vector3>();

            for (int i = 0; i < original.Count; i++)
            {
                Vector3 p1 = original[i];
                Vector3 p2 = original[(i + 1) % original.Count];

                // ГАРАНТИРОВАННО добавляем оригинальный угол контура
                resampled.Add(p1);

                // Добавляем промежуточные точки на прямой линии текущего сегмента
                int extraPoints = pointsPerSegment[i];
                for (int j = 1; j <= extraPoints; j++)
                {
                    double t = (double)j / (extraPoints + 1); // Коэффициент интерполяции

                    double x = p1.X + (p2.X - p1.X) * t;
                    double y = p1.Y + (p2.Y - p1.Y) * t;
                    double z = p1.Z + (p2.Z - p1.Z) * t;

                    resampled.Add(new Vector3(x, y, z));
                }
            }

            return resampled;
        }

        private static List<Vector3> SynchronizeStart(List<Vector3> referenceContour, List<Vector3> targetContour)
        {
            // 1. Ищем лучший сдвиг для прямого направления
            var (forwardSync, forwardDist) = FindBestAlignment(referenceContour, targetContour);

            // 2. Ищем лучший сдвиг для обратного направления (если контур нарисован в другую сторону)
            var reversedTarget = new List<Vector3>(targetContour);
            reversedTarget.Reverse();
            var (reversedSync, reversedDist) = FindBestAlignment(referenceContour, reversedTarget);

            // Выбираем вариант с минимальной суммарной длиной соединений
            if (reversedDist < forwardDist)
            {
                return reversedSync;
            }

            return forwardSync;
        }

        private static (List<Vector3> AlignedContour, double MinDistance) FindBestAlignment(List<Vector3> referenceContour, List<Vector3> targetContour)
        {
            double minTotalDistance = double.MaxValue;
            int bestShift = 0;
            int count = targetContour.Count;

            // Перебираем ВСЕ возможные стартовые точки (индексы сдвига)
            for (int shift = 0; shift < count; shift++)
            {
                double currentTotalDistance = 0;

                for (int i = 0; i < count; i++)
                {
                    int targetIndex = (i + shift) % count;
                    currentTotalDistance += Distance(referenceContour[i], targetContour[targetIndex]);

                    // Оптимизация: если уже набежало больше, чем найденный минимум, дальше не считаем
                    if (currentTotalDistance >= minTotalDistance)
                    {
                        break;
                    }
                }

                // Если нашли более короткий каркас — запоминаем
                if (currentTotalDistance < minTotalDistance)
                {
                    minTotalDistance = currentTotalDistance;
                    bestShift = shift;
                }
            }

            // Перестраиваем массив с найденным лучшим сдвигом
            var synchronized = new List<Vector3>(count);
            for (int i = 0; i < count; i++)
            {
                int index = (i + bestShift) % count;
                synchronized.Add(targetContour[index]);
            }

            return (synchronized, minTotalDistance);
        }

        private static double Distance(Vector3 a, Vector3 b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            double dz = a.Z - b.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }

    public class DrawerObject
    {
        public EntityObject Entity { get; private set; }
        public LinesVisual3D Actual { get; set; }
        public LinesVisual3D LastVisual { get; set; }

        public event Action<DrawerObject>? Changed;

        public DrawerObject(EntityObject entity)
        {
            Entity = entity;

            if (Entity is Polyline3D pl)
            {
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
            else if (Entity is Face3D face)
            {
                var dxfColor = face.Color;
                var lineColor = Color.FromArgb(255, 255, 0, 0);

                Actual = new LinesVisual3D
                {
                    Color = lineColor
                };

                LastVisual = new LinesVisual3D
                {
                    Color = GetDarkColor(lineColor)
                };
            }
            else
            {
                throw new Exception("Supported only Polyline3D and Face3D");
            }
        }

        public void RequestUpdate()
        {
            Changed?.Invoke(this);
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
        public List<DrawerObject> _visuals = new();
        private HelixViewport3D? _viewport;

        public DXFDrawer(List<EntityObject> entities)
        {
            foreach (var entity in entities)
                _visuals.Add(new DrawerObject(entity));
        }

        public void Draw(HelixViewport3D viewport)
        {
            _viewport = viewport; // Сохраняем ссылку на вьюпорт, если это еще не сделано

            foreach (var visual in _visuals)
            {
                // Пропускаем объект, если это не полилиния и не 3D-грань
                if (!(visual.Entity is Polyline3D) && !(visual.Entity is Face3D))
                    continue;

                if (!viewport.Children.Contains(visual.Actual))
                {
                    viewport.Children.Add(visual.Actual);
                    viewport.Children.Add(visual.LastVisual);
                }
            }

            Update();
        }

        public void Build()
        {
            var lines = _visuals
                .Where(x => x.Entity is Polyline3D)
                .Select(x => ((Polyline3D)x.Entity))
                .ToList();

            var meshTriangles = CarcasBuiler.Build(lines);

            if (meshTriangles == null)
                return;

            // 6. Добавление всех треугольников на сцену
            foreach (var face in meshTriangles)
            {
                var newVisual = new DrawerObject(face);
                _visuals.Add(newVisual);

                if (_viewport != null)
                {
                    _viewport.Children.Add(newVisual.Actual);
                    _viewport.Children.Add(newVisual.LastVisual);
                }
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
                if (visual.Entity is Face3D face3D)
                {
                    visual.LastVisual.Points.Clear();

                    foreach (var p in visual.Actual.Points)
                        visual.LastVisual.Points.Add(p);

                    visual.Actual.Points.Clear();

                    // В netDxf вершины Face3D хранятся в отдельных свойствах
                    var p1 = new Point3D(face3D.FirstVertex.X, face3D.FirstVertex.Y, face3D.FirstVertex.Z);
                    var p2 = new Point3D(face3D.SecondVertex.X, face3D.SecondVertex.Y, face3D.SecondVertex.Z);
                    var p3 = new Point3D(face3D.ThirdVertex.X, face3D.ThirdVertex.Y, face3D.ThirdVertex.Z);

                    // Добавляем 3 линии (6 точек), чтобы нарисовать контур треугольника
                    // Линия 1
                    visual.Actual.Points.Add(p1);
                    visual.Actual.Points.Add(p2);
                    // Линия 2
                    visual.Actual.Points.Add(p2);
                    visual.Actual.Points.Add(p3);
                    // Линия 3 (замыкаем обратно на первую вершину)
                    visual.Actual.Points.Add(p3);
                    visual.Actual.Points.Add(p1);
                }

                if (visual.Entity is Polyline3D polyline3D)
                {
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

        private void Build_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _drawer.Build();
        }
    }
}
