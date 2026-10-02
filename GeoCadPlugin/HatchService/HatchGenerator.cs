using Clipper2Lib;
using GeoAppCore.Hatch;
using System.Diagnostics;

namespace GeoCadPlugin.HatchService
{
    public static class HatchGenerator
    {
        private const int P = 4;

        private readonly record struct Box(double MinX, double MinY, double MaxX, double MaxY)
        {
            public bool Overlaps(Box o) =>
                MinX <= o.MaxX && MaxX >= o.MinX && MinY <= o.MaxY && MaxY >= o.MinY;
        }

        private sealed class PlacedShape
        {
            public PathsD Polygons { get; } = new();
            public List<PathD> Strokes { get; } = new();
            public Box Box { get; set; }
            public int Stamp { get; set; }
        }

        private sealed class RegionMask
        {
            private readonly double _minY;
            private readonly double _cell;
            private readonly List<(double X0, double X1)>?[] _rows;

            public RegionMask(PathD region, Box box, double cell)
            {
                double height = box.MaxY - box.MinY;
                _minY = box.MinY;
                _cell = Math.Max(cell, height / 20000.0);

                int rows = Math.Max(1, (int)Math.Ceiling(height / _cell));
                _rows = new List<(double, double)>?[rows];

                var xs = new List<double>();
                for (int r = 0; r < rows; r++)
                {
                    double y = _minY + (r + 0.5) * _cell;
                    xs.Clear();

                    for (int i = 0, j = region.Count - 1; i < region.Count; j = i++)
                    {
                        double yi = region[i].y, yj = region[j].y;
                        if ((yi > y) != (yj > y))
                            xs.Add(region[i].x + (y - yi) / (yj - yi) * (region[j].x - region[i].x));
                    }

                    if (xs.Count < 2) continue;
                    xs.Sort();

                    var list = new List<(double, double)>();
                    for (int k = 0; k + 1 < xs.Count; k += 2)
                        list.Add((xs[k], xs[k + 1]));
                    _rows[r] = list;
                }
            }

            public bool Contains(double x, double y)
            {
                int r = (int)Math.Floor((y - _minY) / _cell);
                if (r < 0 || r >= _rows.Length) return false;

                var row = _rows[r];
                if (row == null) return false;

                foreach (var (x0, x1) in row)
                    if (x >= x0 && x <= x1) return true;

                return false;
            }
        }

        private sealed class SpatialGrid
        {
            private readonly double _cell;
            private readonly Dictionary<(int, int), List<PlacedShape>> _cells = new();
            private int _stamp;

            public SpatialGrid(double cell) => _cell = cell;

            private (int X0, int Y0, int X1, int Y1) Range(Box b) => (
                (int)Math.Floor(b.MinX / _cell), (int)Math.Floor(b.MinY / _cell),
                (int)Math.Floor(b.MaxX / _cell), (int)Math.Floor(b.MaxY / _cell));

            public void Add(PlacedShape s)
            {
                var (x0, y0, x1, y1) = Range(s.Box);
                for (int x = x0; x <= x1; x++)
                {
                    for (int y = y0; y <= y1; y++)
                    {
                        if (!_cells.TryGetValue((x, y), out var list))
                            _cells[(x, y)] = list = new List<PlacedShape>();
                        list.Add(s);
                    }
                }
            }

            public bool Collides(PlacedShape inst, double eps)
            {
                _stamp++;
                var (x0, y0, x1, y1) = Range(inst.Box);

                for (int x = x0; x <= x1; x++)
                {
                    for (int y = y0; y <= y1; y++)
                    {
                        if (!_cells.TryGetValue((x, y), out var list)) continue;

                        foreach (var s in list)
                        {
                            if (s.Stamp == _stamp) continue;
                            s.Stamp = _stamp;

                            if (!s.Box.Overlaps(inst.Box)) continue;

                            var inter = Clipper.Intersect(s.Polygons, inst.Polygons, FillRule.EvenOdd, P);
                            if (inter.Count > 0 && Math.Abs(Clipper.Area(inter)) > eps)
                                return true;
                        }
                    }
                }
                return false;
            }
        }

        private static double ShapeRadius(ParsedShape shape)
        {
            double r2 = 0;
            foreach (var sp in shape.Paths)
                foreach (var p in sp.Points)
                    r2 = Math.Max(r2, p.x * p.x + p.y * p.y);
            return Math.Sqrt(r2);
        }

