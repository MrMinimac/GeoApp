using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.DatabaseServices.Filters;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System.Diagnostics;

namespace GeoCadPlugin
{
    public class ElevationFindService
    {
        private const double WellSearchRadius = 100.0;

        private const double WellZTolerance = 0.001;
        private const double WellOnLineTolerance = 0.01;
        private const double WellMinHitDist = 0.01;

        private const double WellAxisStepDeg = 5.0;
        private const double WellMinRefineDeg = 0.05;

        // Размер ячейки spatial grid.
        // 50 м хорошо подходит для окна поиска 200 x 200 м.
        private const double WellGridSize = 50.0;


        // Отрезок горизонтали в плоском виде (XY) + её отметка
        private struct WellContourSeg
        {
            public double X1, Y1, X2, Y2, Z;
            public WellContourSeg(double x1, double y1, double x2, double y2, double z)
            {
                X1 = x1;
                Y1 = y1;
                X2 = x2;
                Y2 = y2;
                Z = z;
            }
        }


        // Створ: прямая через скважину,
        // d1/z1 - в одну сторону, d2/z2 - в другую
        private struct WellChord
        {
            public double D1, Z1, D2, Z2;
            public double Length => D1 + D2;
            public bool DiffZ => Math.Abs(Z1 - Z2) > WellZTolerance;
        }

        public static void FindWellElevation(PromptSelectionResult selection)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // --------------------------------------------------------
            // Поиск полилиний
            // --------------------------------------------------------

            TypedValue[] polyFilter =
            {
                new TypedValue((int)DxfCode.Operator, "<OR"),
                new TypedValue((int)DxfCode.Start, "LWPOLYLINE"),
                new TypedValue( (int)DxfCode.Start, "POLYLINE"),
                new TypedValue((int)DxfCode.Operator, "OR>")
            };

            PromptSelectionResult psrPolys = ed.SelectAll(new SelectionFilter(polyFilter));

            if (psrPolys.Status != PromptStatus.OK || psrPolys.Value.Count < 2)
            {
                ed.WriteMessage("\nВ чертеже не найдено достаточного количества горизонталей.");
                return;
            }


            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Stopwatch totalWatch = Stopwatch.StartNew();

                // ----------------------------------------------------
                // Разворачиваем горизонтали в сегменты
                // ----------------------------------------------------

                var segList = new List<WellContourSeg>();
                int contourCount = 0;

                foreach (SelectedObject selObj in psrPolys.Value)
                {
                    Curve? curve = tr.GetObject(
                            selObj.ObjectId,
                            OpenMode.ForRead) as Curve;

                    if (curve == null)
                        continue;

                    if (!curve.Layer.Contains("горизонт", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    contourCount++;

                    AddContourSegments(curve, tr, segList);
                }

                if (contourCount < 2)
                    return;

                // ----------------------------------------------------
                // Строим spatial grid ОДИН РАЗ
                // ----------------------------------------------------

                var grid = new WellSpatialGrid(segList, WellGridSize);

                // ----------------------------------------------------
                // Обрабатываем скважины
                // ----------------------------------------------------

                int updatedCount = 0;
                int skippedCount = 0;

                double R = WellSearchRadius;

                foreach (SelectedObject selCircle in selection.Value)
                {
                    Circle? well = tr.GetObject(selCircle.ObjectId, OpenMode.ForRead) as Circle;

                    if (well == null)
                        continue;

                    Point3d center = well.Center;

                    double px = center.X;
                    double py = center.Y;

                    List<WellContourSeg> local = grid.GetSegments(px, py, R);

                    // ------------------------------------------------
                    // Ближайшая горизонталь
                    // ------------------------------------------------

                    double absMinDist = double.MaxValue;
                    double absZ = 0;


                    for (int i = 0; i < local.Count; i++)
                    {
                        double dist = PointSegmentDistance(px, py, local[i]);

                        if (dist < absMinDist)
                        {
                            absMinDist = dist;
                            absZ = local[i].Z;
                        }
                    }


                    if (local.Count == 0 || absMinDist > R)
                    {
                        ed.WriteMessage($"\nГоризонталей в радиусе {R:F0}м нет, пропущена.");
                        skippedCount++;
                        continue;
                    }


                    // ------------------------------------------------
                    // Скважина прямо на горизонтали
                    // ------------------------------------------------

                    if (absMinDist < WellOnLineTolerance)
                    {
                        SetWellZ(well, absZ);
                        updatedCount++;
                        continue;
                    }


                    // ------------------------------------------------
                    // Поиск лучшего створа
                    // ------------------------------------------------

                    if (TryFindBestChord(px, py, local, out WellChord best))
                    {
                        double totalD = best.D1 + best.D2;
                        double ratio = best.D1 / totalD;
                        double finalZ = best.Z1 + ratio * (best.Z2 - best.Z1);

                        SetWellZ(well, finalZ);
                    }
                    else
                    {
                        // ------------------------------------------------
                        // Резервный вариант:
                        // скважина находится у края карты.
                        // ------------------------------------------------

                        SetWellZ(well, absZ);
                    }


                    updatedCount++;
                }


                tr.Commit();

                ed.WriteMessage(
                    $"\n\nГотово. " +
                    $"Вычислены отметки для " +
                    $"{updatedCount} скважин(ы)." +
                    (skippedCount > 0
                        ? $" Пропущено: {skippedCount}."
                        : ""));
            }
        }

