using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeoAppCore;
using GeoAppCore.Models;
using GeoCadPlugin.Managers;
using System.Globalization;

namespace GeoCadPlugin.Drawers
{
    public class GeoSectionDrawer
    {
        private const double HEADER_Y_OFFSET = 20;
        private const double RULER_X_OFFSET = -20;
        private const double TABLE_START_X_OFFSET = -110;
        private const double TABLE_END_X_OFFSET = 15;
        private const double SECTIONS_SPACING = 15;
        private const double BOTTOM_OFFSET = 0.4;
        private const double RIGHT_LEFT_OFFSET = 15;

        public static void Draw(GeoDoc project)
        {
            double xOffset = 0; // Смещенее разреза

            var editor = Application.DocumentManager.MdiActiveDocument.Editor;
            Database db = Application.DocumentManager.MdiActiveDocument.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var dc = new DrawContext(db, tr, ms);

                var rulerDrawer = new RulerDrawer(dc);
                rulerDrawer.VerticalScale = project.VerticalScale;

                LayerManager.CreateLayers(dc.Database, dc.Transaction, [GeoLayers.Header, GeoLayers.Surface, GeoLayers.Litologies, GeoLayers.OreBody]);

                foreach (var line in project.BoreholeLines)
                {
                    var sections = line.BuildSections();

                    // Заголовок
                    var headerX = (sections[^1].X - sections[0].X) / 2;
                    var headerY = line.MaxZ;
                    DrawHeader(dc, line, headerX, headerY, xOffset, project.VerticalScale);

                    // Скважины
                    var cbhDrawer = new SectionBoreholeDrawer(dc);
                    cbhDrawer.VerticalScale = project.VerticalScale;
                    foreach (var cbh in sections)
                    {
                        cbhDrawer.Draw(cbh, xOffset);
                    }

                    // Поверхность
                    DrawSurface(dc, sections, xOffset, project.VerticalScale);

                    // DrawLithologies(dc, sections, xOffset, project.VerticalScale);

                    DrawIntervals(dc, sections, xOffset, project.VerticalScale);

                    // Линейка
                    rulerDrawer.DrawVertRuler(RULER_X_OFFSET + xOffset, line.MinZ, line.MaxZ);

                    // Таблица
                    var startX = sections[0].X;
                    var endX = sections[^1].X;
                    var tableStartX = startX + TABLE_START_X_OFFSET;
                    var tableEndX = endX + TABLE_END_X_OFFSET;
                    var tableStartY = Math.Floor(line.MinZ / rulerDrawer.ValuesStep) * rulerDrawer.ValuesStep;
                    DrawTable(sections, dc, tableStartX, tableEndX, tableStartY, xOffset, project.VerticalScale);

                    // считаем реальную ширину блока
                    double blockMinX = Math.Min(startX + TABLE_START_X_OFFSET, RULER_X_OFFSET);
                    double blockMaxX = Math.Max(endX, tableEndX);
                    double blockWidth = blockMaxX - blockMinX;

                    OreBodyDrawer.Draw(
                        dc, sections, xOffset, project.VerticalScale,
                        maxElevationJump: 5,
                        extrapolationFraction: 0.5,
                        extrapolationThicknessFraction: 0.5);

                    DrawSideLines(dc, sections, xOffset, project.VerticalScale);

                    // Добавляем смещение
                    xOffset += blockWidth + SECTIONS_SPACING;
                }

                tr.Commit();
            }

            //editor.WriteMessage(
            //    $"\nИмпорт документа.\n" +
            //    $"Буровых линий: {project.BoreholeLines.Count}\n" +
            //    $"Общее кол-во скважин: {project.BoreholeLines.Sum(x => x.Boreholes.Count)}\n" +
            //    $"Общее кол-во проб: {project.BoreholeLines.Sum(x => x.Boreholes.Sum(x => x.SamplesCount))}\n");
        }

        private static void DrawSideLines(DrawContext dc, List<SectionBorehole> sections, double xOffset, int verticalScale)
        {
            if (sections.Count == 0)
                return;

            // Рисуем левую границу (для sections[0] со смещением -RIGHT_LEFT_OFFSET)
            DrawSideLineForBorehole(dc, sections[0], -RIGHT_LEFT_OFFSET, xOffset, verticalScale);

            // Рисуем правую границу (для sections[^1] со смещением +RIGHT_LEFT_OFFSET)
            DrawSideLineForBorehole(dc, sections[^1], RIGHT_LEFT_OFFSET, xOffset, verticalScale);
        }

