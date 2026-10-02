using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeoAppCore;
using GeoCadPlugin.Managers;

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
            double xOffset = 0;

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

                    GeoLayersDrawer.DrawSurfaceAndIntervals(dc, sections, xOffset, project.VerticalScale, project.DrawHatch);

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

            var intervals = GeoLayersDrawer.ExtractIntervals(section);
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
                overburdenDepths.Add(new GeoTableRowValue(cbh.Source.Atributes.GetValueOrDefault("Наносы")?.ToString() ?? "-", bhX));

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
