using Clipper2Lib;
using netDxf;
using netDxf.Entities;
using netDxf.Tables;

namespace GeoAppWpf.Services
{
    public class FittingContext
    {
        public List<Vector3> OuterBoundary { get; set; } = new();
        public List<List<Vector3>> Obstacles { get; set; } = new(); // Соседи
    }

    public interface IContourFittingStrategy
    {
        Polyline3D Fit(Polyline3D contour, Polyline3D baseContour, FittingContext context);
    }

    public class MorphToFitStrategy : IContourFittingStrategy
    {
        public double Tolerance { get; set; } = 0.5; // Погрешность площади

        public Polyline3D Fit(Polyline3D contour, Polyline3D baseContour, FittingContext context)
        {
            // Некорректный вход
            if (contour == null || contour.Vertexes.Count < 3)
                return contour;

            if (context == null || context.OuterBoundary == null || context.OuterBoundary.Count < 3)
                return contour;

            double startX = contour.Vertexes[0].X;

            // 1. Доступное пространство
            PathsD availableSpace = BuildAvailableSpace(context);

            if (availableSpace.Count == 0)
                return contour;

            // 2. Исходная площадь
            PathsD subject = ToPathsD(contour.Vertexes);

            double targetArea = Math.Abs(Clipper.Area(subject));

            if (targetArea <= 0)
                return contour;

            // 3. Пересечение с доступной зоной
            PathsD currentShape =
                Clipper.Intersect(
                    subject,
                    availableSpace,
                    FillRule.NonZero);

            currentShape = KeepLargestPolygon(currentShape);

            // Контур полностью вне внешнего каркаса
            if (currentShape.Count == 0 || currentShape[0].Count < 3)
                return contour;

            double currentArea = Math.Abs(Clipper.Area(currentShape));

            // 4. Уже помещается
            if (currentArea >= targetArea - Tolerance)
                return FromPathsD(currentShape, startX, contour);

            // 5. Подгонка МАСШТАБОМ исходного контура от его центра —
            // сохраняет форму (подобие), а не раздувает уже обрезанный остаток
            PointD center = GetCentroid(subject[0]);

            double areaRatio = targetArea / Math.Max(currentArea, 1e-9);
            double minScale = 1.0;                                   // = currentShape/currentArea, уже посчитаны выше
            double maxScale = Math.Max(1.0, Math.Sqrt(areaRatio)) * 2.0; // с запасом, но не «с потолка»

            PathsD bestShape = currentShape;

            for (int i = 0; i < 30; i++)
            {
                double midScale = (minScale + maxScale) / 2.0;

                PathsD scaledSubject = ScaleFrom(subject, center, midScale);

                PathsD constrainedShape =
                    Clipper.Intersect(
                        scaledSubject,
                        availableSpace,
                        FillRule.NonZero);

                constrainedShape = KeepLargestPolygon(constrainedShape);

                if (constrainedShape.Count == 0 || constrainedShape[0].Count < 3)
                {
                    maxScale = midScale; // перебор — контур целиком вылетел
                    continue;
                }

                double testArea = Math.Abs(Clipper.Area(constrainedShape));

                if (Math.Abs(testArea - targetArea) <= Tolerance)
                {
                    bestShape = constrainedShape;
                    break;
                }

                if (testArea < targetArea)
                {
                    minScale = midScale;
                    bestShape = constrainedShape; // на случай, если дальше будет хуже
                }
                else
                {
                    maxScale = midScale;
                }
            }

            return FromPathsD(bestShape, startX, contour);
        }

        private static PointD GetCentroid(PathD path)
        {
            double sx = 0, sy = 0;
            foreach (var p in path) { sx += p.x; sy += p.y; }
            return new PointD(sx / path.Count, sy / path.Count);
        }