        private static void DrawSideLineForBorehole(DrawContext dc, SectionBorehole section, double sideOffset, double xOffset, int verticalScale)
        {
            double edgeX = (section.X + sideOffset) + xOffset;
            double surfaceY = section.Top * verticalScale;
            Point3d surfacePoint = new Point3d(edgeX, surfaceY, 0);

            var intervals = ExtractIntervals(section);
            if (intervals.Count == 0)
                return;

            var lowestInterval = intervals.OrderBy(x => x.BottomElevation).FirstOrDefault();
            if (lowestInterval == null)
                return;

            double intervalBottomElevation = lowestInterval.BottomElevation - BOTTOM_OFFSET;
            Point3d intervalPoint = new Point3d(edgeX, intervalBottomElevation * verticalScale, 0);

            Line boundaryLine = new Line(surfacePoint, intervalPoint)
            {
                Layer = LayerManager.GetLayerName(GeoLayers.Surface)
            };

            dc.ModelSpace.AppendEntity(boundaryLine);
            dc.Transaction.AddNewlyCreatedDBObject(boundaryLine, true);
        }

        private static void DrawLithologies(DrawContext dc, List<SectionBorehole> sections, double xOffset, int verticalScale)
        {
            if (sections.Count < 2)
                return;

            var lithologies = sections
                .SelectMany(x => x.LithologiesIntervals)
                .Select(x => x.Lithologies)
                .Distinct(new LithologyComparer())
                .ToList();


            var layers = BuildLayers(sections, lithologies);

            foreach (var layer in layers)
            {
                DrawLayer(dc, layer, xOffset, verticalScale);
            }
        }

        private static void DrawLayer(DrawContext dc, GeologicalLayer layer, double xOffset, int verticalScale)
        {
            if (layer.Points.Count < 2)
                return;

            var polyline = new Polyline();

            foreach (var point in layer.Points)
            {
                polyline.AddVertexAt(
                    polyline.NumberOfVertices,
                    new Point2d(
                        point.X + xOffset,
                        point.Top * verticalScale),
                    0, 0, 0);
            }


            for (int i = layer.Points.Count - 1; i >= 0; i--)
            {
                var point = layer.Points[i];

                polyline.AddVertexAt(
                    polyline.NumberOfVertices,
                    new Point2d(
                        point.X + xOffset,
                        point.Bottom * verticalScale),
                    0, 0, 0);
            }


            polyline.Closed = true;

            polyline.Layer = LayerManager.GetLayerName(GeoLayers.Litologies);
            dc.ModelSpace.AppendEntity(polyline);
            dc.Transaction.AddNewlyCreatedDBObject(polyline, true);
        }

        private static List<GeologicalLayer> BuildLayers(List<SectionBorehole> sections, List<List<Lithology>> lithologies)
        {
            var layers = new List<GeologicalLayer>();

            foreach (var lithologySet in lithologies)
            {
                var layer = new GeologicalLayer
                {
                    Lithologies = new List<Lithology>(lithologySet)
                };

                foreach (var section in sections)
                {
                    double depth = 0;

                    foreach (var interval in section.LithologiesIntervals)
                    {
                        if (interval.Lithologies
                            .OrderBy(x => x)
                            .SequenceEqual(
                                lithologySet.OrderBy(x => x)))
                        {
                            layer.Points.Add(new LayerPoint
                            {
                                X = section.X,
                                Top = section.Top - depth,
                                Bottom = section.Top - depth - interval.Length
                            });

                            break;
                        }

                        depth += interval.Length;
                    }
                }

                layers.Add(layer);
            }

            return layers;
        }

