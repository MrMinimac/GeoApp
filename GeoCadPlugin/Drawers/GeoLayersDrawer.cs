using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeoAppCore;
using GeoCadPlugin.HatchService;
using GeoCadPlugin.Managers;
using System.Globalization;

namespace GeoCadPlugin.Drawers
{
    public class GeoLayersDrawer
    {
        private const double RIGHT_LEFT_OFFSET = 15;
        private const double PINCH_FRACTION = 0.5;
        private const double CURVE_STEP = 0.5; // шаг дискретизации линий по X, м
        private const double BOTTOM_OFFSET = 0.4;

        public class GeoInterval
        {
            public string Type { get; set; }
            public double TopElevation { get; set; }
            public double BottomElevation { get; set; }
        }

        private class IntervalSegment
        {
            public string Type { get; init; } = "";

            public double LeftX { get; init; }
            public double RightX { get; init; }

            public double LeftBottom { get; init; }
            public double RightBottom { get; init; }

            // Доп. вершины на этой линии (куда упирается выклинивающийся слой)
            public List<(double X, double Y)> Extra { get; } = new();

            // Что является кровлей этого сегмента на каждом участке по X:
            // подошва другого сегмента или поверхность (Owner == null)
            public List<(double XFrom, double XTo, IntervalSegment? Owner)> TopPieces { get; } = new();

            // Кривая подошвы всей цепочки (заполняется после склейки)
            public SmoothCurve? Curve { get; set; }
        }

        /// <summary>
        /// Монотонная кубическая интерполяция (PCHIP): проходит через все точки, без перелётов.
        /// </summary>
        private sealed class SmoothCurve
        {
            private readonly double[] _x;
            private readonly double[] _y;
            private readonly double[] _m;

            public double MinX => _x[0];
            public double MaxX => _x[^1];

            public SmoothCurve(IEnumerable<(double X, double Y)> points)
            {
                var list = new List<(double X, double Y)>();
                foreach (var p in points.OrderBy(p => p.X))
                {
                    if (list.Count > 0 && Math.Abs(list[^1].X - p.X) < 1e-9)
                    {
                        list[^1] = p;
                        continue;
                    }
                    list.Add(p);
                }

                int n = list.Count;
                _x = list.Select(p => p.X).ToArray();
                _y = list.Select(p => p.Y).ToArray();
                _m = new double[n];

                if (n < 2)
                    return;

                var h = new double[n - 1];
                var d = new double[n - 1];
                for (int k = 0; k < n - 1; k++)
                {
                    h[k] = _x[k + 1] - _x[k];
                    d[k] = (_y[k + 1] - _y[k]) / h[k];
                }

                if (n == 2)
                {
                    _m[0] = _m[1] = d[0];
                    return;
                }

                for (int k = 1; k < n - 1; k++)
                {
                    if (d[k - 1] * d[k] <= 0)
                    {
                        _m[k] = 0;
                    }
                    else
                    {
                        double w1 = 2 * h[k] + h[k - 1];
                        double w2 = h[k] + 2 * h[k - 1];
                        _m[k] = (w1 + w2) / (w1 / d[k - 1] + w2 / d[k]);
                    }
                }

                _m[0] = EndSlope(h[0], h[1], d[0], d[1]);
                _m[n - 1] = EndSlope(h[n - 2], h[n - 3], d[n - 2], d[n - 3]);
            }

            private static double EndSlope(double h0, double h1, double d0, double d1)
            {
                double m = ((2 * h0 + h1) * d0 - h0 * d1) / (h0 + h1);
                if (Math.Sign(m) != Math.Sign(d0)) return 0;
                if (Math.Sign(d0) != Math.Sign(d1) && Math.Abs(m) > 3 * Math.Abs(d0)) return 3 * d0;
                return m;
            }

            public double Eval(double x)
            {
                if (x <= _x[0]) return _y[0];
                if (x >= _x[^1]) return _y[^1];

                int k = Array.BinarySearch(_x, x);
                if (k < 0) k = ~k - 1;
                if (k > _x.Length - 2) k = _x.Length - 2;

                double h = _x[k + 1] - _x[k];
                double t = (x - _x[k]) / h;
                double t2 = t * t, t3 = t2 * t;

                return (2 * t3 - 3 * t2 + 1) * _y[k]
                     + (t3 - 2 * t2 + t) * h * _m[k]
                     + (-2 * t3 + 3 * t2) * _y[k + 1]
                     + (t3 - t2) * h * _m[k + 1];
            }

