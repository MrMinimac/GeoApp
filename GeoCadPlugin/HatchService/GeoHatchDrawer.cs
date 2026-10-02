using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Clipper2Lib;
using GeoAppCore.Hatch;

namespace GeoCadPlugin.HatchService
{
    public static class GeoHatchDrawer
    {
        private const double HATCH_UNIT = 0.1;

        // Для отладки: например new() { "Торф" } — штриховать только эти слои. null = все.
        private static readonly HashSet<string>? OnlyTypes = null;

        public static void Draw(DrawContext dc, List<(double X, double Y)> polygon,
            double xOffset, int verticalScale, string layerType)
        {
            if (OnlyTypes != null && !OnlyTypes.Contains(layerType))
                return;

            var config = GeometriesData.LayerHatches
                .FirstOrDefault(kv => string.Equals(kv.Key, layerType, StringComparison.OrdinalIgnoreCase))
                .Value;

            if (config == null)
                return;

            var sw = System.Diagnostics.Stopwatch.StartNew();

            var region = new PathD(polygon.Select(p => new PointD(p.X + xOffset, p.Y * verticalScale)));

            int seed = HatchGenerator.StableSeed(layerType, region);
            var geometry = HatchGenerator.Generate(region, config, HATCH_UNIT, seed);

            if (config.BackgroundColor is { } bg)
                AppendSolidHatch(dc, new PathsD { region }, layerType, bg);

            foreach (var chunk in geometry.FilledChunks)
                AppendSolidHatch(dc, chunk, layerType, config.Color);

            foreach (var stroke in geometry.Strokes)
                AppendPolyline(dc, stroke, layerType, config.Color);

            Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor
                .WriteMessage($"\nШтриховка «{layerType}»: фигур {geometry.Placed}, {sw.ElapsedMilliseconds} мс" +
                              (geometry.Truncated ? " (ОБРЕЗАНО по лимиту)" : ""));
        }

        private static void AppendSolidHatch(DrawContext dc, PathsD loops, string layer, System.Drawing.Color? color)
        {
            Hatch? hatch = null;
            try
            {
                hatch = new Hatch();
                hatch.SetDatabaseDefaults();
                hatch.Layer = layer;

                if (color is { } c)
                    // ИСПРАВЛЕНО: AcColor заменен на Color из пространства имен Autodesk.AutoCAD.Colors
                    hatch.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(c.R, c.G, c.B);

                dc.ModelSpace.AppendEntity(hatch);
                dc.Transaction.AddNewlyCreatedDBObject(hatch, true);

                hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
                hatch.HatchStyle = HatchStyle.Normal;
                hatch.Associative = false;

                bool first = true;
                foreach (var loop in loops)
                {
                    if (loop.Count < 3)
                        continue;

                    var vertices = new Point2dCollection();
                    var bulges = new DoubleCollection();

                    foreach (var p in loop)
                    {
                        vertices.Add(new Point2d(p.x, p.y));
                        bulges.Add(0);
                    }

                    vertices.Add(vertices[0]);
                    bulges.Add(0);

                    hatch.AppendLoop(first ? HatchLoopTypes.Outermost : HatchLoopTypes.Default, vertices, bulges);
                    first = false;
                }

                hatch.EvaluateHatch(true);
            }
            catch (Autodesk.AutoCAD.Runtime.Exception)
            {
                if (hatch != null && !hatch.ObjectId.IsNull && !hatch.IsErased)
                    hatch.Erase();
            }
        }

        private static void AppendPolyline(DrawContext dc, PathD path, string layer, System.Drawing.Color? color)
        {
            if (path.Count < 2)
                return;

            var polyline = new Polyline();
            for (int i = 0; i < path.Count; i++)
                polyline.AddVertexAt(i, new Point2d(path[i].x, path[i].y), 0, 0, 0);

            polyline.Layer = layer;
            if (color is { } c)
                // ИСПРАВЛЕНО: AcColor заменен на Autodesk.AutoCAD.Colors.Color
                polyline.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(c.R, c.G, c.B);

            dc.ModelSpace.AppendEntity(polyline);
            dc.Transaction.AddNewlyCreatedDBObject(polyline, true);
        }
    }
}