        /// <summary>
        /// Простая равномерная spatial grid для сегментов горизонталей.
        /// </summary>
        private sealed class WellSpatialGrid
        {
            private readonly double _cellSize;

            private readonly Dictionary<(int X, int Y), List<int>> _cells = new();

            private readonly List<WellContourSeg> _segments;

            public WellSpatialGrid(List<WellContourSeg> segments, double cellSize)
            {
                _segments = segments;
                _cellSize = cellSize;

                Build();
            }


            /// <summary>
            /// Строит индекс.
            /// Каждый сегмент добавляется во все ячейки,
            /// которые пересекает его bounding box.
            /// </summary>
            private void Build()
            {
                for (int i = 0; i < _segments.Count; i++)
                {
                    WellContourSeg s = _segments[i];

                    double minX = Math.Min(s.X1, s.X2);
                    double maxX = Math.Max(s.X1, s.X2);
                    double minY = Math.Min(s.Y1, s.Y2);
                    double maxY = Math.Max(s.Y1, s.Y2);

                    int minCellX = GetCellX(minX);
                    int maxCellX = GetCellX(maxX);

                    int minCellY = GetCellY(minY);
                    int maxCellY = GetCellY(maxY);

                    for (int cx = minCellX; cx <= maxCellX; cx++)
                    {
                        for (int cy = minCellY; cy <= maxCellY; cy++)
                        {
                            var key = (cx, cy);

                            if (!_cells.TryGetValue(key, out List<int>? list))
                            {
                                list = new List<int>();
                                _cells.Add(key, list);
                            }

                            list.Add(i);
                        }
                    }
                }
            }


            private int GetCellX(double x)
            {
                return (int)Math.Floor(x / _cellSize);
            }


            private int GetCellY(double y)
            {
                return (int)Math.Floor(y / _cellSize);
            }