            /// <summary>
            /// Семплы кривой на [x0, x1]: концы + узлы + сетка с шагом step (сетка привязана к 0,
            /// поэтому у разных вызовов совпадает).
            /// </summary>
            public List<(double X, double Y)> Sample(double x0, double x1, double step)
            {
                var result = new List<(double X, double Y)>();

                x0 = Math.Max(x0, MinX);
                x1 = Math.Min(x1, MaxX);
                if (x1 < x0) return result;

                var xs = new SortedSet<double> { x0, x1 };

                foreach (var kx in _x)
                    if (kx > x0 && kx < x1) xs.Add(kx);

                long i0 = (long)Math.Ceiling(x0 / step);
                long i1 = (long)Math.Floor(x1 / step);
                for (long i = i0; i <= i1; i++)
                {
                    double gx = i * step;
                    if (gx > x0 && gx < x1) xs.Add(gx);
                }

                double last = double.NegativeInfinity;
                foreach (var x in xs)
                {
                    if (x - last < 1e-6) continue;
                    result.Add((x, Eval(x)));
                    last = x;
                }

                return result;
            }
        }

        public static void DrawSurfaceAndIntervals(DrawContext dc, List<SectionBorehole> sections, double xOffset, int verticalScale, bool drawHatch)
        {
            if (sections.Count < 2)
                return;

            var boreholeIntervals = sections.Select(ExtractIntervals).ToList();

            // Самый нижний интервал каждой скважины опускаем на BOTTOM_OFFSET
            foreach (var intervals in boreholeIntervals)
            {
                if (intervals.Count == 0) continue;
                intervals.OrderBy(x => x.BottomElevation).First().BottomElevation -= BOTTOM_OFFSET;
            }

            var surfaceExtras = new List<(double X, double Y)>();
            var segments = new List<IntervalSegment>();

            for (int i = 0; i < sections.Count - 1; i++)
            {
                segments.AddRange(BuildPairSegments(
                    sections[i], sections[i + 1],
                    boreholeIntervals[i], boreholeIntervals[i + 1],
                    surfaceExtras));
            }

            double firstX = sections[0].X;
            double lastX = sections[^1].X;

            // Поверхность
            var surfaceCurve = BuildSurfaceCurve(sections, surfaceExtras);
            DrawCurve(dc, surfaceCurve, surfaceCurve.MinX, surfaceCurve.MaxX, xOffset, verticalScale,
                LayerManager.GetLayerName(GeoLayers.Surface));

            // Кривые подошв: сначала для всех цепочек (они ссылаются друг на друга как на кровлю)
            var chains = MergeIntervalSegments(segments);
            foreach (var chain in chains)
            {
                var curve = new SmoothCurve(BuildChainControlPoints(chain, firstX, lastX));
                foreach (var seg in chain)
                    seg.Curve = curve;
            }

            foreach (var chain in chains)
                DrawChain(dc, chain, surfaceCurve, xOffset, verticalScale, firstX, lastX, drawHatch);
        }

        private static SmoothCurve BuildSurfaceCurve(List<SectionBorehole> sections, List<(double X, double Y)> surfaceExtras)
        {
            var pts = sections.Select(s => (X: s.X, Y: s.Top)).ToList();
            pts.AddRange(surfaceExtras);
            pts.Add((sections[0].X - RIGHT_LEFT_OFFSET, sections[0].Top));
            pts.Add((sections[^1].X + RIGHT_LEFT_OFFSET, sections[^1].Top));
            return new SmoothCurve(pts);
        }

        private static List<(double X, double Y)> BuildChainControlPoints(List<IntervalSegment> chain, double firstX, double lastX)
        {
            var pts = new List<(double X, double Y)>();

            for (int i = 0; i < chain.Count; i++)
            {
                var seg = chain[i];

                if (i == 0)
                {
                    if (AreEqual(seg.LeftX, firstX))
                        pts.Add((seg.LeftX - RIGHT_LEFT_OFFSET, seg.LeftBottom));

                    pts.Add((seg.LeftX, seg.LeftBottom));
                }

                foreach (var e in seg.Extra.OrderBy(p => p.X))
                {
                    if (e.X > seg.LeftX && e.X < seg.RightX)
                        pts.Add(e);
                }

                pts.Add((seg.RightX, seg.RightBottom));

                if (i == chain.Count - 1 && AreEqual(seg.RightX, lastX))
                    pts.Add((seg.RightX + RIGHT_LEFT_OFFSET, seg.RightBottom));
            }

            return pts;
        }

