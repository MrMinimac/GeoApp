using Clipper2Lib;
using GeoAppCore.Hatch;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GeoCadPlugin.HatchService
{
    /// <summary>
    /// Разбор path-разметки (M, L, C, Z — абсолютные, с неявным повтором команд).
    /// Фигура центрируется по bbox, ось Y переворачивается (в XAML Y вниз, в AutoCAD вверх).
    /// </summary>
    public static class GeometryParser
    {
        private const int BEZIER_STEPS = 12;

        private static readonly Regex Token = new(
            @"[A-Za-z]|[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?",
            RegexOptions.Compiled);

        private static readonly Dictionary<GeometryKey, ParsedShape> Cache = new();

        public static ParsedShape Get(GeometryKey key)
        {
            if (!Cache.TryGetValue(key, out var shape))
            {
                shape = Parse(GeometriesData.Geometries[key]);
                Cache[key] = shape;
            }
            return shape;
        }

        public static ParsedShape Parse(GeometryDefinition def)
        {
            var raw = ParseRaw(def.Markup);
            if (raw.Count == 0)
                throw new FormatException("Геометрия не содержит ни одного контура");

            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            foreach (var path in raw)
            {
                foreach (var p in path.Points)
                {
                    minX = Math.Min(minX, p.x); maxX = Math.Max(maxX, p.x);
                    minY = Math.Min(minY, p.y); maxY = Math.Max(maxY, p.y);
                }
            }

            double cx = (minX + maxX) / 2;
            double cy = (minY + maxY) / 2;

            var shape = new ParsedShape();
            foreach (var rp in raw)
            {
                var sp = new ShapePath { Closed = rp.Closed };
                foreach (var p in rp.Points)
                    sp.Points.Add(new PointD(p.x - cx, -(p.y - cy)));
                shape.Paths.Add(sp);
            }

            var polygons = new PathsD(shape.Paths
                .Where(p => p.Points.Count >= 3)
                .Select(p => new PathD(p.Points)));

            shape.BaseArea = polygons.Count == 0
                ? 0
                // ИСПРАВЛЕНО: Добавлен пустой PathsD() вторым аргументом
                : Math.Abs(Clipper.Area(Clipper.Union(polygons, new PathsD(), Clipper2Lib.FillRule.EvenOdd, 4)));

            return shape;
        }

        private static List<ShapePath> ParseRaw(string markup)
        {
            var tokens = Token.Matches(markup).Select(m => m.Value).ToList();
            var paths = new List<ShapePath>();

            ShapePath? cur = null;
            PointD pos = new(0, 0), start = new(0, 0);
            char cmd = '\0';
            int i = 0;

            double Num() => double.Parse(tokens[i++], CultureInfo.InvariantCulture);

            void Flush()
            {
                if (cur != null && cur.Points.Count >= 2)
                    paths.Add(cur);
                cur = null;
            }

            while (i < tokens.Count)
            {
                if (char.IsLetter(tokens[i][0]))
                    cmd = tokens[i++][0];

                switch (cmd)
                {
                    case 'M':
                        {
                            var p = new PointD(Num(), Num());
                            Flush();
                            cur = new ShapePath();
                            cur.Points.Add(p);
                            pos = start = p;
                            cmd = 'L';
                            break;
                        }
                    case 'L':
                        {
                            var p = new PointD(Num(), Num());
                            cur ??= new ShapePath { Points = { pos } };
                            cur.Points.Add(p);
                            pos = p;
                            break;
                        }
                    case 'C':
                        {
                            var c1 = new PointD(Num(), Num());
                            var c2 = new PointD(Num(), Num());
                            var p3 = new PointD(Num(), Num());
                            cur ??= new ShapePath { Points = { pos } };

                            var p0 = pos;
                            for (int k = 1; k <= BEZIER_STEPS; k++)
                            {
                                double t = k / (double)BEZIER_STEPS;
                                double u = 1 - t;
                                double a = u * u * u, b = 3 * u * u * t, c = 3 * u * t * t, d = t * t * t;
                                cur.Points.Add(new PointD(
                                    a * p0.x + b * c1.x + c * c2.x + d * p3.x,
                                    a * p0.y + b * c1.y + c * c2.y + d * p3.y));
                            }

                            pos = p3;
                            break;
                        }
                    case 'Z':
                    case 'z':
                        {
                            if (cur != null)
                            {
                                cur.Closed = true;
                                Flush();
                            }
                            pos = start;
                            cmd = '\0';
                            break;
                        }
                    default:
                        throw new FormatException(
                            $"Неподдерживаемая команда path: '{cmd}' (поддерживаются M, L, C, Z)");
                }
            }

            Flush();
            return paths;
        }
    }
}