            /// <summary>
            /// Возвращает сегменты, которые потенциально попадают
            /// в окно вокруг точки.
            ///
            /// Дубликаты удаляются, потому что один длинный сегмент
            /// может присутствовать сразу в нескольких ячейках.
            /// </summary>
            public List<WellContourSeg> GetSegments(double px, double py, double radius)
            {
                double minX = px - radius;
                double maxX = px + radius;

                double minY = py - radius;
                double maxY = py + radius;

                int minCellX = GetCellX(minX);
                int maxCellX = GetCellX(maxX);

                int minCellY = GetCellY(minY);
                int maxCellY = GetCellY(maxY);

                var result = new List<WellContourSeg>();

                // В обычном случае сегмент попадёт сюда только один раз.
                // Но длинные сегменты могут быть в нескольких ячейках.
                var used = new HashSet<int>();

                for (int cx = minCellX; cx <= maxCellX; cx++)
                {
                    for (int cy = minCellY; cy <= maxCellY; cy++)
                    {
                        if (!_cells.TryGetValue((cx, cy), out List<int>? list))
                            continue;

                        for (int i = 0; i < list.Count; i++)
                        {
                            int segmentIndex = list[i];

                            if (!used.Add(segmentIndex))
                                continue;

                            WellContourSeg s = _segments[segmentIndex];

                            // Финальная проверка bounding box.
                            // Это важно, потому что ячейка сама по себе
                            // может быть внутри общего окна,
                            // а сегмент может лежать за его пределами.
                            if (Math.Max(s.X1, s.X2) < minX ||
                                Math.Min(s.X1, s.X2) > maxX ||
                                Math.Max(s.Y1, s.Y2) < minY ||
                                Math.Min(s.Y1, s.Y2) > maxY)
                            {
                                continue;
                            }

                            result.Add(s);
                        }
                    }
                }

                return result;
            }
        }

        // ============================================================
        // Set Z
        // ============================================================

        private static void SetWellZ(Circle well, double z)
        {
            if (!well.IsWriteEnabled)
                well.UpgradeOpen();

            Point3d c = well.Center;
            well.Center = new Point3d(
                    c.X,
                    c.Y,
                    Math.Round(z, 1));
        }


        // ============================================================
        // Find Best Chord
        // ============================================================

        private static bool TryFindBestChord(double px, double py, List<WellContourSeg> segs, out WellChord best)
        {
            best = default(WellChord);
            double bestAngle = 0;
            bool found = false;
            double step = WellAxisStepDeg * Math.PI / 180.0;
            int axes = (int)Math.Round(180.0 / WellAxisStepDeg);

            for (int i = 0; i < axes; i++)
            {
                double a = i * step;


                if (TryCastChord(px, py, a, segs, out WellChord c) && (!found || IsBetterChord(c, best)))
                {
                    best = c;
                    bestAngle = a;
                    found = true;
                }
            }

            if (!found)
                return false;

            double minDelta = WellMinRefineDeg * Math.PI / 180.0;

            for (double delta = step / 2.0; delta > minDelta; delta /= 2.0)
            {
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    double a = bestAngle + sign * delta;

                    if (TryCastChord(px, py, a, segs, out WellChord c) && IsBetterChord(c, best))
                    {
                        best = c;
                        bestAngle = a;
                    }
                }
            }

            return true;
        }


        // ============================================================
        // Compare chords
        // ============================================================
        private static bool IsBetterChord(WellChord a, WellChord b)
        {
            if (a.DiffZ != b.DiffZ)
                return a.DiffZ;


            return a.Length < b.Length;
        }


        // ============================================================
        // Cast chord
        // ============================================================
        private static bool TryCastChord(double px, double py, double angle, List<WellContourSeg> segs, out WellChord chord)
        {
            double dx = Math.Cos(angle);
            double dy = Math.Sin(angle);
            bool hit1 = CastRay(px, py, dx, dy, segs, WellSearchRadius, out double d1, out double z1);
            bool hit2 = CastRay(px, py, -dx, -dy, segs, WellSearchRadius, out double d2, out double z2);


            chord = new WellChord
            {
                D1 = d1,
                Z1 = z1,
                D2 = d2,
                Z2 = z2
            };

            return hit1 && hit2;
        }


        // ============================================================
        // Cast Ray
        // ============================================================

