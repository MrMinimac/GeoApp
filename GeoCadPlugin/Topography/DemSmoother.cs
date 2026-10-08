using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoCadPlugin.Topography
{
    internal static class ContourSimplifier
    {
        private const double MetersPerDegreeLat = 111_320.0;

        public static List<TopographyLine> Simplify(
            List<TopographyLine> lines,
            double toleranceMeters
        )
        {
            if (lines == null)
                throw new ArgumentNullException(nameof(lines));

            if (toleranceMeters <= 0)
                return lines;

            var result = new List<TopographyLine>(lines.Count);

            foreach (TopographyLine line in lines)
            {
                List<ContourPoint> simplified = SimplifyLine(line, toleranceMeters);

                int minPoints = line.IsClosed ? 3 : 2;

                if (simplified.Count < minPoints)
                    continue;

                result.Add(new TopographyLine(line.Elevation, simplified, line.IsClosed));
            }

            return result;
        }

        private static List<ContourPoint> SimplifyLine(TopographyLine line, double tolerance)
        {
            List<ContourPoint> points = line.Points;

            // Убираем дубль последней точки у замкнутой линии
            if (line.IsClosed && points.Count > 1 && points[0] == points[^1])
            {
                points = points.GetRange(0, points.Count - 1);
            }

            int n = points.Count;

            if (n <= 2)
                return new List<ContourPoint>(points);

            // Локальная метрическая система
            double lat0 = points[0].Latitude;
            double mx = MetersPerDegreeLat * Math.Cos(lat0 * Math.PI / 180.0);
            double my = MetersPerDegreeLat;

            var xy = new (double X, double Y)[n];

            for (int i = 0; i < n; i++)
            {
                xy[i] = (
                    (points[i].Longitude - points[0].Longitude) * mx,
                    (points[i].Latitude - lat0) * my
                );
            }

            var keep = new bool[n];

            if (line.IsClosed)
            {
                // Делим кольцо на две части по самой удалённой от старта точке
                int far = 0;
                double best = -1;

                for (int i = 1; i < n; i++)
                {
                    double d = Dist2(xy[0], xy[i]);
                    if (d > best)
                    {
                        best = d;
                        far = i;
                    }
                }

                keep[0] = true;
                keep[far] = true;

                // Часть 1: 0 .. far
                DouglasPeucker(xy, keep, 0, far, tolerance, n, wrap: false);

                // Часть 2: far .. n (n == индекс 0, замыкание кольца)
                DouglasPeucker(xy, keep, far, n, tolerance, n, wrap: true);
            }
            else
            {
                keep[0] = true;
                keep[n - 1] = true;

                DouglasPeucker(xy, keep, 0, n - 1, tolerance, n, wrap: false);
            }

            var result = new List<ContourPoint>();

            for (int i = 0; i < n; i++)
            {
                if (keep[i])
                    result.Add(points[i]);
            }

            return result;
        }

        /// <summary>
        /// Итеративный Douglas-Peucker (без рекурсии, чтобы не словить StackOverflow на длинных линиях).
        /// wrap = true: индекс n означает точку 0 (замыкание кольца).
        /// </summary>
        private static void DouglasPeucker(
            (double X, double Y)[] xy,
            bool[] keep,
            int first,
            int last,
            double tolerance,
            int n,
            bool wrap
        )
        {
            double tol2 = tolerance * tolerance;

            var stack = new Stack<(int First, int Last)>();
            stack.Push((first, last));

            while (stack.Count > 0)
            {
                (int a, int b) = stack.Pop();

                if (b - a < 2)
                    continue;

                var pa = xy[a % n];
                var pb = xy[b % n];

                double maxD2 = -1;
                int index = -1;

                for (int i = a + 1; i < b; i++)
                {
                    double d2 = PointToSegmentDist2(xy[i % n], pa, pb);

                    if (d2 > maxD2)
                    {
                        maxD2 = d2;
                        index = i;
                    }
                }

                if (index >= 0 && maxD2 > tol2)
                {
                    keep[index % n] = true;

                    stack.Push((a, index));
                    stack.Push((index, b));
                }
            }
        }

        private static double Dist2((double X, double Y) a, (double X, double Y) b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        private static double PointToSegmentDist2(
            (double X, double Y) p,
            (double X, double Y) a,
            (double X, double Y) b
        )
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double len2 = dx * dx + dy * dy;

            if (len2 < 1e-12)
                return Dist2(p, a);

            double t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / len2;
            t = Math.Clamp(t, 0.0, 1.0);

            double px = a.X + t * dx;
            double py = a.Y + t * dy;

            double ex = p.X - px;
            double ey = p.Y - py;

            return ex * ex + ey * ey;
        }
    }

    internal static class ContourSmoother
    {
        private const double MetersPerDegreeLat = 111_320.0;
        private const double Alpha = 0.5; // centripetal
        private const int MaxSubdivisions = 32;

        public static List<TopographyLine> Smooth(
            List<TopographyLine> lines,
            double stepMeters = 10.0
        )
        {
            if (lines == null)
                throw new ArgumentNullException(nameof(lines));

            if (stepMeters <= 0)
                return lines;

            var result = new List<TopographyLine>(lines.Count);

            foreach (TopographyLine line in lines)
            {
                List<ContourPoint> smooth = SmoothLine(line, stepMeters);

                result.Add(new TopographyLine(line.Elevation, smooth, line.IsClosed));
            }

            return result;
        }

        private static List<ContourPoint> SmoothLine(TopographyLine line, double step)
        {
            List<ContourPoint> points = line.Points;

            if (line.IsClosed && points.Count > 1 && points[0] == points[^1])
            {
                points = points.GetRange(0, points.Count - 1);
            }

            int n = points.Count;

            // Слишком мало точек для сплайна
            if (n < 3)
                return new List<ContourPoint>(points);

            double lat0 = points[0].Latitude;
            double lon0 = points[0].Longitude;
            double mx = MetersPerDegreeLat * Math.Cos(lat0 * Math.PI / 180.0);
            double my = MetersPerDegreeLat;

            var p = new (double X, double Y)[n];

            for (int i = 0; i < n; i++)
            {
                p[i] = ((points[i].Longitude - lon0) * mx, (points[i].Latitude - lat0) * my);
            }

            bool closed = line.IsClosed;
            int segmentCount = closed ? n : n - 1;

            var output = new List<ContourPoint>();

            for (int i = 0; i < segmentCount; i++)
            {
                var p1 = p[i];
                var p2 = p[(i + 1) % n];

                (double X, double Y) p0;
                (double X, double Y) p3;

                if (closed)
                {
                    p0 = p[(i - 1 + n) % n];
                    p3 = p[(i + 2) % n];
                }
                else
                {
                    // На концах открытой линии экстраполируем соседа
                    p0 = i == 0 ? Reflect(p1, p2) : p[i - 1];
                    p3 = i + 2 >= n ? Reflect(p2, p1) : p[i + 2];
                }

                double segLen = Math.Sqrt(Dist2(p1, p2));
                int subdivisions = Math.Clamp((int)Math.Ceiling(segLen / step), 1, MaxSubdivisions);

                for (int s = 0; s < subdivisions; s++)
                {
                    double t = (double)s / subdivisions;
                    var q = CatmullRom(p0, p1, p2, p3, t);

                    output.Add(ToGeo(q, lon0, lat0, mx, my));
                }
            }

            // У открытой линии добавляем последнюю точку
            if (!closed)
            {
                output.Add(points[^1]);
            }

            return output;
        }

        /// <summary>
        /// Centripetal Catmull-Rom, формула Барри-Голдмана.
        /// t в [0..1] между p1 и p2.
        /// </summary>
        private static (double X, double Y) CatmullRom(
            (double X, double Y) p0,
            (double X, double Y) p1,
            (double X, double Y) p2,
            (double X, double Y) p3,
            double u
        )
        {
            double t0 = 0.0;
            double t1 = t0 + Knot(p0, p1);
            double t2 = t1 + Knot(p1, p2);
            double t3 = t2 + Knot(p2, p3);

            double t = t1 + (t2 - t1) * u;

            var a1 = Lerp(p0, p1, t0, t1, t);
            var a2 = Lerp(p1, p2, t1, t2, t);
            var a3 = Lerp(p2, p3, t2, t3, t);

            var b1 = Lerp(a1, a2, t0, t2, t);
            var b2 = Lerp(a2, a3, t1, t3, t);

            return Lerp(b1, b2, t1, t2, t);
        }

        private static double Knot((double X, double Y) a, (double X, double Y) b)
        {
            double d = Math.Sqrt(Dist2(a, b));

            // Защита от нулевой длины (совпадающие точки)
            return Math.Max(Math.Pow(d, Alpha), 1e-6);
        }

        private static (double X, double Y) Lerp(
            (double X, double Y) a,
            (double X, double Y) b,
            double ta,
            double tb,
            double t
        )
        {
            double denom = tb - ta;

            if (Math.Abs(denom) < 1e-12)
                return a;

            double wa = (tb - t) / denom;
            double wb = (t - ta) / denom;

            return (a.X * wa + b.X * wb, a.Y * wa + b.Y * wb);
        }

        private static (double X, double Y) Reflect(
            (double X, double Y) from,
            (double X, double Y) over
        )
        {
            // from + (from - over)
            return (2 * from.X - over.X, 2 * from.Y - over.Y);
        }

        private static ContourPoint ToGeo(
            (double X, double Y) q,
            double lon0,
            double lat0,
            double mx,
            double my
        )
        {
            return new ContourPoint(lon0 + q.X / mx, lat0 + q.Y / my);
        }

        private static double Dist2((double X, double Y) a, (double X, double Y) b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }
    }

    internal static class DemSmoother
    {
        private const double NoData = -32768.0;

        // Ядро Гаусса 3x3 (сумма весов = 16)
        private static readonly double[,] Kernel =
        {
            { 1, 2, 1 },
            { 2, 4, 2 },
            { 1, 2, 1 },
        };

        public static DemGrid Smooth(DemGrid dem, int iterations = 1)
        {
            if (dem == null)
                throw new ArgumentNullException(nameof(dem));

            if (iterations <= 0)
                return dem;

            double[,] src = dem.Elevation;

            for (int it = 0; it < iterations; it++)
            {
                src = SmoothOnce(src, dem.Width, dem.Height);
            }

            return new DemGrid(
                dem.Width,
                dem.Height,
                src,
                dem.OriginLongitude,
                dem.OriginLatitude,
                dem.PixelWidth,
                dem.PixelHeight
            );
        }

        private static double[,] SmoothOnce(double[,] src, int width, int height)
        {
            var dst = new double[height, width];

            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    double center = src[row, col];

                    // NoData остаётся NoData
                    if (center <= NoData)
                    {
                        dst[row, col] = center;
                        continue;
                    }

                    double sum = 0;
                    double weightSum = 0;

                    for (int dr = -1; dr <= 1; dr++)
                    {
                        int r = row + dr;
                        if (r < 0 || r >= height)
                            continue;

                        for (int dc = -1; dc <= 1; dc++)
                        {
                            int c = col + dc;
                            if (c < 0 || c >= width)
                                continue;

                            double z = src[r, c];
                            if (z <= NoData)
                                continue;

                            double w = Kernel[dr + 1, dc + 1];
                            sum += z * w;
                            weightSum += w;
                        }
                    }

                    // Веса перенормируются на краях растра и рядом с NoData
                    dst[row, col] = sum / weightSum;
                }
            }

            return dst;
        }
    }
}