        private static List<PathsD> ChunkByOuter(PathsD paths, int maxLoops)
        {
            var outers = new List<(PathD Path, Box Box, List<PathD> Holes)>();
            var holes = new List<PathD>();

            foreach (var p in paths)
            {
                if (p.Count < 3) continue;
                if (Clipper.Area(p) >= 0) outers.Add((p, GetBox(p), new List<PathD>()));
                else holes.Add(p);
            }

            foreach (var h in holes)
            {
                var pt = h[0];
                foreach (var o in outers)
                {
                    if (pt.x < o.Box.MinX || pt.x > o.Box.MaxX || pt.y < o.Box.MinY || pt.y > o.Box.MaxY)
                        continue;

                    if (Inside(o.Path, pt.x, pt.y))
                    {
                        o.Holes.Add(h);
                        break;
                    }
                }
            }

            var chunks = new List<PathsD>();
            var cur = new PathsD();

            foreach (var o in outers)
            {
                cur.Add(o.Path);
                cur.AddRange(o.Holes);

                if (cur.Count >= maxLoops)
                {
                    chunks.Add(cur);
                    cur = new PathsD();
                }
            }

            if (cur.Count > 0) chunks.Add(cur);
            return chunks;
        }

        public static HatchGeometry Generate(PathD region, HatchConfig config, double unit, int seed,
    int timeBudgetMs = 4000, int maxShapes = 6000)
        {
            var result = new HatchGeometry();
            if (region.Count < 3 || config.Elements.Count == 0)
                return result;

            var sw = Stopwatch.StartNew();
            var regionPaths = new PathsD { region };
            var regionBox = GetBox(region);
            var rnd = new Random(seed);
            var filledRaw = new PathsD();

            double minStep = double.MaxValue, maxRadius = 0;
            foreach (var el in config.Elements)
            {
                minStep = Math.Min(minStep, Math.Min(el.StepX, el.StepY) * unit);
                maxRadius = Math.Max(maxRadius, ShapeRadius(GeometryParser.Get(el.Geometry)) * el.Scale * unit);
            }

            var mask = new RegionMask(region, regionBox, Math.Max(minStep / 4, 0.05));
            var grid = new SpatialGrid(Math.Max(maxRadius * 2, 0.5));
            int tick = 0;

            foreach (var el in config.Elements)
            {
                var def = GeometriesData.Geometries[el.Geometry];
                var shape = GeometryParser.Get(el.Geometry);

                double scale = el.Scale * unit;
                double stepX = el.StepX * unit;
                double stepY = el.StepY * unit;
                if (stepX <= 0 || stepY <= 0 || scale <= 0)
                    continue;

                double shapeArea = shape.BaseArea * scale * scale;
                double collideEps = 1e-4 * scale * scale;
                int attempts = el.RandomOffset ? Math.Max(1, el.PlacementAttempts) : 1;

                for (double gy = regionBox.MinY; gy < regionBox.MaxY; gy += stepY)
                {
                    for (double gx = regionBox.MinX; gx < regionBox.MaxX; gx += stepX)
                    {
                        for (int a = 0; a < attempts; a++)
                        {
                            if ((++tick & 255) == 0 && sw.ElapsedMilliseconds > timeBudgetMs)
                            {
                                result.Truncated = true;
                                goto Done;
                            }

                            double cx = gx + stepX / 2 + (el.RandomOffset ? (rnd.NextDouble() - 0.5) * stepX : 0);
                            double cy = gy + stepY / 2 + (el.RandomOffset ? (rnd.NextDouble() - 0.5) * stepY : 0);
                            double angle = el.RandomRotation ? rnd.NextDouble() * Math.PI * 2 : 0;

                            // Самое дешёвое отсечение — до создания геометрии
                            if (!mask.Contains(cx, cy)) continue;

                            var inst = Transform(shape, cx, cy, scale, angle);

                            if (!el.IngnoreInrersections && grid.Collides(inst, collideEps)) continue;

                            var clipped = Clipper.Intersect(inst.Polygons, regionPaths, FillRule.EvenOdd, P);
                            double inside = Math.Abs(Clipper.Area(clipped));
                            double fraction = shapeArea > 1e-9 ? 1.0 - inside / shapeArea : 0.0;

                            if (fraction > config.MaxClippedFraction) continue;

                            if (def.Filled)
                                filledRaw.AddRange(clipped);
                            else
                                AddStrokes(result.Strokes, inst, regionPaths, fraction);

                            if (!el.IngnoreInrersections)
                                grid.Add(inst);

                            result.Placed++;
                            if (result.Placed >= maxShapes)
                            {
                                result.Truncated = true;
                                goto Done;
                            }

                            break; // место найдено
                        }
                    }
                }
            }

        Done:
            if (filledRaw.Count > 0)
            {
                var union = Clipper.Union(filledRaw, new PathsD(), FillRule.NonZero, P);
                result.FilledChunks.AddRange(ChunkByOuter(union, 150));
            }

            return result;
        }

