using netDxf;
using netDxf.Entities;
using netDxf.Tables;

namespace GeoAppWpf.Services
{
    public class PolylineOperations
    {
        public static IEnumerable<Polyline3D> Sort(IEnumerable<Polyline3D> polylines)
        {
            var list = polylines.ToList();

            // 1. Умная сортировка: находим центры всех объектов для определения направления

            var centers = GetCenters(list);

            double minX = centers.Values.Min(c => c.X), maxX = centers.Values.Max(c => c.X);
            double minY = centers.Values.Min(c => c.Y), maxY = centers.Values.Max(c => c.Y);
            double minZ = centers.Values.Min(c => c.Z), maxZ = centers.Values.Max(c => c.Z);

            // Определяем ось с наибольшим разбросом, чтобы сортировать по ней
            double rangeX = maxX - minX;
            double rangeY = maxY - minY;
            double rangeZ = maxZ - minZ;

            if (rangeX >= rangeY && rangeX >= rangeZ)
                list = list.OrderBy(p => centers[p].X).ToList();
            else if (rangeY >= rangeX && rangeY >= rangeZ)
                list = list.OrderBy(p => centers[p].Y).ToList();
            else
                list = list.OrderBy(p => centers[p].Z).ToList();

            return list;
        }

        public static Dictionary<Polyline3D, Vector3> GetCenters(IEnumerable<Polyline3D> polylines)
        {
            return polylines.ToDictionary(p => p, p => GetCenter(p));
        }

        public static Vector3 GetCenter(Polyline3D polyline)
        {
            var vertexes = polyline.Vertexes;
            if (vertexes == null || !vertexes.Any())
                return new Vector3(0, 0, 0);

            // Вычисляем среднее арифметическое всех точек полилинии
            double x = vertexes.Average(v => v.X);
            double y = vertexes.Average(v => v.Y);
            double z = vertexes.Average(v => v.Z);

            return new Vector3(x, y, z);
        }

        public static void MovePolyline(Polyline3D polyline, Vector3 offset)
        {
            for (int i = 0; i < polyline.Vertexes.Count; i++)
            {
                var v = polyline.Vertexes[i];
                polyline.Vertexes[i] += offset;
            }
        }

