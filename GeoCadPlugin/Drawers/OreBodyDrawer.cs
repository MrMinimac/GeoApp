using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeoAppCore;
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
            double extrapolationThicknessFraction = 1.0,
            double flagSize = 3)
        {
            if (sections.Count < 2)
                return;

            // Находим максимальную верхнюю точку для высоты флагштока
            var maxTop = sections.Max(x => x.Top);

            var runs = new List<(int StartIndex, int EndIndex)>();
            int? runStart = null;
            double? prevElevation = null;

            for (int i = 0; i < sections.Count; i++)
            {
                var interval = sections[i].OreInterval;

                if (interval == null || interval.ConditionResult == null || !interval.ConditionResult.IsValid)
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

            Point2d? overallLeft = null;
            Point2d? overallRight = null;

            foreach (var (startIndex, endIndex) in runs)
            {
                var (leftTopEdge, rightTopEdge) = DrawRun(
                    dc,
                    sections,
                    startIndex,
                    endIndex,
                    xOffset,
                    verticalScale,
                    extrapolationFraction,
                    extrapolationThicknessFraction);

                // Ищем абсолютные края рудного тела для установки флажков
                if (leftTopEdge.HasValue)
                {
                    if (overallLeft == null || leftTopEdge.Value.X < overallLeft.Value.X)
                        overallLeft = leftTopEdge;
                }

                if (rightTopEdge.HasValue)
                {
                    if (overallRight == null || rightTopEdge.Value.X > overallRight.Value.X)
                        overallRight = rightTopEdge;
                }
            }

            // Отрисовка флажков, если контур был построен
            if (overallLeft.HasValue && overallRight.HasValue)
            {
                double maxY = maxTop * verticalScale;

                // Левый флажок (треугольник смотрит вправо: направление = 1)
                DrawFlag(dc, overallLeft.Value, maxY, flagSize, 1);

                // Правый флажок (треугольник смотрит влево: направление = -1)
                DrawFlag(dc, overallRight.Value, maxY, flagSize, -1);
            }
        }

        // Изменили возвращаемый тип на кортеж, чтобы получить крайние точки отрисованного контура
        private static (Point2d? Left, Point2d? Right) DrawRun(
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
                return (null, null);

            var polyline = new Polyline();

            foreach (var v in vertices)
                polyline.AddVertexAt(polyline.NumberOfVertices, v, 0, 0, 0);

            polyline.Closed = true;
            polyline.Layer = LayerManager.GetLayerName(GeoLayers.OreBody);

            dc.ModelSpace.AppendEntity(polyline);
            dc.Transaction.AddNewlyCreatedDBObject(polyline, true);

            // Возвращаем крайнюю левую и крайнюю правую точки верхней границы
            Point2d runLeftTop = hasLeft ? leftTop : top.First();
            Point2d runRightTop = hasRight ? rightTop : top.Last();

            return (runLeftTop, runRightTop);
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

        // Новый метод для отрисовки флажков
        private static void DrawFlag(DrawContext dc, Point2d basePt, double maxY, double flagSize, int direction)
        {
            // Точка вершины флагштока
            var poleTopPt = new Point2d(basePt.X, maxY);

            // Флагшток (вертикальная линия)
            var pole = new Polyline();
            pole.AddVertexAt(0, basePt, 0, 0, 0);
            pole.AddVertexAt(1, poleTopPt, 0, 0, 0);
            pole.Layer = LayerManager.GetLayerName(GeoLayers.OreBody);

            // Треугольник (флажок)
            var triangle = new Polyline();
            // Точка 1: Верх флагштока
            triangle.AddVertexAt(0, poleTopPt, 0, 0, 0);
            // Точка 2: Вниз по флагштоку на размер flagSize
            triangle.AddVertexAt(1, new Point2d(poleTopPt.X, poleTopPt.Y - flagSize), 0, 0, 0);
            // Точка 3: Острие треугольника, вытянутое в сторону direction
            triangle.AddVertexAt(2, new Point2d(poleTopPt.X + (direction * flagSize), poleTopPt.Y - (flagSize / 2.0)), 0, 0, 0);

            triangle.Closed = true;
            triangle.Layer = LayerManager.GetLayerName(GeoLayers.OreBody);

            dc.ModelSpace.AppendEntity(pole);
            dc.Transaction.AddNewlyCreatedDBObject(pole, true);

            dc.ModelSpace.AppendEntity(triangle);
            dc.Transaction.AddNewlyCreatedDBObject(triangle, true);
        }
    }
}