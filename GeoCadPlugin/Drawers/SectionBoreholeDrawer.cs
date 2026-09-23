using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeoAppCore;
using GeoCadPlugin.Managers;

namespace GeoCadPlugin.Drawers
{
    public class SectionBoreholeDrawer
    {
        private DrawContext _dc;
        public int VerticalScale { get; set; } = 10;

        public SectionBoreholeDrawer(DrawContext drawContext)
        {
            _dc = drawContext;

            LayerManager.CreateLayers(_dc.Database, _dc.Transaction,
            [
                GeoLayers.Boreholes,
                GeoLayers.BoreholeNumbers,
                GeoLayers.AbsoluteElevations,
                GeoLayers.Intervals,
                GeoLayers.Avgs,
                GeoLayers.EmptyAvgs,
                GeoLayers.NotDeterminedAvgs,
            ]);
        }

        public void Draw(SectionBorehole borehole, double xOffset)
        {
            var cbhX = borehole.X + xOffset;
            var bhTopY = borehole.Top * VerticalScale;

            DrawBoreholeText(borehole, cbhX, bhTopY);

            AddLine(cbhX, bhTopY, cbhX, borehole.Bottom * VerticalScale, GeoLayers.Boreholes);

            var samplesStartY = borehole.Top * VerticalScale; // Начало скважины
            foreach (var sample in borehole.Samples)
            {
                var sampleLength = sample.Length * VerticalScale; // Длина интервала в масштабе
                var sampleY = samplesStartY - sampleLength; // Конец интервала
                var lineLength = 1.5;

                bool isLast = sample == borehole.Samples.Last();
                var lineOffset = isLast ? lineLength : 0;

                AddLine(cbhX - lineOffset, sampleY, cbhX + lineLength, sampleY, GeoLayers.Intervals);

                // Текст содержания
                string valueStr = sample.GetValueString();
                double textY = samplesStartY - sampleLength / 2;

                var layer = sample.Grade switch
                {
                    0 => GeoLayers.EmptyAvgs,
                    -1 => GeoLayers.NotDeterminedAvgs,
                    _ => GeoLayers.Avgs,
                };

                var dbtext = AddText(valueStr, cbhX + 1, textY, layer);

                samplesStartY -= sampleLength;
            }
        }

        private void DrawBoreholeText(SectionBorehole borehole, double x, double y)
        {
            const double yOffset = 7;
            const double textInterval = 2;
            const double textHeight = 3;
            const double textHalfHeight = textHeight / 2;
            const double separatorHalfWidth = 6;

            double elevationY = y + yOffset + textHeight / 2;
            double separatorY = elevationY + textHalfHeight + textInterval / 2;
            double numberY = elevationY + textHalfHeight + textInterval + textHeight / 2;

            // Номер скважины
            var nubmberText = AddText(borehole.Id.ToString(), x, numberY, GeoLayers.BoreholeNumbers);
            nubmberText.HorizontalMode = TextHorizontalMode.TextMid;
            nubmberText.Height = textHeight;

            // Абс. отметка устья
            var elevationText = AddText(borehole.Top.ToString("F1"), x, elevationY, GeoLayers.AbsoluteElevations);
            elevationText.HorizontalMode = TextHorizontalMode.TextMid;
            elevationText.Height = textHeight;

            // Сепаратор
            AddLine(x - separatorHalfWidth, separatorY, x + separatorHalfWidth, separatorY);
        }

        private Line AddLine(double startX, double startY, double endX, double endY, GeoLayers layer = GeoLayers.None)
        {
            var line = new Line(new Point3d(startX, startY, 0), new Point3d(endX, endY, 0));

            if (layer != GeoLayers.None)
                line.Layer = LayerManager.GetLayerName(layer);

            _dc.ModelSpace.AppendEntity(line);
            _dc.Transaction.AddNewlyCreatedDBObject(line, true);
            return line;
        }

        private DBText AddText(string text, double X, double Y, GeoLayers layer = GeoLayers.None)
        {
            DBText dbtext = new DBText
            {
                TextString = text,
                Height = 1,
                HorizontalMode = TextHorizontalMode.TextLeft,
                VerticalMode = TextVerticalMode.TextVerticalMid
            };

            dbtext.AlignmentPoint = new Point3d(X, Y, 0);

            if (layer != GeoLayers.None)
                dbtext.Layer = LayerManager.GetLayerName(layer);

            _dc.ModelSpace.AppendEntity(dbtext);
            _dc.Transaction.AddNewlyCreatedDBObject(dbtext, true);

            return dbtext;
        }
    }
}