        private static void DrawChain(DrawContext dc, List<IntervalSegment> chain, SmoothCurve surface, double xOffset, int verticalScale, double firstX, double lastX, bool drawHatch)
        {
            var type = chain[0].Type;
            var curve = chain[0].Curve!;

            LayerManager.CreateLayer(dc.Database, dc.Transaction, type);

            // Видимая подошва
            DrawCurve(dc, curve, curve.MinX, curve.MaxX, xOffset, verticalScale, type);

            // Штриховка

            if (drawHatch)
            {
                var polygon = BuildHatchPolygon(chain, surface, firstX, lastX);
                if (polygon.Count >= 3)
                    DrawIntervalHatch(dc, polygon, xOffset, verticalScale, type);
            }
        }

        private static void DrawCurve(DrawContext dc, SmoothCurve curve, double xFrom, double xTo, double xOffset, int verticalScale, string layer)
        {
            var samples = curve.Sample(xFrom, xTo, CURVE_STEP);
            if (samples.Count < 2)
                return;

            var polyline = new Polyline();
            for (int i = 0; i < samples.Count; i++)
            {
                polyline.AddVertexAt(i,
                    new Point2d(samples[i].X + xOffset, samples[i].Y * verticalScale),
                    0, 0, 0);
            }

            polyline.Layer = layer;
            dc.ModelSpace.AppendEntity(polyline);
            dc.Transaction.AddNewlyCreatedDBObject(polyline, true);
        }

        private static List<(double X, double Y)> BuildHatchPolygon(List<IntervalSegment> chain, SmoothCurve surface, double firstX, double lastX)
        {
            var top = new List<(double X, double Y)>();
            var pieces = new List<(double X0, double X1, SmoothCurve Curve)>();

            for (int i = 0; i < chain.Count; i++)
            {
                var seg = chain[i];
                bool extendLeft = i == 0 && AreEqual(seg.LeftX, firstX);
                bool extendRight = i == chain.Count - 1 && AreEqual(seg.RightX, lastX);

                for (int p = 0; p < seg.TopPieces.Count; p++)
                {
                    var (xFrom, xTo, owner) = seg.TopPieces[p];

                    if (extendLeft && p == 0) xFrom -= RIGHT_LEFT_OFFSET;
                    if (extendRight && p == seg.TopPieces.Count - 1) xTo += RIGHT_LEFT_OFFSET;

                    var curve = owner?.Curve ?? surface;
                    pieces.Add((xFrom, xTo, curve));
                    top.AddRange(curve.Sample(xFrom, xTo, CURVE_STEP));
                }
            }

            if (top.Count == 0)
                return new();

            double TopY(double x)
            {
                foreach (var pc in pieces)
                {
                    if (x >= pc.X0 - 1e-9 && x <= pc.X1 + 1e-9)
                        return pc.Curve.Eval(x);
                }
                return double.PositiveInfinity;
            }

            var bottomCurve = chain[0].Curve!;
            var bottom = bottomCurve.Sample(bottomCurve.MinX, bottomCurve.MaxX, CURVE_STEP);

            var polygon = new List<(double X, double Y)>(top);
            for (int i = bottom.Count - 1; i >= 0; i--)
            {
                var (x, y) = bottom[i];
                polygon.Add((x, Math.Min(y, TopY(x)))); // подошва не выше кровли
            }

            // Убираем подряд идущие дубли (в точках выклинивания верх == низ)
            var clean = new List<(double X, double Y)>();
            foreach (var p in polygon)
            {
                if (clean.Count == 0 ||
                    Math.Abs(clean[^1].X - p.X) > 1e-9 ||
                    Math.Abs(clean[^1].Y - p.Y) > 1e-9)
                {
                    clean.Add(p);
                }
            }

            if (clean.Count > 1 &&
                Math.Abs(clean[0].X - clean[^1].X) < 1e-9 &&
                Math.Abs(clean[0].Y - clean[^1].Y) < 1e-9)
            {
                clean.RemoveAt(clean.Count - 1);
            }

            // Нулевая площадь — штриховать нечего
            double area = 0;
            for (int i = 0; i < clean.Count; i++)
            {
                var a = clean[i];
                var b = clean[(i + 1) % clean.Count];
                area += a.X * b.Y - b.X * a.Y;
            }

            return Math.Abs(area) / 2 < 1e-6 ? new() : clean;
        }