        private static bool CastRay(
            double px,
            double py,
            double dx,
            double dy,
            List<WellContourSeg> segs,
            double maxDist,
            out double dist,
            out double z)
        {
            dist = maxDist;
            z = 0;

            bool hit = false;


            for (int i = 0; i < segs.Count; i++)
            {
                WellContourSeg s = segs[i];

                double ex =
                    s.X2 - s.X1;

                double ey =
                    s.Y2 - s.Y1;

                double denom =
                    dx * ey -
                    dy * ex;

                // Параллельно
                if (Math.Abs(denom) < 1e-12)
                    continue;

                double wx =
                    s.X1 - px;

                double wy =
                    s.Y1 - py;

                double t =
                    (wx * ey -
                     wy * ex) /
                    denom;


                // За точкой / слишком близко /
                // дальше уже найденного пересечения
                if (t <= WellMinHitDist ||
                    t >= dist)
                {
                    continue;
                }


                double u =
                    (wx * dy -
                     wy * dx) /
                    denom;


                // Не попали в сам отрезок
                if (u < -1e-9 ||
                    u > 1.0 + 1e-9)
                {
                    continue;
                }


                dist = t;
                z = s.Z;
                hit = true;
            }


            return hit;
        }


        // ============================================================
        // Point -> Segment distance
        // ============================================================

        private static double PointSegmentDistance(
            double px,
            double py,
            WellContourSeg s)
        {
            double ex =
                s.X2 - s.X1;

            double ey =
                s.Y2 - s.Y1;


            double len2 =
                ex * ex +
                ey * ey;


            double t =
                len2 > 0
                    ? ((px - s.X1) * ex +
                       (py - s.Y1) * ey) /
                      len2
                    : 0.0;


            t =
                Math.Max(
                    0.0,
                    Math.Min(
                        1.0,
                        t));


            double cx =
                s.X1 +
                t * ex -
                px;

            double cy =
                s.Y1 +
                t * ey -
                py;


            return Math.Sqrt(
                cx * cx +
                cy * cy);
        }


        // ============================================================
        // Add Contour Segments
        // ============================================================

