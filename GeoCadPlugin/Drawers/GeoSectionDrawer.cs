using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeoAppCore;
using GeoCadPlugin.Managers;

namespace GeoCadPlugin.Drawers
{
    public class GeoSectionDrawer
    {
        public static void Draw(GeoDoc project)
        {
            double xOffset = 0; // Смещенее разреза
            double rulerStartX = -20; // Отступ влево
            double tableLeftOffset = -110; // Отступ влево
            double tableRightOffset = 15; // Отступ вправо
            double blockPadding = 15; // Отступ между разрезами

            var editor = Application.DocumentManager.MdiActiveDocument.Editor;
            Database db = Application.DocumentManager.MdiActiveDocument.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var dc = new DrawContext(db, tr, ms);

                var rulerDrawer = new RulerDrawer(dc);
                rulerDrawer.VerticalScale = project.VerticalScale;

                LayerManager.CreateLayers(dc.Database, dc.Transaction, [ GeoLayers.Header,GeoLayers.Surface ]);

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

                    // Линейка
                    rulerDrawer.DrawVertRuler(rulerStartX + xOffset, line.MinZ, line.MaxZ);

                    // Таблица
                    var startX = sections[0].X;
                    var endX = sections[^1].X;
                    var tableStartX = startX + tableLeftOffset;
                    var tableEndX = endX + tableRightOffset;
                    var tableStartY = line.MinZ;
                    DrawTable(sections, dc, tableStartX, tableEndX, tableStartY, xOffset, project.VerticalScale);

                    // считаем реальную ширину блока
                    double blockMinX = Math.Min(startX + tableLeftOffset, rulerStartX);
                    double blockMaxX = Math.Max(endX, tableEndX);
                    double blockWidth = blockMaxX - blockMinX;

                    // Добавляем смещение
                    xOffset += blockWidth + blockPadding;
                }

                tr.Commit();
            }

            editor.WriteMessage(
                $"\nИмпорт документа.\n" +
                $"Буровых линий: {project.BoreholeLines.Count}\n" +
                $"Общее кол-во скважин: {project.BoreholeLines.Sum(x => x.Boreholes.Count)}\n" +
                $"Общее кол-во проб: {project.BoreholeLines.Sum(x => x.Boreholes.Sum(x => x.SamplesCount))}\n");
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

        private static void DrawLithologies(DrawContext dc, List<SectionBorehole> sections, double xOffset, int verticalScale)
        {
            if (sections.Count < 2)
                return;

            Polyline polyline = new Polyline();

            for (int i = 0; i < sections.Count; i++)
            {
                var section = sections[i];

                for (int j = 0; j < section.LithologiesIntervals.Count(); j++)
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
            }

            polyline.Layer = LayerManager.GetLayerName(GeoLayers.Surface);
            dc.ModelSpace.AppendEntity(polyline);
            dc.Transaction.AddNewlyCreatedDBObject(polyline, true);
        }
        #endregion

        #region Header
        private static void DrawHeader(DrawContext dc, BoreholeLine line, double X, double Y, double xOffset, double vScale)
        {
            X += xOffset;
            Y *= vScale;
            Y += 10; // Отступ вверх

            // Номер линии
            DBText lineNumberText = new DBText
            {
                TextString = $"БЛ-{line.Number}",
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
                var bh = cbhs[i];
                var bhX = bh.X + xOffset;

                boreholeNumbers.Add(new GeoTableRowValue(bh.Id.ToString(), bhX));

                // Интервалы
                var distance = i != cbhs.Count - 1 ? (cbhs[i + 1].X - bh.X) : 0;
                var text = distance != 0 ? distance.ToString("F1") : null;
                boreholeDistances.Add(new GeoTableRowValue(text, bh.X + distance / 2, [bhX]));

                // Глубины скважин
                boreholeDepths.Add(new GeoTableRowValue(bh.Deapth.ToString("F1"), bhX));

                // Пройдено наносами
                overburdenDepths.Add(new GeoTableRowValue(bh.Deapth.ToString("F1"), bhX));

                // Пройдено в РКП
                bedrockDepths.Add(new GeoTableRowValue("-", bhX));

                // Пройдено в ПКП
                weatheredBedrockDepths.Add(new GeoTableRowValue("-", bhX));

                // Мощность торфов
                peatThicknesses.Add(new GeoTableRowValue("-", bhX));

                // Мощность пласта песков
                sandLayerThicknesses.Add(new GeoTableRowValue("-", bhX));

                // Среднее содержание на пласт
                oreGradeValues.Add(new GeoTableRowValue("0,000", bhX));

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
}