        #region Surface
        private static void DrawSurface(DrawContext dc, List<SectionBorehole> sections, double xOffset, int verticalScale)
        {
            if (sections.Count < 2)
                return;

            var points = new Point3dCollection();

            // 1. Левый отступ RIGHT_LEFT_OFFSET для поверхности
            double firstX = (sections[0].X - RIGHT_LEFT_OFFSET) + xOffset;
            double firstY = sections[0].Top * verticalScale;
            points.Add(new Point3d(firstX, firstY, 0));

            // 2. Вершины поверхности по скважинам
            for (int i = 0; i < sections.Count; i++)
            {
                double x = sections[i].X + xOffset;
                double y = sections[i].Top * verticalScale;
                points.Add(new Point3d(x, y, 0));
            }

            // 3. Правый отступ RIGHT_LEFT_OFFSET для поверхности
            double lastX = (sections[^1].X + RIGHT_LEFT_OFFSET) + xOffset;
            double lastY = sections[^1].Top * verticalScale;
            Point3d rightSurfacePoint = new Point3d(lastX, lastY, 0);
            points.Add(rightSurfacePoint);

            // 4. Создаем сглаженную 2D-полилинию поверхности (сглаживание CurveFit)
            using (var polyline2d = new Polyline2d(Poly2dType.SimplePoly, points, 0, false, 0, 0, null))
            {
                polyline2d.CurveFit();
                polyline2d.Layer = LayerManager.GetLayerName(GeoLayers.Surface);

                dc.ModelSpace.AppendEntity(polyline2d);
                dc.Transaction.AddNewlyCreatedDBObject(polyline2d, true);
            }
        }
        #endregion

        #region DrawIntervals

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

            public double LeftTop { get; init; }
            public double LeftBottom { get; init; }

