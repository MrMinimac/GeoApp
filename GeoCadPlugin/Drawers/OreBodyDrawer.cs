using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeoAppCore;
using GeoAppCore.Services;
using GeoCadPlugin.Managers;

namespace GeoCadPlugin.Drawers
{
    public static class OreBodyDrawer
    {
        public static void Draw(
            DrawContext dc,
            List<SectionBorehole> sections,
            double xOffset,
            int verticalScale,
            double? maxElevationJump = null,
            double extrapolationFraction = 0.5,
            double extrapolationThicknessFraction = 1.0)
        {
            if (sections.Count < 2)
                return;

            var runs = new List<(int StartIndex, int EndIndex)>();
            int? runStart = null;
            double? prevElevation = null;

            for (int i = 0; i < sections.Count; i++)
            {
                var interval = sections[i].OreInterval;

                if (interval == null)
                {
                    if (runStart.HasValue)
                        runs.Add((runStart.Value, i - 1));

                    runStart = null;
                    prevElevation = null;
                    continue;
                }

                double elevation = sections[i].Top - (interval.From + interval.To) / 2;

                bool tooFarByElevation =
                    maxElevationJump.HasValue &&
                    prevElevation.HasValue &&
                    Math.Abs(elevation - prevElevation.Value) > maxElevationJump.Value;

                if (tooFarByElevation && runStart.HasValue)
                {
                    runs.Add((runStart.Value, i - 1));
                    runStart = null;
                }

                runStart ??= i;
                prevElevation = elevation;
            }

            if (runStart.HasValue)
                runs.Add((runStart.Value, sections.Count - 1));

            foreach (var (startIndex, endIndex) in runs)
            {
                DrawRun(
                    dc, sections, startIndex, endIndex,
                    xOffset, verticalScale, extrapolationFraction, extrapolationThicknessFraction);
            }
        }

        private static void DrawRun(
            DrawContext dc,
            List<SectionBorehole> sections,
            int startIndex,
            int endIndex,
            double xOffset,
            int verticalScale,
            double extrapolationFraction,
            double extrapolationThicknessFraction)
        {
            var top = new List<Point2d>();
            var bottom = new List<Point2d>();

            for (int i = startIndex; i <= endIndex; i++)
            {
                var interval = sections[i].OreInterval!;
                double x = sections[i].X + xOffset;

                top.Add(new Point2d(x, (sections[i].Top - interval.From) * verticalScale));
                bottom.Add(new Point2d(x, (sections[i].Top - interval.To) * verticalScale));
            }

            bool hasLeft = TryGetExtrapolatedEdge(
                sections, startIndex, -1,
                extrapolationFraction, extrapolationThicknessFraction, xOffset, verticalScale,
                out var leftTop, out var leftBottom);

            bool hasRight = TryGetExtrapolatedEdge(
                sections, endIndex, +1,
                extrapolationFraction, extrapolationThicknessFraction, xOffset, verticalScale,
                out var rightTop, out var rightBottom);

            var vertices = new List<Point2d>();

            if (hasLeft) vertices.Add(leftTop);
            vertices.AddRange(top);

            if (hasRight)
            {
                vertices.Add(rightTop);
                vertices.Add(rightBottom);
            }

            for (int i = bottom.Count - 1; i >= 0; i--)
                vertices.Add(bottom[i]);

            if (hasLeft) vertices.Add(leftBottom);

            // без экстраполяции одиночная скважина - это 2 точки (верх/низ),
            // контур из них не построить
            if (vertices.Count < 3)
                return;

            var polyline = new Polyline();

            foreach (var v in vertices)
                polyline.AddVertexAt(polyline.NumberOfVertices, v, 0, 0, 0);

            polyline.Closed = true;
            polyline.Layer = LayerManager.GetLayerName(GeoLayers.OreBody);

            dc.ModelSpace.AppendEntity(polyline);
            dc.Transaction.AddNewlyCreatedDBObject(polyline, true);
        }

        private static bool TryGetExtrapolatedEdge(
            List<SectionBorehole> sections,
            int edgeIndex,
            int direction,
            double extrapolationFraction,
            double extrapolationThicknessFraction,
            double xOffset,
            int verticalScale,
            out Point2d topPoint,
            out Point2d bottomPoint)
        {
            topPoint = default;
            bottomPoint = default;

            int neighborIndex = edgeIndex + direction;

            if (neighborIndex < 0 || neighborIndex >= sections.Count)
                return false; // край разреза - экстраполировать не к чему

            var edgeSection = sections[edgeIndex];
            var neighborSection = sections[neighborIndex];
            var edgeInterval = sections[edgeIndex].OreInterval!;

            double distance = Math.Abs(neighborSection.X - edgeSection.X);
            double pinchX = edgeSection.X + direction * distance * extrapolationFraction;

            double midElevation = edgeSection.Top - (edgeInterval.From + edgeInterval.To) / 2;
            double halfThickness = (edgeInterval.To - edgeInterval.From) / 2 * extrapolationThicknessFraction;

            topPoint = new Point2d(pinchX + xOffset, (midElevation + halfThickness) * verticalScale);
            bottomPoint = new Point2d(pinchX + xOffset, (midElevation - halfThickness) * verticalScale);

            return true;
        }
    }
}