        public static void ChangeScale(Polyline3D polyline3D, double scale)
        {
            var vertexes = polyline3D.Vertexes.ToArray();

            // 1. Группируем индексы по дистанции в плоскости XY (с допуском погрешности 0.05)
            var groups = GroupVerticalvertexes(vertexes);

            if (groups.Count == 0)
                return;

            // 2. Вычисляем геометрический центр контура по X и Y
            double sumX = 0;
            double sumY = 0;

            foreach (var group in groups)
            {
                var v = vertexes[group[0]];
                sumX += v.X;
                sumY += v.Y;
            }

            if (groups.Count == 0) return;

            double centerX = sumX / groups.Count;
            double centerY = sumY / groups.Count;
            double xyScaleFactor = 1.0 - scale;

            // 3. Обрабатываем каждую вертикаль
            foreach (var group in groups)
            {
                var baseV = vertexes[group[0]];
                double dirX = baseV.X - centerX;
                double dirY = baseV.Y - centerY;

                double newX = centerX + (dirX * xyScaleFactor);
                double newY = centerY + (dirY * xyScaleFactor);

                bool hasValidHeight = false;
                double minZ = double.MaxValue;
                double maxZ = double.MinValue;

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

        public static List<List<int>> GroupVerticalvertexes(Vector3[] vertices, double tolerance = 0.05)
        {
            var groups = new List<List<int>>();

            for (int i = 0; i < vertices.Length; i++)
            {
                bool found = false;

                foreach (var group in groups)
                {
                    var v = vertices[group[0]];

                    double dx = v.X - vertices[i].X;
                    double dy = v.Y - vertices[i].Y;

                    if (dx * dx + dy * dy < tolerance * tolerance)
                    {
                        group.Add(i);
                        found = true;
                        break;
                    }
                }

                if (!found)
                    groups.Add([i]);
            }

            return groups;
        }
    }

    public class Extrapolator
    {
        /// <summary>
        /// Выполняет экстраполяцию крайних полилиний относительно существующего набора объектов.
        ///
        /// Метод предназначен для создания дополнительных крайних сечений за пределами
        /// исходного диапазона полилиний. Для этого:
        /// <list type="number">
        /// <item>
        /// Полилинии сортируются в пространственном порядке.
        /// </item>
        /// <item>
        /// Определяются центры первого, второго, предпоследнего и последнего элементов.
        /// </item>
        /// <item>
        /// Крайние полилинии клонируются и смещаются наружу по направлению продолжения ряда.
        /// </item>
        /// <item>
        /// Новые полилинии уменьшаются в масштабе, чтобы обеспечить плавное уменьшение
        /// при удалении от исходных данных.
        /// </item>
        /// </list>
        ///
        /// Если передан только один элемент или набор пустой, метод не выполняет
        /// полноценную экстраполяцию и возвращает исходный результат.
        /// </summary>
        /// <param name="polylines">
        /// Исходный набор трёхмерных полилиний, для которых необходимо создать
        /// дополнительные крайние элементы.
        /// </param>
        /// <param name="defaultDistance">
        /// Расстояние смещения по умолчанию, используемое если невозможно определить
        /// направление и расстояние между соседними полилиниями.
        /// </param>
        /// <param name="scale">
        /// Коэффициент уменьшения масштаба создаваемых крайних полилиний.
        /// Значение 0.1 означает уменьшение размера на 10%.
        /// </param>
        /// <returns>
        /// Коллекция из двух новых полилиний:
        /// первая — продолжение в начале ряда,
        /// вторая — продолжение в конце ряда.
        /// </returns>
        public static IEnumerable<Polyline3D> Extrapolate(IEnumerable<Polyline3D> polylines, double defaultDistance = 0, double scale = 0.2)
        {
            // Сортируем полилинии для определения начала и конца ряда
            var list = PolylineOperations.Sort(polylines).ToList();

            if (list.Count == 0)
                return list;

            // Получаем центры всех полилиний для анализа их взаимного положения
            var centers = PolylineOperations.GetCenters(list);


            // Создаем копии крайних полилиний.
            // Исходные объекты не изменяются.
            var first = (Polyline3D)list.First().Clone();
            var last = (Polyline3D)list.Last().Clone();


            // Расстояние смещения.
            // По умолчанию используется заданное значение,
            // но при наличии нескольких объектов рассчитывается автоматически.
            double distExtrapolateFirst = defaultDistance;
            double distExtrapolateLast = defaultDistance;


            // Направления смещения по умолчанию.
            // Используются только если невозможно вычислить реальное направление.
            Vector3 firstDirection = new Vector3(-1, 0, 0);
            Vector3 lastDirection = new Vector3(1, 0, 0);

            if (list.Count >= 2 && defaultDistance == 0)
            {
                var c1 = centers[list[0]];
                var c2 = centers[list[1]];

                var cPreLast = centers[list[^2]];
                var cLast = centers[list[^1]];


                // Определяем расстояние между соседними центрами.
                // Новые полилинии будут вынесены на половину этого расстояния.
                double dFirst = Vector3.Distance(c1, c2);
                double dLast = Vector3.Distance(cLast, cPreLast);


                distExtrapolateFirst = dFirst / 2.0;
                distExtrapolateLast = dLast / 2.0;


                // Направление продолжения первого элемента.
                // Вектор направлен от второго объекта к первому.
                if (dFirst > 0.0001)
                {
                    firstDirection = c1 - c2;
                    firstDirection.Normalize();
                }


                // Направление продолжения последнего элемента.
                // Вектор направлен от предпоследнего объекта к последнему.
                if (dLast > 0.0001)
                {
                    // lastDirection = cLast - cPreLast;
                    // lastDirection.Normalize();
                }
            }

            // Перемещаем крайние копии наружу относительно исходного ряда
            PolylineOperations.MovePolyline(
                first,
                firstDirection * distExtrapolateFirst);

            PolylineOperations.MovePolyline(
                last,
                lastDirection * distExtrapolateLast);


            // Уменьшаем размеры крайних полилиний,
            // чтобы они плавно переходили в исходную геометрию
            PolylineOperations.ChangeScale(first, scale);
            PolylineOperations.ChangeScale(last, scale);


            // Возвращаем только созданные экстремальные элементы
            return [first, last];
        }
    }

    public class CarcasBuilder
    {
        private static int _carcasId = 0;
        private static Layer _curLayer;

        public static List<Face3D>? Build(IEnumerable<Polyline3D> polylines)
        {
            _carcasId++;
            string blockName = polylines.Count() < 4 ? "C2" : "C1";
            _curLayer = new Layer($"{_carcasId}-{blockName}");

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

                    var face1 = new Face3D(contourA[j], contourB[j], contourA[nextJ]);
                    face1.Layer = _curLayer;

                    var face2 = new Face3D(contourB[j], contourB[nextJ], contourA[nextJ]);
                    face2.Layer = _curLayer;

                    meshTriangles.Add(face1);
                    meshTriangles.Add(face2);
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

                var face = new Face3D(contour[prevIdx], contour[currIdx], contour[nextIdx]);
                face.Layer = _curLayer;

                faces.Add(face);
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
}