            public double RightTop { get; init; }
            public double RightBottom { get; init; }
        }

        private static void DrawIntervals(DrawContext dc, List<SectionBorehole> sections, double xOffset, int verticalScale)
        {
            if (sections.Count < 2)
                return;

            var boreholeIntervals = sections.Select(ExtractIntervals).ToList();

            foreach (var intervals in boreholeIntervals)
            {
                if (intervals.Count == 0) continue;

                var lowestInterval = intervals.OrderBy(x => x.BottomElevation).FirstOrDefault();
                if (lowestInterval != null)
                {
                    lowestInterval.BottomElevation -= 0.4;
                }
            }

            var segments = new List<IntervalSegment>();

            for (int i = 0; i < sections.Count - 1; i++)
            {
                var leftSection = sections[i];
                var rightSection = sections[i + 1];

                var leftIntervals = boreholeIntervals[i];
                var rightIntervals = boreholeIntervals[i + 1];

                var types = leftIntervals.Select(x => x.Type)
                    .Union(rightIntervals.Select(x => x.Type))
                    .Distinct(StringComparer.OrdinalIgnoreCase);

                foreach (var type in types)
                {
                    var leftOfType = leftIntervals
                        .Where(x => string.Equals(x.Type, type, StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(x => x.TopElevation)
                        .ToList();

                    var rightOfType = rightIntervals
                        .Where(x => string.Equals(x.Type, type, StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(x => x.TopElevation)
                        .ToList();

                    int maxCount = Math.Max(leftOfType.Count, rightOfType.Count);

                    for (int j = 0; j < maxCount; j++)
                    {
                        bool hasLeft = j < leftOfType.Count;
                        bool hasRight = j < rightOfType.Count;

                        if (hasLeft && hasRight)
                        {
                            var left = leftOfType[j];
                            var right = rightOfType[j];

                            segments.Add(new IntervalSegment
                            {
                                Type = type,
                                LeftX = leftSection.X,
                                RightX = rightSection.X,
                                LeftTop = left.TopElevation,
                                LeftBottom = left.BottomElevation,
                                RightTop = right.TopElevation,
                                RightBottom = right.BottomElevation
                            });
                        }
                        else if (hasLeft && !hasRight)
                        {
                            var left = leftOfType[j];
                            double rightPinchElevation = GetPinchElevation(left, leftIntervals, rightIntervals);

                            segments.Add(new IntervalSegment
                            {
                                Type = type,
                                LeftX = leftSection.X,
                                RightX = rightSection.X,
                                LeftTop = left.TopElevation,
                                LeftBottom = left.BottomElevation,
                                RightTop = rightPinchElevation,
                                RightBottom = rightPinchElevation
                            });
                        }
                        else if (!hasLeft && hasRight)
                        {
                            var right = rightOfType[j];
                            double leftPinchElevation = GetPinchElevation(right, rightIntervals, leftIntervals);

                            segments.Add(new IntervalSegment
                            {
                                Type = type,
                                LeftX = leftSection.X,
                                RightX = rightSection.X,
                                LeftTop = leftPinchElevation,
                                LeftBottom = leftPinchElevation,
                                RightTop = right.TopElevation,
                                RightBottom = right.BottomElevation
                            });
                        }
                    }
                }
            }

            var groups = MergeIntervalSegments(segments);

            // Координаты X самой первой и самой последней скважин в профиле
            double firstX = sections[0].X;
            double lastX = sections[^1].X;

            foreach (var group in groups)
            {
                DrawMergedInterval(dc, group, xOffset, verticalScale, firstX, lastX);
            }
        }

        private static double GetPinchElevation(GeoInterval missingInterval, List<GeoInterval> sourceIntervals, List<GeoInterval> targetIntervals)
        {
            return (missingInterval.TopElevation + missingInterval.BottomElevation) / 2.0;


            if (targetIntervals.Count == 0)
                return (missingInterval.TopElevation + missingInterval.BottomElevation) / 2.0;

            double missingCenter = (missingInterval.TopElevation + missingInterval.BottomElevation) / 2.0;

            var layersAbove = sourceIntervals
                .Where(x => x.BottomElevation >= missingInterval.TopElevation && x != missingInterval)
                .OrderBy(x => x.BottomElevation);

            GeoInterval targetAbove = null;
            foreach (var layer in layersAbove)
            {
                targetAbove = targetIntervals
                    .Where(t => string.Equals(t.Type, layer.Type, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(t => Math.Abs(t.BottomElevation - layer.BottomElevation))
                    .FirstOrDefault();

                if (targetAbove != null) break;
            }

            var layersBelow = sourceIntervals
                .Where(x => x.TopElevation <= missingInterval.BottomElevation && x != missingInterval)
                .OrderByDescending(x => x.TopElevation);

            GeoInterval targetBelow = null;
            foreach (var layer in layersBelow)
            {
                targetBelow = targetIntervals
                    .Where(t => string.Equals(t.Type, layer.Type, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(t => Math.Abs(t.TopElevation - layer.TopElevation))
                    .FirstOrDefault();

                if (targetBelow != null) break;
            }

            double maxTargetElevation = targetIntervals.Max(x => x.TopElevation);
            double minTargetElevation = targetIntervals.Min(x => x.BottomElevation);

            if (targetAbove != null) maxTargetElevation = targetAbove.BottomElevation;
            if (targetBelow != null) minTargetElevation = targetBelow.TopElevation;

            if (minTargetElevation > maxTargetElevation)
                return (minTargetElevation + maxTargetElevation) / 2.0;

            if (missingCenter > maxTargetElevation) return maxTargetElevation;
            if (missingCenter < minTargetElevation) return minTargetElevation;

            return missingCenter;
        }

        private static void DrawMergedInterval(DrawContext dc, List<IntervalSegment> segments, double xOffset, int verticalScale, double firstX, double lastX)
        {
            if (segments.Count == 0)
                return;

            var type = segments[0].Type;

            LayerManager.CreateLayer(
                dc.Database,
                dc.Transaction,
                type);

            var points = new Point3dCollection();

            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];

                if (i == 0)
                {
                    if (AreEqual(segment.LeftX, firstX))
                    {
                        points.Add(new Point3d(
                            (segment.LeftX - RIGHT_LEFT_OFFSET) + xOffset,
                            segment.LeftBottom * verticalScale,
                            0));
                    }

                    points.Add(new Point3d(
                        segment.LeftX + xOffset,
                        segment.LeftBottom * verticalScale,
                        0));
                }

                points.Add(new Point3d(
                    segment.RightX + xOffset,
                    segment.RightBottom * verticalScale,
                    0));

                if (i == segments.Count - 1 && AreEqual(segment.RightX, lastX))
                {
                    points.Add(new Point3d(
                        (segment.RightX + RIGHT_LEFT_OFFSET) + xOffset,
                        segment.RightBottom * verticalScale,
                        0));
                }
            }

            // Создаем классическую 2D-полилинию и сглаживаем её
            using (var polyline2d = new Polyline2d(Poly2dType.SimplePoly, points, 0, false, 0, 0, null))
            {
                polyline2d.CurveFit();
                polyline2d.Layer = type;

                dc.ModelSpace.AppendEntity(polyline2d);
                dc.Transaction.AddNewlyCreatedDBObject(polyline2d, true);
            }
        }

        private static List<List<IntervalSegment>> MergeIntervalSegments(List<IntervalSegment> segments)
        {
            var result = new List<List<IntervalSegment>>();

            foreach (var typeGroup in segments.GroupBy(x => x.Type, StringComparer.OrdinalIgnoreCase))
            {
                var ordered = typeGroup.OrderBy(x => x.LeftX).ToList();
                var current = new List<IntervalSegment>();

                foreach (var segment in ordered)
                {
                    if (current.Count == 0)
                    {
                        current.Add(segment);
                        continue;
                    }

                    var previous = current[^1];
                    bool continues = AreEqual(previous.RightX, segment.LeftX);

                    if (continues)
                    {
                        current.Add(segment);
                    }
                    else
                    {
                        result.Add(current);
                        current = new List<IntervalSegment> { segment };
                    }
                }

                if (current.Count > 0)
                    result.Add(current);
            }

            return result;
        }

        private static bool AreEqual(double a, double b)
        {
            return Math.Abs(a - b) < 0.000001;
        }

        private static List<GeoInterval> ExtractIntervals(SectionBorehole section)
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
                        intervals.Add(new GeoInterval
                        {
                            Type = type,
                            TopElevation = section.Top - start,
                            BottomElevation = section.Top - end
                        });
                    }
                }
            }

            return intervals;
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

        #endregion


        /*
        private static void DrawMergedInterval(DrawContext dc, List<IntervalSegment> segments, double xOffset, int verticalScale)
        {
            if (segments.Count == 0)
                return;

            var type = segments[0].Type;

            LayerManager.CreateLayer(
                dc.Database,
                dc.Transaction,
                type);

            var polyline = new Polyline();

            // -------------------------
            // Верхняя граница
            // -------------------------

            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];

                // Первый сегмент добавляет левую точку.
                if (i == 0)
                {
                    polyline.AddVertexAt(
                        polyline.NumberOfVertices,
                        new Point2d(
                            segment.LeftX + xOffset,
                            segment.LeftTop * verticalScale),
                        0, 0, 0);
                }

                // Каждый сегмент добавляет свою правую верхнюю точку.
                polyline.AddVertexAt(
                    polyline.NumberOfVertices,
                    new Point2d(
                        segment.RightX + xOffset,
                        segment.RightTop * verticalScale),
                    0, 0, 0);
            }

            // -------------------------
            // Нижняя граница
            // -------------------------

            // Идём справа налево.
            for (int i = segments.Count - 1; i >= 0; i--)
            {
                var segment = segments[i];

                // Для последнего сегмента
                // сначала добавляем правую нижнюю точку.
                if (i == segments.Count - 1)
                {
                    polyline.AddVertexAt(
                        polyline.NumberOfVertices,
                        new Point2d(
                            segment.RightX + xOffset,
                            segment.RightBottom * verticalScale),
                        0, 0, 0);
                }

                // Затем левую нижнюю точку.
                polyline.AddVertexAt(
                    polyline.NumberOfVertices,
                    new Point2d(
                        segment.LeftX + xOffset,
                        segment.LeftBottom * verticalScale),
                    0, 0, 0);
            }

            polyline.Closed = true;
            polyline.Layer = type;

            dc.ModelSpace.AppendEntity(polyline);
            dc.Transaction.AddNewlyCreatedDBObject(
                polyline,
                true);
        }

        */


        #region Header

        private static void DrawHeader(DrawContext dc, BoreholeLine line, double X, double Y, double xOffset, double vScale)
        {
            X += xOffset;
            Y *= vScale;
            Y += HEADER_Y_OFFSET; // Отступ вверх

            // Номер линии
            DBText lineNumberText = new DBText
            {
                TextString = $"{line.Id}",
                Height = 5,
                HorizontalMode = TextHorizontalMode.TextCenter,
                VerticalMode = TextVerticalMode.TextVerticalMid,
            };

            lineNumberText.AlignmentPoint = new Point3d(X, Y + 14, 0);
            lineNumberText.Layer = LayerManager.GetLayerName(GeoLayers.Header);
            dc.ModelSpace.AppendEntity(lineNumberText);
            dc.Transaction.AddNewlyCreatedDBObject(lineNumberText, true);

            // Азимут

            DBText azimuthText = new DBText
            {
                TextString = $"Азимут {line.Azimuth:F0}°",
                Height = 3,
                HorizontalMode = TextHorizontalMode.TextCenter,
                VerticalMode = TextVerticalMode.TextVerticalMid,
            };

            azimuthText.AlignmentPoint = new Point3d(X, Y + 4, 0);
            azimuthText.Layer = LayerManager.GetLayerName(GeoLayers.Header);
            dc.ModelSpace.AppendEntity(azimuthText);
            dc.Transaction.AddNewlyCreatedDBObject(azimuthText, true);

            // Стрелка
            Line arrowLine = new Line(new Point3d(X - 25, Y, 0), new Point3d(X + 25, Y, 0));
            arrowLine.Layer = LayerManager.GetLayerName(GeoLayers.Header);
            dc.ModelSpace.AppendEntity(arrowLine);
            dc.Transaction.AddNewlyCreatedDBObject(arrowLine, true);

            double arrowWingStartX = X + 25;
            double arrowWingEndX = arrowWingStartX - 4.3;

            Line arrowWingLine = new Line(new Point3d(arrowWingStartX, Y, 0), new Point3d(arrowWingEndX, Y - 2.5, 0));
            arrowWingLine.Layer = LayerManager.GetLayerName(GeoLayers.Header);
            dc.ModelSpace.AppendEntity(arrowWingLine);
            dc.Transaction.AddNewlyCreatedDBObject(arrowWingLine, true);

            Line arrowWingLine2 = new Line(new Point3d(arrowWingStartX, Y, 0), new Point3d(arrowWingEndX, Y + 2.5, 0));
            arrowWingLine2.Layer = LayerManager.GetLayerName(GeoLayers.Header);
            dc.ModelSpace.AppendEntity(arrowWingLine2);
            dc.Transaction.AddNewlyCreatedDBObject(arrowWingLine2, true);

            // Direction Front

            var direction = line.GetDirection();

            DBText dirFrontText = new DBText
            {
                TextString = $"{direction.Front}",
                Height = 3,
                HorizontalMode = TextHorizontalMode.TextCenter,
                VerticalMode = TextVerticalMode.TextVerticalMid,
            };

            dirFrontText.AlignmentPoint = new Point3d(X + 35, Y, 0);
            dirFrontText.Layer = LayerManager.GetLayerName(GeoLayers.Header);
            dc.ModelSpace.AppendEntity(dirFrontText);
            dc.Transaction.AddNewlyCreatedDBObject(dirFrontText, true);

            // Direction Backward

            DBText dirBackwardText = new DBText
            {
                TextString = $"{direction.Backward}",
                Height = 3,
                HorizontalMode = TextHorizontalMode.TextCenter,
                VerticalMode = TextVerticalMode.TextVerticalMid,
            };

            dirBackwardText.AlignmentPoint = new Point3d(X - 35, Y, 0);
            dirBackwardText.Layer = LayerManager.GetLayerName(GeoLayers.Header);
            dc.ModelSpace.AppendEntity(dirBackwardText);
            dc.Transaction.AddNewlyCreatedDBObject(dirBackwardText, true);
        }

        #endregion

        #region Table
        private static void DrawTable(List<SectionBorehole> chbs, DrawContext dc, double tableStartX, double tableEndX, double tableStartY, double xOffset, int verticalScale)
        {
            var table = CreateTable(chbs, tableStartX, tableEndX, tableStartY, xOffset, verticalScale);
            var tableDrawer = new GeoTableDrawer(table, dc);
            tableDrawer.DrawTable();
        }

        private static GeoTable CreateTable(List<SectionBorehole> cbhs, double tableStartX, double tableEndX, double tableStartY, double xOffset, int verticalScale)
        {
            tableStartX += xOffset;
            tableEndX += xOffset;

            var boreholeNumbers = new List<GeoTableRowValue>();
            var boreholeDistances = new List<GeoTableRowValue>();
            var boreholeDepths = new List<GeoTableRowValue>();

            var overburdenDepths = new List<GeoTableRowValue>();          // Пройдено наносами
            var bedrockDepths = new List<GeoTableRowValue>();             // Пройдено в РКП
            var weatheredBedrockDepths = new List<GeoTableRowValue>();    // Пройдено в ПКП

            var peatThicknesses = new List<GeoTableRowValue>();           // Мощность торфов
            var sandLayerThicknesses = new List<GeoTableRowValue>();      // Мощность пласта песков

            var oreGradeValues = new List<GeoTableRowValue>();            // Среднее содержание на пласт
            var oreReserveValues = new List<GeoTableRowValue>();          // Вертикальный запас на пласт

            var rockMassThicknesses = new List<GeoTableRowValue>();       // Мощность горной массы
            var rockMassGradeValues = new List<GeoTableRowValue>();       // Среднее содержание на горную массу

            for (int i = 0; i < cbhs.Count; i++)
            {
                // Номера скважин
                var cbh = cbhs[i];
                var bhX = cbh.X + xOffset;

                boreholeNumbers.Add(new GeoTableRowValue(cbh.Id.ToString(), bhX));

                // Интервалы
                var distance = i != cbhs.Count - 1 ? (cbhs[i + 1].X - cbh.X) : 0;
                var text = distance != 0 ? distance.ToString("F1") : null;
                boreholeDistances.Add(new GeoTableRowValue(text, bhX + distance / 2, [bhX]));

                // Глубины скважин
                boreholeDepths.Add(new GeoTableRowValue(cbh.Deapth.ToString("F1"), bhX));

                // Пройдено наносами
                overburdenDepths.Add(new GeoTableRowValue(cbh.Deapth.ToString("F1"), bhX));

                // Пройдено в РКП
                string rkp = cbh.Source.Atributes.GetValueOrDefault("РКП")?.ToString() ?? "-";
                bedrockDepths.Add(new GeoTableRowValue(rkp, bhX));

                // Пройдено в ПКП
                string pkp = cbh.Source.Atributes.GetValueOrDefault("ПКП")?.ToString() ?? "-";
                weatheredBedrockDepths.Add(new GeoTableRowValue(pkp, bhX));

                // Мощность торфов
                peatThicknesses.Add(new GeoTableRowValue(cbh.OreInterval?.From.ToString("F1") ?? "-", bhX));

                // Мощность пласта песков
                sandLayerThicknesses.Add(new GeoTableRowValue(cbh.OreInterval?.Thinkness.ToString("F1") ?? "-", bhX));

                // Среднее содержание на пласт
                oreGradeValues.Add(new GeoTableRowValue(cbh.OreInterval?.AvgGrade.ToString("F3") ?? "пс", bhX));

                // Вертикальный запас на пласт
                oreReserveValues.Add(new GeoTableRowValue(cbh.OreInterval?.VertReserv.ToString("F3") ?? "пс", bhX));

                // Мощность горной массы
                rockMassThicknesses.Add(new GeoTableRowValue(cbh.OreInterval?.RockMassThickness.ToString("F1") ?? "-", bhX));

                // Среднее содержание на горную массу
                rockMassGradeValues.Add(new GeoTableRowValue(cbh.OreInterval?.AvgRockMassGrade.ToString("F3") ?? "пс", bhX));
            }

            var table = new GeoTable(tableStartX, tableEndX, tableStartY, verticalScale);
            table.Rows.Add(new GeoTableRow("Номер скважины", "№", boreholeNumbers));
            table.Rows.Add(new GeoTableRow("Расстояние между скважинами", "м", boreholeDistances));
            table.Rows.Add(new GeoTableRow("Глубина скважины", "м", boreholeDepths));
            table.Rows.Add(new GeoTableRow("Пройдено наносами", "м", overburdenDepths));
            table.Rows.Add(new GeoTableRow("Пройдено в РКП", "м", bedrockDepths));
            table.Rows.Add(new GeoTableRow("Пройдено в ПКП", "м", weatheredBedrockDepths));
            table.Rows.Add(new GeoTableRow("Мощность торфов", "м", peatThicknesses));
            table.Rows.Add(new GeoTableRow("Мощность пласта песков", "м", sandLayerThicknesses));
            table.Rows.Add(new GeoTableRow("Среднее содержание на пласт", "г/м³", oreGradeValues));
            table.Rows.Add(new GeoTableRow("Вертикальный запас на пласт", "г/м²", oreReserveValues));
            table.Rows.Add(new GeoTableRow("Мощность горной массы", "м", rockMassThicknesses));
            table.Rows.Add(new GeoTableRow("Среднее содержание на горную массу", "г/м³", rockMassGradeValues));

            return table;
        }

        #endregion
    }

    public class LithologyComparer : IEqualityComparer<List<Lithology>>
    {
        public bool Equals(List<Lithology> x, List<Lithology> y)
        {
            return x.OrderBy(a => a)
                .SequenceEqual(y.OrderBy(a => a));
        }

        public int GetHashCode(List<Lithology> obj)
        {
            int hash = 17;

            foreach (var item in obj.OrderBy(x => x))
                hash = hash * 31 + item.GetHashCode();

            return hash;
        }
    }
}