        public static int StableSeed(string text, PathD region)
        {
            unchecked
            {
                int h = 17;
                foreach (var ch in text) h = h * 31 + ch;
                if (region.Count > 0)
                {
                    h = h * 31 + (int)Math.Round(region[0].x * 100);
                    h = h * 31 + (int)Math.Round(region[0].y * 100);
                }
                return h;
            }
        }

        private static PlacedShape Transform(ParsedShape shape, double cx, double cy, double scale, double angle)
        {
            double cos = Math.Cos(angle), sin = Math.Sin(angle);
            var placed = new PlacedShape();

            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            foreach (var sp in shape.Paths)
            {
                var path = new PathD();
                foreach (var p in sp.Points)
                {
                    double x = p.x * scale, y = p.y * scale;
                    double rx = cx + x * cos - y * sin;
                    double ry = cy + x * sin + y * cos;
                    path.Add(new PointD(rx, ry));

                    minX = Math.Min(minX, rx); maxX = Math.Max(maxX, rx);
                    minY = Math.Min(minY, ry); maxY = Math.Max(maxY, ry);
                }

                if (path.Count >= 3)
                    placed.Polygons.Add(path);

                var stroke = new PathD(path);
                if (sp.Closed && stroke.Count > 0)
                    stroke.Add(stroke[0]);
                placed.Strokes.Add(stroke);
            }

            placed.Box = new Box(minX, minY, maxX, maxY);
            return placed;
        }

        private static bool Collides(List<PlacedShape> placed, PlacedShape inst, double eps)
        {
            foreach (var p in placed)
            {
                if (!p.Box.Overlaps(inst.Box)) continue;

                var inter = Clipper.Intersect(p.Polygons, inst.Polygons, FillRule.EvenOdd, P);
                if (inter.Count > 0 && Math.Abs(Clipper.Area(inter)) > eps)
                    return true;
            }
            return false;
        }

        private static void AddStrokes(PathsD output, PlacedShape inst, PathsD regionPaths, double fraction)
        {
            if (fraction < 1e-6)
            {
                output.AddRange(inst.Strokes);
                return;
            }

            var clipper = new ClipperD(P);
            clipper.AddOpenSubject(new PathsD(inst.Strokes));
            clipper.AddClip(regionPaths);

            var closedSolution = new PathsD();
            var openSolution = new PathsD();
            clipper.Execute(ClipType.Intersection, FillRule.EvenOdd, closedSolution, openSolution);

            output.AddRange(openSolution);
        }

        private static bool TouchesRegion(PathD region, Box b, double cx, double cy)
        {
            double mx = (b.MinX + b.MaxX) / 2, my = (b.MinY + b.MaxY) / 2;

            var pts = new (double X, double Y)[]
            {
                (cx, cy),
                (b.MinX, b.MinY), (b.MaxX, b.MinY), (b.MinX, b.MaxY), (b.MaxX, b.MaxY),
                (mx, b.MinY), (mx, b.MaxY), (b.MinX, my), (b.MaxX, my)
            };

            foreach (var (x, y) in pts)
                if (Inside(region, x, y)) return true;

            return false;
        }

        private static bool Inside(PathD poly, double x, double y)
        {
            bool inside = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                if ((poly[i].y > y) != (poly[j].y > y) &&
                    x < (poly[j].x - poly[i].x) * (y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        private static Box GetBox(PathD path)
        {
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;
            foreach (var p in path)
            {
                minX = Math.Min(minX, p.x); maxX = Math.Max(maxX, p.x);
                minY = Math.Min(minY, p.y); maxY = Math.Max(maxY, p.y);
            }
            return new Box(minX, minY, maxX, maxY);
        }
    }
}