using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeoAppCore;
using GeoAppCore.Services;
using GeoCadPlugin.Managers;

namespace GeoCadPlugin.Drawers
{
    public static class OreBodyDrawer
    {
        /// <summary>
        /// Рисует контур основного пласта по разрезу: один замкнутый контур
        /// на каждый непрерывный участок скважин, где пласт есть. Скважины
        /// без пласта (GetMainOreInterval == null) образуют разрыв
        /// (выклинивание) - контур на этом месте обрывается и начинается
        /// заново со следующей скважины, у которой пласт снова есть.
        /// </summary>
        public static void Draw(
            DrawContext dc,
            List<SectionBorehole> sections,
            double xOffset,
            int verticalScale,
            double minGrade,
            double? maxWasteThickness = null)
        {
            if (sections.Count < 2)
                return;

            var runs = new List<List<(SectionBorehole Section, OreInterval Interval)>>();
            List<(SectionBorehole, OreInterval)>? current = null;

            foreach (var section in sections)
            {
                var interval = section.Source.GetMainOreInterval(minGrade, maxWasteThickness);

                if (interval == null)
                {
                    if (current is { Count: > 0 })
                    {
                        runs.Add(current);
                        current = null;
                    }
                    continue;
                }

                current ??= new List<(SectionBorehole, OreInterval)>();
                current.Add((section, interval));
            }

            if (current is { Count: > 0 })
                runs.Add(current);

            foreach (var run in runs)
                DrawRun(dc, run, xOffset, verticalScale);
        }

        private static void DrawRun(
            DrawContext dc,
            List<(SectionBorehole Section, OreInterval Interval)> run,
            double xOffset,
            int verticalScale)
        {
            // одиночная скважина с пластом контуром не рисуется -
            // не с чем соединять по горизонтали
            if (run.Count < 2)
                return;

            var polyline = new Polyline();

            // верхняя граница пласта, слева направо
            foreach (var (section, interval) in run)
            {
                double top = (section.Top - interval.From) * verticalScale;

                polyline.AddVertexAt(
                    polyline.NumberOfVertices,
                    new Point2d(section.X + xOffset, top),
                    0, 0, 0);
            }

            // нижняя граница пласта, справа налево
            for (int i = run.Count - 1; i >= 0; i--)
            {
                var (section, interval) = run[i];
                double bottom = (section.Top - interval.To) * verticalScale;

                polyline.AddVertexAt(
                    polyline.NumberOfVertices,
                    new Point2d(section.X + xOffset, bottom),
                    0, 0, 0);
            }

            polyline.Closed = true;
            polyline.Layer = LayerManager.GetLayerName(GeoLayers.OreBody);

            dc.ModelSpace.AppendEntity(polyline);
            dc.Transaction.AddNewlyCreatedDBObject(polyline, true);
        }
    }
}