        private static PathsD ScaleFrom(PathsD paths, PointD center, double scale)
        {
            var result = new PathsD();
            foreach (var path in paths)
            {
                var scaled = new PathD();
                foreach (var p in path)
                    scaled.Add(new PointD(
                        center.x + (p.x - center.x) * scale,
                        center.y + (p.y - center.y) * scale));
                result.Add(scaled);
            }
            return result;
        }

        private PathsD BuildAvailableSpace(FittingContext context)
        {
            PathsD outer = ToPathsD(context.OuterBoundary);
            if (context.Obstacles.Count == 0) return outer;

            PathsD obstacles = new PathsD();
            foreach (var obs in context.Obstacles)
            {
                obstacles.AddRange(ToPathsD(obs));
            }

            // Вычитаем из внешнего контура все препятствия
            return Clipper.Difference(outer, obstacles, FillRule.NonZero);
        }

        private PathsD KeepLargestPolygon(PathsD paths)
        {
            if (paths == null || paths.Count == 0)
                return new PathsD();

            if (paths.Count == 1)
                return paths[0].Count >= 3
                    ? paths
                    : new PathsD();

            var largest = paths
                .Where(p => p.Count >= 3)
                .OrderByDescending(p => Math.Abs(Clipper.Area(new PathsD { p })))
                .FirstOrDefault();

            if (largest == null)
                return new PathsD();

            return new PathsD { largest };
        }

        private PathsD ToPathsD(IEnumerable<Vector3> points)
        {
            var path = new PathD();
            foreach (var p in points) path.Add(new PointD(p.Y, p.Z)); // Работаем в YZ
            return new PathsD { path };
        }

        private Polyline3D FromPathsD(PathsD paths, double startX, Polyline3D fallback)
        {
            if (paths == null ||
                paths.Count == 0 ||
                paths[0].Count < 3)
            {
                return fallback;
            }

            var result = new Polyline3D();

            foreach (var p in paths[0])
            {
                result.Vertexes.Add(
                    new Vector3(
                        startX,
                        p.x,
                        p.y));
            }

            if (result.Vertexes.Count < 3)
                return fallback;

            return result;
        }
    }

    public class Carcas3D
    {
        private List<List<Vector3>> _normalizedContours = new();
        private List<double> _xPositions = new();
        private List<Face3D> _meshTriangles = new();
        private bool _isBuilt;

        public Guid Id { get; } = Guid.NewGuid();
        public Carcas3D? Parent { get; set; }
        public List<Carcas3D> Children { get; } = new();
        public IReadOnlyList<Polyline3D> InitialContours { get; }
        public IReadOnlyList<IReadOnlyList<Vector3>> NormalizedContours => _normalizedContours;
        public IReadOnlyList<double> XPositions => _xPositions;
        public IReadOnlyList<Face3D> MeshTriangles => _meshTriangles;

        public int TargetPointsCount { get; set; } = 15;
        public Layer Layer { get; set; } = new Layer("0");

        public bool IsBuilt => _isBuilt;


        public Carcas3D(IEnumerable<Polyline3D> initialContours)
        {
            InitialContours = initialContours.ToList();
        }

        public Carcas3D(IEnumerable<Polyline3D> initialContours, Layer layer)
        {
            Layer = layer;
            InitialContours = initialContours.ToList();
        }

        public Carcas3D(IEnumerable<Face3D> faces)
        {
            if (faces == null)
                throw new ArgumentNullException(nameof(faces));

            _meshTriangles = faces.ToList();

            if (_meshTriangles.Count == 0)
                throw new ArgumentException(
                    "Каркас не содержит ни одной грани.",
                    nameof(faces));

            Layer = _meshTriangles[0].Layer;

            // Каркас уже готов — строить его повторно не нужно.
            _isBuilt = true;

            // Для готового каркаса исходных контуров может не быть.
            InitialContours = Array.Empty<Polyline3D>();

            _xPositions = ExtractXPositions(_meshTriangles);
        }

