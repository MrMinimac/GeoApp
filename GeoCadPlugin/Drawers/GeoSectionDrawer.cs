using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeoAppCore;
using GeoAppCore.Models;
using GeoAppCore.Services;
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
                    // DrawSurface(dc, sections, xOffset, project.VerticalScale);

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

                    // Добавляем смещение
                    xOffset += blockWidth + SECTIONS_SPACING;
                }

                tr.Commit();
            }

            editor.WriteMessage(
                $"\nИмпорт документа.\n" +
                $"Буровых линий: {project.BoreholeLines.Count}\n" +
                $"Общее кол-во скважин: {project.BoreholeLines.Sum(x => x.Boreholes.Count)}\n" +
                $"Общее кол-во проб: {project.BoreholeLines.Sum(x => x.Boreholes.Sum(x => x.SamplesCount))}\n");
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

            Polyline polyline = new Polyline();

            for (int i = 0; i < sections.Count; i++)
            {
                double x = sections[i].X + xOffset;
                double y = sections[i].Top * verticalScale;

                polyline.AddVertexAt(
                    i,
                    new Point2d(x, y),
                    0,      // bulge (0 = прямая линия между вершинами)
                    0,
                    0);
            }

            polyline.Layer = LayerManager.GetLayerName(GeoLayers.Surface);
            dc.ModelSpace.AppendEntity(polyline);
            dc.Transaction.AddNewlyCreatedDBObject(polyline, true);
        }
        #endregion

        #region DrawIntervals

        /// <summary>
        /// Рисует геологические горизонты ("Наносы Интервал", "РКП Интервал",
        /// "ПКП Интервал" и т.п.) - по одному замкнутому контуру на каждый
        /// встречающийся тип, так же, как DrawLithologies строит контур на
        /// каждый тип литологии. Кровля/подошва берутся от той же точки
        /// отсчёта (section.Top), что и поверхность и литология, поэтому
        /// контуры автоматически согласованы по вертикали и не "гуляют"
        /// относительно линии поверхности.
        /// </summary>
        private static void DrawIntervals(DrawContext dc, List<SectionBorehole> sections, double xOffset, int verticalScale)
        {
            if (sections.Count < 2)
                return;

            foreach (var (type, points) in BuildIntervalLayers(sections))
                DrawIntervalLayer(dc, type, points, xOffset, verticalScale);
        }

        private static Dictionary<string, List<LayerPoint>> BuildIntervalLayers(List<SectionBorehole> sections)
        {
            const string suffix = " Интервал";

            // тип -> точки кровли/подошвы по скважинам, где он встретился
            var layers = new Dictionary<string, List<LayerPoint>>(StringComparer.OrdinalIgnoreCase);

            foreach (var section in sections)
            {
                var types = section.Source.Atributes.Keys
                    .Where(k => k.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    .Select(k => k[..^suffix.Length]);

                foreach (var type in types)
                {
                    var raw = section.Source.Atributes
                        .GetValueOrDefault($"{type}{suffix}")?
                        .ToString();

                    if (!TryParseInterval(raw, out double start, out double end))
                        continue;

                    if (!layers.TryGetValue(type, out var points))
                    {
                        points = new List<LayerPoint>();
                        layers[type] = points;
                    }

                    points.Add(new LayerPoint
                    {
                        X = section.X,
                        Top = section.Top - start,
                        Bottom = section.Top - end
                    });
                }
            }

            return layers;
        }

        private static void DrawIntervalLayer(DrawContext dc, string type, List<LayerPoint> points, double xOffset, int verticalScale)
        {
            // одна точка - контур не построить (нечего соединять по горизонтали)
            if (points.Count < 2)
                return;

            LayerManager.CreateLayer(dc.Database, dc.Transaction, type);

            var polyline = new Polyline();

            foreach (var point in points)
            {
                polyline.AddVertexAt(
                    polyline.NumberOfVertices,
                    new Point2d(point.X + xOffset, point.Top * verticalScale),
                    0, 0, 0);
            }

            for (int i = points.Count - 1; i >= 0; i--)
            {
                var point = points[i];

                polyline.AddVertexAt(
                    polyline.NumberOfVertices,
                    new Point2d(point.X + xOffset, point.Bottom * verticalScale),
                    0, 0, 0);
            }

            polyline.Closed = true;
            polyline.Layer = type;

            dc.ModelSpace.AppendEntity(polyline);
            dc.Transaction.AddNewlyCreatedDBObject(polyline, true);
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
            value = value.Replace(',', '.');

            var number = new string(
                value
                    .Trim()
                    .TakeWhile(c => char.IsDigit(c) || c == '.')
                    .ToArray());

            return double.TryParse(
                number,
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
                peatThicknesses.Add(new GeoTableRowValue(cbh.OreInterval?.From.ToString() ?? "-", bhX));

                // Мощность пласта песков
                sandLayerThicknesses.Add(new GeoTableRowValue(cbh.OreInterval?.Length.ToString("F1") ?? "-", bhX));

                // Среднее содержание на пласт
                oreGradeValues.Add(new GeoTableRowValue(cbh.OreInterval?.AvgGrade.ToString("F1") ?? "0,000", bhX));

                // Вертикальный запас на пласт
                oreReserveValues.Add(new GeoTableRowValue("0,000", bhX));

                // Мощность горной массы
                rockMassThicknesses.Add(new GeoTableRowValue("-", bhX));

                // Среднее содержание на горную массу
                rockMassGradeValues.Add(new GeoTableRowValue("0,000", bhX));
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

    public class IntervalInfo
    {
        public string Type { get; init; } = "";
        public double Start { get; init; }
        public double End { get; init; }
    }

    public class SectionInterval
    {
        public string Type { get; init; }

        public double Start { get; init; }
        public double End { get; init; }

        public double Top => Start;
        public double Bottom => End;
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
