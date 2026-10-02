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

                    // Интервалы (возвращают точки выклинивания на поверхности)
                    var surfaceExtras = DrawIntervals(dc, sections, xOffset, project.VerticalScale);

                    // Поверхность
                    DrawSurface(dc, sections, xOffset, project.VerticalScale, surfaceExtras);

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

        #region Surface
        private static void DrawSurface(DrawContext dc, List<SectionBorehole> sections, double xOffset, int verticalScale, IEnumerable<(double X, double Y)>? extraPoints = null)
        {
            if (sections.Count < 2)
                return;

            var pts = sections
                .Select(s => (X: s.X, Y: s.Top))
                .Concat(extraPoints ?? Enumerable.Empty<(double X, double Y)>())
                .OrderBy(p => p.X)
                .ToList();

            var points = new Point3dCollection();

            // Левый отступ
            points.Add(new Point3d((sections[0].X - RIGHT_LEFT_OFFSET) + xOffset, sections[0].Top * verticalScale, 0));

            foreach (var p in pts)
                points.Add(new Point3d(p.X + xOffset, p.Y * verticalScale, 0));

            // Правый отступ
            points.Add(new Point3d((sections[^1].X + RIGHT_LEFT_OFFSET) + xOffset, sections[^1].Top * verticalScale, 0));

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

        private const double PINCH_FRACTION = 0.5;

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

            // Верхняя граница интервала
            public double LeftTop { get; init; }
            public double RightTop { get; init; }

            // Нижняя граница интервала
            public double LeftBottom { get; init; }
            public double RightBottom { get; init; }

            // Доп. вершины на этой линии
            // (места, где в неё упирается выклинивающийся слой)
            public List<(double X, double Y)> Extra { get; } = new();
        }

        private static List<(double X, double Y)> DrawIntervals(DrawContext dc, List<SectionBorehole> sections, double xOffset, int verticalScale)
        {
            var surfaceExtras = new List<(double X, double Y)>();

            if (sections.Count < 2)
                return surfaceExtras;

            var boreholeIntervals = sections.Select(ExtractIntervals).ToList();

            // Самый нижний интервал каждой скважины опускаем на BOTTOM_OFFSET
            foreach (var intervals in boreholeIntervals)
            {
                if (intervals.Count == 0) continue;
                intervals.OrderBy(x => x.BottomElevation).First().BottomElevation -= BOTTOM_OFFSET;
            }

            var segments = new List<IntervalSegment>();
            for (int i = 0; i < sections.Count - 1; i++)
            {
                segments.AddRange(BuildPairSegments(
                    sections[i], sections[i + 1],
                    boreholeIntervals[i], boreholeIntervals[i + 1],
                    surfaceExtras));
            }

            var chains = MergeIntervalSegments(segments);

            double firstX = sections[0].X;
            double lastX = sections[^1].X;

            foreach (var chain in chains)
                DrawMergedInterval(dc, chain, xOffset, verticalScale, firstX, lastX);

            return surfaceExtras;
        }

        private static List<IntervalSegment> BuildPairSegments(SectionBorehole leftSection, SectionBorehole rightSection, List<GeoInterval> left, List<GeoInterval> right, List<(double X, double Y)> surfaceExtras)
        {
            var result = new List<IntervalSegment>();

            double xA = leftSection.X;
            double xB = rightSection.X;
            double dx = xB - xA;

            // Подошва последнего слоя в колонке (для защиты от пересечений)
            double stackA = leftSection.Top;
            double stackB = rightSection.Top;

            // Подошва последнего СКВОЗНОГО слоя (по ней идёт контакт для выклинивающихся)
            double contactA = leftSection.Top;
            double contactB = rightSection.Top;
            IntervalSegment? contactOwner = null; // null = поверхность

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

                    stackA = contactA = bottomA;
                    stackB = contactB = bottomB;
                    contactOwner = seg;
                }
                else if (a != null)
                {
                    // Есть слева, справа нет: выклинивается на расстоянии t от левой скважины
                    double t = PINCH_FRACTION;
                    double px = xA + dx * t;
                    double contact = contactA + (contactB - contactA) * t;
                    double bottomA = Math.Min(a.BottomElevation, stackA);

                    result.Add(new IntervalSegment
                    {
                        Type = a.Type,
                        LeftX = xA,
                        RightX = px,
                        LeftBottom = bottomA,
                        RightBottom = contact
                    });

                    stackA = bottomA;
                    AddContactPoint(contactOwner, surfaceExtras, px, contact);
                }
                else if (b != null)
                {
                    // Есть справа, слева нет: слой начинается на расстоянии (1 - t) от левой скважины
                    double t = 1.0 - PINCH_FRACTION;
                    double px = xA + dx * t;
                    double contact = contactA + (contactB - contactA) * t;
                    double bottomB = Math.Min(b.BottomElevation, stackB);

                    result.Add(new IntervalSegment
                    {
                        Type = b.Type,
                        LeftX = px,
                        RightX = xB,
                        LeftBottom = contact,
                        RightBottom = bottomB
                    });

                    stackB = bottomB;
                    AddContactPoint(contactOwner, surfaceExtras, px, contact);
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
        
        private static void DrawMergedInterval(DrawContext dc, List<IntervalSegment> segments, double xOffset, int verticalScale, double firstX, double lastX)
        {
            if (segments.Count == 0)
                return;

            var type = segments[0].Type;

            LayerManager.CreateLayer(dc.Database, dc.Transaction, type);

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
                            segment.LeftBottom * verticalScale, 0));
                    }

                    points.Add(new Point3d(
                        segment.LeftX + xOffset,
                        segment.LeftBottom * verticalScale, 0));
                }

                // Вершины, где в эту линию упирается выклинивающийся соседний слой
                foreach (var e in segment.Extra.OrderBy(p => p.X))
                {
                    if (e.X > segment.LeftX && e.X < segment.RightX)
                        points.Add(new Point3d(e.X + xOffset, e.Y * verticalScale, 0));
                }

                points.Add(new Point3d(
                    segment.RightX + xOffset,
                    segment.RightBottom * verticalScale, 0));

                if (i == segments.Count - 1 && AreEqual(segment.RightX, lastX))
                {
                    points.Add(new Point3d(
                        (segment.RightX + RIGHT_LEFT_OFFSET) + xOffset,
                        segment.RightBottom * verticalScale, 0));
                }
            }

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

        #endregion

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
                sandLayerThicknesses.Add(new GeoTableRowValue(cbh.OreInterval?.Thickness.ToString("F1") ?? "-", bhX));

                // Среднее содержание на пласт
                oreGradeValues.Add(new GeoTableRowValue(DoubleToString(cbh.OreInterval?.PureAvgGrade), bhX));

                // Вертикальный запас на пласт
                oreReserveValues.Add(new GeoTableRowValue(DoubleToString(cbh.OreInterval?.PureVertReserve), bhX));

                // Мощность горной массы
                rockMassThicknesses.Add(new GeoTableRowValue(cbh.OreInterval?.RockMassThickness.ToString("F1") ?? "-", bhX));

                // Среднее содержание на горную массу
                rockMassGradeValues.Add(new GeoTableRowValue(DoubleToString(cbh.OreInterval?.AvgRockMassGrade), bhX));
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

        private static string DoubleToString(double? value)
        {
            if (value == null || value == 0)
                return "пс";

            if (value < 0)
                return "зн";

            return value?.ToString("F3");
        }
    }
}