        public Carcas3D Build()
        {
            var allContours = new List<List<Vector3>>();

            foreach (var l in InitialContours)
                allContours.Add(l.Vertexes.ToList());

            if (allContours.Count < 2)
                throw new InvalidOperationException(
                    "Для построения каркаса необходимо минимум два контура.");

            int badContourIndex = allContours.FindIndex(c => c.Count < 3);

            if (badContourIndex >= 0)
                throw new InvalidOperationException(
                    $"Контур №{badContourIndex + 1} из {allContours.Count} содержит меньше 3 вершин " +
                    "(похоже, не удалось вписать экстраполированный контур во внешний каркас) " +
                    "— построение каркаса невозможно.");

            allContours = allContours.OrderBy(contour => contour.Average(v => v.X)).ToList();

            List<List<Vector3>> normalizedContours = new List<List<Vector3>>();

            int targetPointsCount = Math.Max(TargetPointsCount, allContours.Max(c => c.Count));

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

            // 4. Построение боковых граней (между контурами)
            for (int i = 0; i < normalizedContours.Count - 1; i++)
            {
                var contourA = normalizedContours[i];
                var contourB = normalizedContours[i + 1];

                for (int j = 0; j < targetPointsCount; j++)
                {
                    int nextJ = (j + 1) % targetPointsCount;

                    var face1 = new Face3D(contourA[j], contourB[j], contourA[nextJ]);
                    face1.Layer = Layer;

                    var face2 = new Face3D(contourB[j], contourB[nextJ], contourA[nextJ]);
                    face2.Layer = Layer;

                    meshTriangles.Add(face1);
                    meshTriangles.Add(face2);
                }
            }

            // 5. ТРИАНГУЛЯЦИЯ ТОРЦОВ
            var startCap = TriangulateContourEarClipping(normalizedContours.First());
            var endCap = TriangulateContourEarClipping(normalizedContours.Last());

            meshTriangles.AddRange(startCap);
            meshTriangles.AddRange(endCap);

            // НОВОЕ: Собираем X-координаты центров для удобной интерполяции
            List<double> xPositions = normalizedContours.Select(c => c.Average(v => v.X)).ToList();


            _meshTriangles = meshTriangles;
            _normalizedContours = normalizedContours;
            _xPositions = xPositions;

            _isBuilt = true;

            return this;
        }

        public void AddChild(Carcas3D child)
        {
            child.Parent = this;
            Children.Add(child);
        }

        public void RemoveChild(Carcas3D child)
        {
            Children.Remove(child);
            child.Parent = null;
        }

        public List<List<Vector3>> GetAllSections(int countBetween)
        {
            var result = new List<List<Vector3>>();

            if (XPositions.Count < 2)
                return result;

            for (int i = 0; i < XPositions.Count - 1; i++)
            {
                double x1 = XPositions[i];
                double x2 = XPositions[i + 1];

                for (int j = 1; j <= countBetween; j++)
                {
                    double t = (double)j / (countBetween + 1);
                    double x = x1 + (x2 - x1) * t;

                    var section = GetSection(x);

                    if (section.Count >= 3)
                        result.Add(section);
                }
            }

            return result;
        }

        public List<Vector3> GetSection(double x)
        {
            if (!IsBuilt)
                throw new InvalidOperationException(
                    "Каркас еще не построен. Сначала вызовите Build().");

            if (MeshTriangles.Count == 0)
                return new List<Vector3>();

            var segments = new List<(Vector3 A, Vector3 B)>();

            foreach (var triangle in MeshTriangles)
            {
                var segment = IntersectTriangleWithXPlane(triangle, x);

                if (segment.HasValue)
                    segments.Add(segment.Value);
            }

            if (segments.Count == 0)
                return new List<Vector3>();

            return ConnectSegments(segments);
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
                double dist = Vector3.Distance(original[i], original[next]);
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

        private List<Face3D> TriangulateContourEarClipping(List<Vector3> contour)
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
                if (lenSq < 1e-18) return Vector3.Distance(p, segA);

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
                avgEdgeLen += Vector3.Distance(contour[V[i]], contour[V[(i + 1) % count]]);
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
                face.Layer = Layer;

                faces.Add(face);
                V.RemoveAt(bestI);
                count--;
            }