        private static void AddContourSegments(
            Curve curve,
            Transaction tr,
            List<WellContourSeg> dst)
        {
            double z =
                GetElevation(curve);


            var verts =
                new List<(double x, double y, double bulge)>();


            bool closed;


            // --------------------------------------------------------
            // LWPOLYLINE
            // --------------------------------------------------------

            if (curve is Polyline pl)
            {
                for (int i = 0;
                     i < pl.NumberOfVertices;
                     i++)
                {
                    Point2d p =
                        pl.GetPoint2dAt(i);


                    verts.Add(
                        (
                            p.X,
                            p.Y,
                            pl.GetBulgeAt(i)
                        ));
                }


                closed =
                    pl.Closed;
            }


            // --------------------------------------------------------
            // POLYLINE2D
            // --------------------------------------------------------

            else if (
                curve is Polyline2d pl2d &&
                pl2d.PolyType ==
                Poly2dType.SimplePoly)
            {
                foreach (
                    ObjectId vId
                    in pl2d)
                {
                    Vertex2d? v =
                        tr.GetObject(
                            vId,
                            OpenMode.ForRead)
                        as Vertex2d;


                    if (v != null)
                    {
                        verts.Add(
                            (
                                v.Position.X,
                                v.Position.Y,
                                v.Bulge
                            ));
                    }
                }


                closed =
                    pl2d.Closed;
            }


            // --------------------------------------------------------
            // POLYLINE3D
            // --------------------------------------------------------

            else if (
                curve is Polyline3d pl3d &&
                pl3d.PolyType ==
                Poly3dType.SimplePoly)
            {
                foreach (
                    ObjectId vId
                    in pl3d)
                {
                    PolylineVertex3d? v =
                        tr.GetObject(
                            vId,
                            OpenMode.ForRead)
                        as PolylineVertex3d;


                    if (v != null)
                    {
                        verts.Add(
                            (
                                v.Position.X,
                                v.Position.Y,
                                0.0
                            ));
                    }
                }


                closed =
                    pl3d.Closed;
            }


            // --------------------------------------------------------
            // Остальные Curve
            // --------------------------------------------------------

            else
            {
                double startParam =
                    curve.StartParam;

                double endParam =
                    curve.EndParam;


                double length = 0;


                try
                {
                    length =
                        curve.GetDistanceAtParameter(
                            endParam);
                }
                catch
                {
                }


                int numSamples =
                    (int)Math.Min(
                        10000,
                        Math.Max(
                            100,
                            length / 0.5));


                double step =
                    (endParam - startParam) /
                    numSamples;


                Point3d prev =
                    curve.GetPointAtParameter(
                        startParam);


                for (int i = 1;
                     i <= numSamples;
                     i++)
                {
                    Point3d cur =
                        curve.GetPointAtParameter(
                            Math.Min(
                                startParam +
                                i * step,
                                endParam));


                    dst.Add(
                        new WellContourSeg(
                            prev.X,
                            prev.Y,
                            cur.X,
                            cur.Y,
                            z));


                    prev = cur;
                }


                return;
            }


            if (verts.Count < 2)
                return;


            int segCount =
                closed
                    ? verts.Count
                    : verts.Count - 1;


            for (int i = 0;
                 i < segCount;
                 i++)
            {
                var a =
                    verts[i];

                var b =
                    verts[
                        (i + 1) %
                        verts.Count];


                // ----------------------------------------------------
                // Прямая
                // ----------------------------------------------------

                if (Math.Abs(a.bulge) < 1e-9)
                {
                    dst.Add(
                        new WellContourSeg(
                            a.x,
                            a.y,
                            b.x,
                            b.y,
                            z));


                    continue;
                }


                // ----------------------------------------------------
                // Дуговой сегмент
                // ----------------------------------------------------

                double cdx =
                    b.x - a.x;

                double cdy =
                    b.y - a.y;


                double chord =
                    Math.Sqrt(
                        cdx * cdx +
                        cdy * cdy);


                if (chord < 1e-9)
                    continue;


                double theta =
                    4.0 *
                    Math.Atan(a.bulge);


                double radius =
                    chord /
                    (2.0 *
                     Math.Sin(
                         Math.Abs(theta) / 2.0));


                double h =
                    (chord / 2.0) /
                    Math.Tan(theta / 2.0);


                double cx =
                    (a.x + b.x) / 2.0 +
                    (-cdy / chord) * h;


                double cy =
                    (a.y + b.y) / 2.0 +
                    (cdx / chord) * h;


                double a0 =
                    Math.Atan2(
                        a.y - cy,
                        a.x - cx);


                double dTheta =
                    2.0 *
                    Math.Acos(
                        1.0 -
                        Math.Min(
                            1.0,
                            0.001 / radius));


                int n =
                    dTheta > 1e-9
                        ? (int)Math.Ceiling(
                            Math.Abs(theta) /
                            dTheta)
                        : 1;


                n =
                    Math.Min(
                        Math.Max(n, 1),
                        1000);


                double prevX =
                    a.x;

                double prevY =
                    a.y;


                for (int k = 1;
                     k <= n;
                     k++)
                {
                    double curX;
                    double curY;


                    if (k == n)
                    {
                        curX = b.x;
                        curY = b.y;
                    }
                    else
                    {
                        double ang =
                            a0 +
                            theta * k / n;


                        curX =
                            cx +
                            radius *
                            Math.Cos(ang);


                        curY =
                            cy +
                            radius *
                            Math.Sin(ang);
                    }


                    dst.Add(
                        new WellContourSeg(
                            prevX,
                            prevY,
                            curX,
                            curY,
                            z));


                    prevX = curX;
                    prevY = curY;
                }
            }
        }


        // ============================================================
        // Get Elevation
        // ============================================================

        private static double GetElevation(Curve curve)
        {
            if (curve is Polyline pl)
                return pl.Elevation;


            if (curve is Polyline2d pl2d)
                return pl2d.Elevation;


            return curve.StartPoint.Z;
        }
    }
}