        private static void DrawIntervalHatch(DrawContext dc, List<(double X, double Y)> polygon, double xOffset, int verticalScale, string type)
                => GeoHatchDrawer.Draw(dc, polygon, xOffset, verticalScale, type);

        private static List<IntervalSegment> BuildPairSegments(SectionBorehole leftSection, SectionBorehole rightSection, List<GeoInterval> left, List<GeoInterval> right, List<(double X, double Y)> surfaceExtras)
        {
            var result = new List<IntervalSegment>();
            var entries = new List<(IntervalSegment Seg, double T0, double T1)>(); // в порядке сверху вниз

            double xA = leftSection.X;
            double xB = rightSection.X;
            double dx = xB - xA;

            double stackA = leftSection.Top;   // подошва последнего слоя в колонке
            double stackB = rightSection.Top;

            double contactA = leftSection.Top; // подошва последнего СКВОЗНОГО слоя
            double contactB = rightSection.Top;
            IntervalSegment? contactOwner = null;

            foreach (var (a, b) in AlignIntervals(left, right))
            {
                if (a != null && b != null)
                {
                    double bottomA = Math.Min(a.BottomElevation, stackA);
                    double bottomB = Math.Min(b.BottomElevation, stackB);

                    var seg = new IntervalSegment
                    {
                        Type = a.Type,
                        LeftX = xA,
                        RightX = xB,
                        LeftBottom = bottomA,
                        RightBottom = bottomB
                    };

                    result.Add(seg);
                    entries.Add((seg, 0.0, 1.0));

                    stackA = contactA = bottomA;
                    stackB = contactB = bottomB;
                    contactOwner = seg;
                }
                else if (a != null)
                {
                    double t = PINCH_FRACTION;
                    double px = xA + dx * t;
                    double contact = contactA + (contactB - contactA) * t;
                    double bottomA = Math.Min(a.BottomElevation, stackA);

                    var seg = new IntervalSegment
                    {
                        Type = a.Type,
                        LeftX = xA,
                        RightX = px,
                        LeftBottom = bottomA,
                        RightBottom = contact
                    };

                    result.Add(seg);
                    entries.Add((seg, 0.0, t));

                    stackA = bottomA;
                    AddContactPoint(contactOwner, surfaceExtras, px, contact);
                }
                else if (b != null)
                {
                    double t = 1.0 - PINCH_FRACTION;
                    double px = xA + dx * t;
                    double contact = contactA + (contactB - contactA) * t;
                    double bottomB = Math.Min(b.BottomElevation, stackB);

                    var seg = new IntervalSegment
                    {
                        Type = b.Type,
                        LeftX = px,
                        RightX = xB,
                        LeftBottom = contact,
                        RightBottom = bottomB
                    };

                    result.Add(seg);
                    entries.Add((seg, t, 1.0));

                    stackB = bottomB;
                    AddContactPoint(contactOwner, surfaceExtras, px, contact);
                }
            }

            // Кровля каждого слоя на каждом участке пролёта:
            // ближайший выше по колонке слой, существующий на этом участке, иначе поверхность.
            var cuts = new SortedSet<double> { 0.0, PINCH_FRACTION, 1.0 - PINCH_FRACTION, 1.0 }.ToList();

            for (int k = 0; k < cuts.Count - 1; k++)
            {
                double t0 = cuts[k], t1 = cuts[k + 1];
                if (t1 - t0 < 1e-9) continue;

                double mid = (t0 + t1) / 2;
                IntervalSegment? above = null;

                foreach (var e in entries)
                {
                    if (mid < e.T0 || mid > e.T1) continue;

                    e.Seg.TopPieces.Add((xA + dx * t0, xA + dx * t1, above));
                    above = e.Seg;
                }
            }

            return result;
        }