            return faces;
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
                    currentTotalDistance += Vector3.Distance(referenceContour[i], targetContour[targetIndex]);

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

        private static (Vector3 A, Vector3 B)? IntersectTriangleWithXPlane(Face3D triangle, double x)
        {
            var vertices = new[]
            {
                triangle.FirstVertex,
                triangle.SecondVertex,
                triangle.ThirdVertex
            };

            var points = new List<Vector3>();

            // Проверяем каждое ребро треугольника
            for (int i = 0; i < 3; i++)
            {
                var p1 = vertices[i];
                var p2 = vertices[(i + 1) % 3];

                // если ребро пересекает плоскость X
                if ((p1.X <= x && p2.X >= x) ||
                    (p2.X <= x && p1.X >= x))
                {
                    double dx = p2.X - p1.X;

                    if (Math.Abs(dx) < 1e-9)
                        continue;

                    double t = (x - p1.X) / dx;

                    // точка на ребре
                    var point = new Vector3(
                        p1.X + (p2.X - p1.X) * t,
                        p1.Y + (p2.Y - p1.Y) * t,
                        p1.Z + (p2.Z - p1.Z) * t
                    );

                    points.Add(point);
                }
            }


            // треугольник может дать только отрезок
            if (points.Count == 2)
            {
                return (points[0], points[1]);
            }

            return null;
        }

        private static List<Vector3> ConnectSegments(List<(Vector3 A, Vector3 B)> segments)
        {
            var result = new List<Vector3>();

            if (segments.Count == 0)
                return result;


            var first = segments[0];

            result.Add(first.A);
            result.Add(first.B);


            segments.RemoveAt(0);


            while (segments.Count > 0)
            {
                var last = result.Last();

                int index = segments.FindIndex(s =>
                    Vector3.Distance(last, s.A) < 0.001 ||
                    Vector3.Distance(last, s.B) < 0.001);


                if (index < 0)
                    break;


                var next = segments[index];

                segments.RemoveAt(index);


                if (Vector3.Distance(last, next.A) < 0.001)
                {
                    result.Add(next.B);
                }
                else
                {
                    result.Add(next.A);
                }
            }


            // замыкаем контур
            if (result.Count > 2 &&
                Vector3.Distance(result[0], result[^1]) > 0.001)
            {
                result.Add(result[0]);
            }


            return result;
        }

        private static List<double> ExtractXPositions(List<Face3D> faces)
        {
            var xs = faces
                .SelectMany(f => new[]
                {
                    f.FirstVertex.X,
                    f.SecondVertex.X,
                    f.ThirdVertex.X
                })
                .OrderBy(x => x)
                .ToList();

            if (xs.Count == 0)
                return new List<double>();

            const double tolerance = 0.001;

            var result = new List<double>();
            double currentSum = xs[0];
            int currentCount = 1;

            for (int i = 1; i < xs.Count; i++)
            {
                if (Math.Abs(xs[i] - xs[i - 1]) <= tolerance)
                {
                    currentSum += xs[i];
                    currentCount++;
                }
                else
                {
                    result.Add(currentSum / currentCount);

                    currentSum = xs[i];
                    currentCount = 1;
                }
            }

            result.Add(currentSum / currentCount);

            return result;
        }
    }

    public class Extrapolator
    {
        private readonly IContourFittingStrategy _fittingStrategy;
        private readonly Carcas3D? _outerCarcas;
        private readonly Carcas3D? _currentChildCarcas;
        private Polyline3D? _firstExtrapolatedCountour;
        private Polyline3D? _lastExtrapolatedCountour;

