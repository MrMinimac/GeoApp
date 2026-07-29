using Autodesk.AutoCAD.ApplicationServices;
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
            double xOffset = 0;

            double rulerStartX = -20;

            double tableLeftOffset = -110;
            double tableRightOffset = 15;
            double blockPadding = 15;

            

            var editor = Application.DocumentManager.MdiActiveDocument.Editor;
            Database db = Application.DocumentManager.MdiActiveDocument.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var dc = new DrawContext(db, tr, ms);

                foreach (var line in project.BoreholeLines)
                {
                    LayerManager.CreateLayers(dc.Database, dc.Transaction, 
                        [
                            GeoLayers.Boreholes, 
                            GeoLayers.Rulers, 
                            GeoLayers.Tables
                        ]);

                    var sections = line.BuildSections();
                    var startX = sections.FirstOrDefault()?.X ?? 0;
                    var endX = sections.LastOrDefault()?.X ?? 0;

                    DrawVertRuler(dc, rulerStartX + xOffset, line.MinZ, line.MaxZ, project.VerticalScale);
                    DrawLine(dc, sections, project.VerticalScale, xOffset);

                    var tableStartX = startX + tableLeftOffset + xOffset;
                    var tableEndX = endX + tableRightOffset + xOffset;
                    var tableStartY = line.MinZ;

                    DrawTable(sections, dc, tableStartX, tableEndX, tableStartY, project.VerticalScale);

                    // считаем реальную ширину блока
                    double blockMinX = Math.Min(startX + tableLeftOffset, rulerStartX);
                    double blockMaxX = Math.Max(endX, tableEndX);
                    double blockWidth = blockMaxX - blockMinX;
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

        private static void DrawTable(List<SectionBorehole> chbs, DrawContext dc, double tableStartX, double tableEndX, double tableStartY, int verticalScale)
        {
            var table = CreateTable(chbs, tableStartX, tableEndX, tableStartY, verticalScale);
            var tableDrawer = new GeoTableDrawer(table, dc);

            tableDrawer.DrawTable();
        }

        private static GeoTable CreateTable(List<SectionBorehole> cbhs, double tableStartX, double tableEndX, double tableStartY, int verticalScale)
        {
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
                boreholeNumbers.Add(new GeoTableRowValue(bh.Source.Id.ToString(), bh.X));

                // Интервалы
                var distance = i != cbhs.Count - 1 ? (cbhs[i + 1].X - bh.X) : 0;
                var text = distance != 0 ? distance.ToString("F1") : null;
                boreholeDistances.Add(new GeoTableRowValue(text, bh.X + distance / 2, [bh.X]));

                // Глубины скважин
                boreholeDepths.Add(new GeoTableRowValue(bh.Source.Deapth.ToString("F1"), bh.X));

                // Пройдено наносами
                overburdenDepths.Add(new GeoTableRowValue(bh.Source.Deapth.ToString("F1"), bh.X));

                // Пройдено в РКП
                bedrockDepths.Add(new GeoTableRowValue("-", bh.X));

                // Пройдено в ПКП
                weatheredBedrockDepths.Add(new GeoTableRowValue("-", bh.X));

                // Мощность торфов
                peatThicknesses.Add(new GeoTableRowValue("-", bh.X));

                // Мощность пласта песков
                sandLayerThicknesses.Add(new GeoTableRowValue("-", bh.X));

                // Среднее содержание на пласт
                oreGradeValues.Add(new GeoTableRowValue("0,000", bh.X));

                // Вертикальный запас на пласт
                oreReserveValues.Add(new GeoTableRowValue("0,000", bh.X));

                // Мощность горной массы
                rockMassThicknesses.Add(new GeoTableRowValue("-", bh.X));

                // Среднее содержание на горную массу
                rockMassGradeValues.Add(new GeoTableRowValue("0,000", bh.X));
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

        private static void DrawVertRuler(
            DrawContext dc,
            double startX,
            double minZ,
            double maxZ,
            int scale,
            double step = 5,
            double tickStep = 1,
            double width = 1)
        {
            maxZ = Math.Ceiling(maxZ / step) * step;
            maxZ = Math.Ceiling(maxZ / step) * step;
            minZ = Math.Floor(minZ / step) * step;

            LayerManager.CreateLayer(dc.Database, dc.Transaction, GeoLayers.Rulers);

            Line axis = new Line(new Point3d(startX, minZ * scale, 0), new Point3d(startX, maxZ * scale, 0));
            axis.Layer = LayerManager.GetLayerName(GeoLayers.Rulers);
            dc.ModelSpace.AppendEntity(axis);
            dc.Transaction.AddNewlyCreatedDBObject(axis, true);

            Line axis2 = new Line(new Point3d(startX + width, minZ * scale, 0), new Point3d(startX + width, maxZ * scale, 0));
            axis2.Layer = LayerManager.GetLayerName(GeoLayers.Rulers);
            dc.ModelSpace.AppendEntity(axis2);
            dc.Transaction.AddNewlyCreatedDBObject(axis2, true);

            for (double z = minZ; z <= maxZ; z += tickStep)
            {
                Line tick = new Line(
                    new Point3d(startX + width, z * scale, 0),
                    new Point3d(startX, z * scale, 0));

                tick.Layer = LayerManager.GetLayerName(GeoLayers.Rulers);

                dc.ModelSpace.AppendEntity(tick);
                dc.Transaction.AddNewlyCreatedDBObject(tick, true);
            }

            for (double z = minZ; z <= maxZ; z += step)
            {
                DBText text = new DBText
                {
                    TextString = z.ToString("0"),
                    Height = 2,
                    Position = new Point3d(startX - 7, z * scale, 0)
                };

                text.Layer = LayerManager.GetLayerName(GeoLayers.Rulers);

                dc.ModelSpace.AppendEntity(text);
                dc.Transaction.AddNewlyCreatedDBObject(text, true);
            }
        }

        private static void DrawLine(
            DrawContext dc,
            List<SectionBorehole> line,
            int vertScale,
            double xOffset)
        {
            // рисуем скважины
            foreach (var cbh in line)
            {
                DrawBorehole(dc, cbh, vertScale, xOffset);
            }
        }

        private static void DrawBorehole(
            DrawContext dc,
            SectionBorehole bh,
            int Scale,
            double xOffset)
        {
            Line bhLine = new Line(
                new Point3d(
                    bh.X + xOffset,
                    bh.Top * Scale,
                    0),

                new Point3d(
                    bh.X + xOffset,
                    bh.Bottom * Scale,
                    0));

            bhLine.Layer = LayerManager.GetLayerName(GeoLayers.Boreholes);

            dc.ModelSpace.AppendEntity(bhLine);
            dc.Transaction.AddNewlyCreatedDBObject(bhLine, true);
        }
    }
}