        private static void AddContactPoint(IntervalSegment? owner, List<(double X, double Y)> surfaceExtras, double x, double y)
        {
            var list = owner != null ? owner.Extra : surfaceExtras;
            if (list.Any(p => AreEqual(p.X, x) && AreEqual(p.Y, y)))
                return;
            list.Add((x, y));
        }

        private static List<(GeoInterval? A, GeoInterval? B)> AlignIntervals(List<GeoInterval> a, List<GeoInterval> b)
        {
            int n = a.Count, m = b.Count;
            var score = new double[n + 1, m + 1];

            static bool SameType(GeoInterval x, GeoInterval y) =>
                string.Equals(x.Type, y.Type, StringComparison.OrdinalIgnoreCase);

            // Чуть предпочитаем пары с близкой отметкой (только как tie-break)
            static double MatchScore(GeoInterval x, GeoInterval y)
            {
                double d = Math.Abs((x.TopElevation + x.BottomElevation) / 2 - (y.TopElevation + y.BottomElevation) / 2);
                return 1.0 + 1.0 / (1000.0 * (1.0 + d));
            }

            for (int i = n - 1; i >= 0; i--)
            {
                for (int j = m - 1; j >= 0; j--)
                {
                    double best = Math.Max(score[i + 1, j], score[i, j + 1]);
                    if (SameType(a[i], b[j]))
                        best = Math.Max(best, score[i + 1, j + 1] + MatchScore(a[i], b[j]));
                    score[i, j] = best;
                }
            }

            var result = new List<(GeoInterval?, GeoInterval?)>();
            int x = 0, y = 0;

            while (x < n || y < m)
            {
                if (x < n && y < m && SameType(a[x], b[y]) &&
                    Math.Abs(score[x, y] - (score[x + 1, y + 1] + MatchScore(a[x], b[y]))) < 1e-12)
                {
                    result.Add((a[x], b[y]));
                    x++; y++;
                }
                else if (x < n && (y >= m || score[x + 1, y] >= score[x, y + 1]))
                {
                    result.Add((a[x], null));
                    x++;
                }
                else
                {
                    result.Add((null, b[y]));
                    y++;
                }
            }

            return result;
        }

        private static List<List<IntervalSegment>> MergeIntervalSegments(List<IntervalSegment> segments)
        {
            var chains = new List<List<IntervalSegment>>();

            foreach (var seg in segments.OrderBy(s => s.LeftX))
            {
                var chain = chains.FirstOrDefault(c =>
                    string.Equals(c[0].Type, seg.Type, StringComparison.OrdinalIgnoreCase) &&
                    AreEqual(c[^1].RightX, seg.LeftX) &&
                    AreEqual(c[^1].RightBottom, seg.LeftBottom));

                if (chain == null)
                    chains.Add(new List<IntervalSegment> { seg });
                else
                    chain.Add(seg);
            }

            return chains;
        }

        private static bool AreEqual(double a, double b) => Math.Abs(a - b) < 0.000001;

        public static List<GeoInterval> ExtractIntervals(SectionBorehole section)
        {
            var intervals = new List<GeoInterval>();
            const string suffix = " Интервал";

            var types = section.Source.Atributes.Keys
                .Where(k => k.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                .Select(k => k[..^suffix.Length]);

            foreach (var type in types)
            {
                var raw = section.Source.Atributes.GetValueOrDefault($"{type}{suffix}")?.ToString();
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                var parts = raw.Split(new[] { ';', '|', ',' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var part in parts)
                {
                    if (TryParseInterval(part, out double start, out double end))
                    {
                        if (start > end) (start, end) = (end, start);
                        start = Math.Max(start, 0);

                        intervals.Add(new GeoInterval
                        {
                            Type = type,
                            TopElevation = section.Top - start,
                            BottomElevation = section.Top - end
                        });
                    }
                }
            }

            return intervals
                .OrderByDescending(x => x.TopElevation)
                .ThenByDescending(x => x.BottomElevation)
                .ToList();
        }

        private static bool TryParseInterval(string value, out double start, out double end)
        {
            start = 0;
            end = 0;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            var parts = value.Split('-', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
                return false;

            return TryParseDepth(parts[0], out start) && TryParseDepth(parts[1], out end);
        }

        private static bool TryParseDepth(string value, out double depth)
        {
            value = value.Trim().Replace(',', '.');

            return double.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out depth);
        }
    }
}