        public IReadOnlyList<Polyline3D> InitialContours { get; }
        public Polyline3D? FirstExtrapolatedCountour => _firstExtrapolatedCountour;
        public Polyline3D? LastExtrapolatedCountour => _lastExtrapolatedCountour;
        public double Scale { get; set; } = 0.2;
        public double Distance { get; set; } = 0; // <= 0 = Auto, > 0 = Static


        public Extrapolator(IEnumerable<Polyline3D> polylines, IContourFittingStrategy fittingStrategy)
        {
            InitialContours = polylines.ToList();
            _fittingStrategy = fittingStrategy;
        }

        public Extrapolator(IEnumerable<Polyline3D> polylines, IContourFittingStrategy fittingStrategy, Carcas3D outerCarcas, Carcas3D? currentChildCarcas = null)
        {
            InitialContours = polylines.ToList();
            _fittingStrategy = fittingStrategy;
            _outerCarcas = outerCarcas;
            _currentChildCarcas = currentChildCarcas;
        }

        public IEnumerable<Polyline3D> Extrapolate()
        {
            var sortedIntialsContours = PolylineOperations.Sort(InitialContours).ToList();
            if (sortedIntialsContours.Count == 0) return sortedIntialsContours;

            var centers = PolylineOperations.GetCenters(sortedIntialsContours);
            var firstContourClone = (Polyline3D)sortedIntialsContours.First().Clone();
            var lastContourClone = (Polyline3D)sortedIntialsContours.Last().Clone();

            double distExtrapolateFirst = Distance;
            double distExtrapolateLast = Distance;

            Vector3 firstDirection = new Vector3(-1, 0, 0);
            Vector3 lastDirection = new Vector3(1, 0, 0);

            if (sortedIntialsContours.Count >= 2 && Distance <= 0)
            {
                var c1 = centers[sortedIntialsContours[0]];
                var c2 = centers[sortedIntialsContours[1]];
                var cPreLast = centers[sortedIntialsContours[^2]];
                var cLast = centers[sortedIntialsContours[^1]];

                double dFirst = Vector3.Distance(c1, c2);
                double dLast = Vector3.Distance(cLast, cPreLast);

                distExtrapolateFirst = dFirst / 2.0;
                distExtrapolateLast = dLast / 2.0;

                //if (dFirst > 0.0001)
                //{
                //    firstDirection = c1 - c2;
                //    firstDirection.Normalize();
                //}

                //if (dLast > 0.0001)
                //{
                //    lastDirection = cLast - cPreLast;
                //    lastDirection.Normalize();
                //}
            }
            else if (sortedIntialsContours.Count == 1 && Distance <= 0)
            {
                distExtrapolateFirst = 2.5;
                distExtrapolateLast = 2.5;
            }

            // 1. Перемещаем крайние копии наружу
            PolylineOperations.MovePolyline(firstContourClone, firstDirection * distExtrapolateFirst);
            PolylineOperations.MovePolyline(lastContourClone, lastDirection * distExtrapolateLast);

            // 2. Первичное уменьшение (базовый масштаб)
            PolylineOperations.ChangeScale(firstContourClone, Scale);
            PolylineOperations.ChangeScale(lastContourClone, Scale);

            // 3. Подгонка под внешний каркас
            FitInsideOuterCarcas(firstContourClone, sortedIntialsContours.First());
            FitInsideOuterCarcas(lastContourClone, sortedIntialsContours.Last());

            return [firstContourClone, lastContourClone];
        }

        private void FitInsideOuterCarcas(Polyline3D extrapolated, Polyline3D baseContour)
        {
            if (_outerCarcas == null) return;

            // Получаем внешнюю границу для сечения по оси X
            var outerSection = _outerCarcas.GetSection(extrapolated.Vertexes.First().X);
            if (outerSection.Count < 3) return;

            // Собираем контекст
            var context = new FittingContext { OuterBoundary = outerSection };

            // Ищем соседей (других детей того же внешнего каркаса)
            if (_currentChildCarcas != null)
            {
                var siblings = _currentChildCarcas != null
                    ? _outerCarcas.Children.Where(c => c.Id != _currentChildCarcas.Id)
                    : _outerCarcas.Children;

                foreach (var sibling in siblings)
                {
                    var siblingSection = sibling.GetSection(extrapolated.Vertexes.First().X);
                    if (siblingSection.Count > 2)
                    {
                        context.Obstacles.Add(siblingSection);
                    }
                }
            }

            var newPolyline = _fittingStrategy.Fit(extrapolated, baseContour, context);
            var fittedVertexes = newPolyline.Vertexes.ToList();

            extrapolated.Vertexes.Clear();
            extrapolated.Vertexes.AddRange(fittedVertexes);
        }

        private static List<Vector3>? ShiftToMaintainRelativePosition(Polyline3D tail, Polyline3D baseContour, Carcas3D outerCarcas)
        {
            var baseCenter = PolylineOperations.GetCenter(baseContour);
            var tailCenter = PolylineOperations.GetCenter(tail);

            var baseOuterBoundary = outerCarcas.GetSection(baseCenter.X);
            var tailOuterBoundary = outerCarcas.GetSection(tailCenter.X);

            if (baseOuterBoundary.Count < 3 || tailOuterBoundary.Count < 3)
                return null;

            var baseOuterCenter = PolylineOperations.GetCenter(baseOuterBoundary);
            var tailOuterCenter = PolylineOperations.GetCenter(tailOuterBoundary);

            Vector3 relativeOffset = baseCenter - baseOuterCenter;
            relativeOffset.X = 0;

            double baseHeight = baseOuterBoundary.Max(v => v.Z) - baseOuterBoundary.Min(v => v.Z);
            double tailHeight = tailOuterBoundary.Max(v => v.Z) - tailOuterBoundary.Min(v => v.Z);
            double baseWidth = baseOuterBoundary.Max(v => v.Y) - baseOuterBoundary.Min(v => v.Y);
            double tailWidth = tailOuterBoundary.Max(v => v.Y) - tailOuterBoundary.Min(v => v.Y);

            double scaleY = baseWidth > 0.001 ? tailWidth / baseWidth : 1.0;
            double scaleZ = baseHeight > 0.001 ? tailHeight / baseHeight : 1.0;

            relativeOffset.Y *= Math.Max(0, scaleY);
            relativeOffset.Z *= Math.Max(0, scaleZ);

            Vector3 targetTailCenter = tailOuterCenter + relativeOffset;
            targetTailCenter.X = tailCenter.X;

            Vector3 shiftOffset = targetTailCenter - tailCenter;
            PolylineOperations.MovePolyline(tail, shiftOffset);

            return tailOuterBoundary;
        }
    }

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
            List<Vector3> points = polyline.Vertexes.Select(v => new Vector3(v.X, v.Y, v.Z)).ToList();
            return GetCenter(points);
        }

        public static Vector3 GetCenter(List<Vector3> points)
        {
            double sumX = 0, sumY = 0, sumZ = 0;
            foreach (var p in points)
            {
                sumX += p.X;
                sumY += p.Y;
                sumZ += p.Z;
            }
            return new Vector3(sumX / points.Count, sumY / points.Count, sumZ / points.Count);
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

            for (int i = 0; i < vertexes.Length; i++)
            {
                polyline3D.Vertexes[i] = vertexes[i];
            }
        }

        public static void ChangeScale2(Polyline3D polyline3D, double scale)
        {
            var vertexes = polyline3D.Vertexes.ToArray();

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
                    if (Math.Sqrt(dx * dx + dy * dy) < 0.01)
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

        public static List<Vector3> CloseContour(List<Vector3> points)
        {
            var result = new List<Vector3>(points);

            if (result.Count > 0 &&
                result[0] != result[^1])
            {
                result.Add(result[0]);
            }

            return result;
        }
    }
}
