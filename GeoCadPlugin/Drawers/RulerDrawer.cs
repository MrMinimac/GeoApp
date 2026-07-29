using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeoCadPlugin.Managers;

namespace GeoCadPlugin.Drawers
{
    public class RulerDrawer
    {
        private DrawContext _dc;

        public double ValuesStep { get; set; } = 5;
        public double TickStep { get; set; } = 1;
        public int VerticalScale { get; set; } = 10;
        public double Width { get; set; } = 1;

        public RulerDrawer(DrawContext drawContext)
        {
            _dc = drawContext;
            LayerManager.CreateLayer(_dc.Database, _dc.Transaction, GeoLayers.Rulers);
        }

        public void DrawVertRuler(double startX, double startY, double endY)
        {
            endY = Math.Ceiling(endY / ValuesStep) * ValuesStep;
            startY = Math.Floor(startY / ValuesStep) * ValuesStep;

            AddText("м", startX + Width / 2, endY * VerticalScale + 2, TextHorizontalMode.TextMid);

            DrawLine(startX, startY * VerticalScale, startX, endY * VerticalScale);
            DrawLine(startX + Width, startY * VerticalScale, startX + Width, endY * VerticalScale);

            for (double y = startY; y < endY; y += TickStep * 2)
                DrawHatch(startX, y);

            for (double y = startY; y <= endY; y += TickStep)
                DrawLine(startX + Width, y * VerticalScale, startX, y * VerticalScale);

            for (double y = startY; y <= endY; y += ValuesStep)
            {
                AddText(y.ToString("0"), startX - 7, y * VerticalScale);
            }
        }

        private DBText AddText(string text, double x, double y, TextHorizontalMode horizontalMode = TextHorizontalMode.TextLeft)
        {
            DBText dbtext = new DBText
            {
                TextString = text,
                Height = 2,
                HorizontalMode = horizontalMode,
                VerticalMode = TextVerticalMode.TextVerticalMid,
            };

            dbtext.AlignmentPoint = new Point3d(x, y, 0);
            dbtext.Layer = LayerManager.GetLayerName(GeoLayers.Rulers);
            _dc.ModelSpace.AppendEntity(dbtext);
            _dc.Transaction.AddNewlyCreatedDBObject(dbtext, true);

            return dbtext;
        }

        private void DrawHatch(double startX, double startY)
        {
            Polyline boundary = new Polyline();

            double y1 = startY * VerticalScale;
            double y2 = (startY + TickStep) * VerticalScale;

            boundary.AddVertexAt(0, new Point2d(startX, y1), 0, 0, 0);
            boundary.AddVertexAt(1, new Point2d(startX + Width, y1), 0, 0, 0);
            boundary.AddVertexAt(2, new Point2d(startX + Width, y2), 0, 0, 0);
            boundary.AddVertexAt(3, new Point2d(startX, y2), 0, 0, 0);
            boundary.Closed = true;
            _dc.ModelSpace.AppendEntity(boundary);
            _dc.Transaction.AddNewlyCreatedDBObject(boundary, true);

            Hatch hatch = new Hatch();
            hatch.Layer = LayerManager.GetLayerName(GeoLayers.Rulers);
            hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
            hatch.Associative = false;
            _dc.ModelSpace.AppendEntity(hatch);
            _dc.Transaction.AddNewlyCreatedDBObject(hatch, true);
            hatch.AppendLoop(HatchLoopTypes.Outermost, new ObjectIdCollection { boundary.ObjectId });
            hatch.EvaluateHatch(true);

            boundary.Erase();
        }

        private Line DrawLine(double startX, double startY, double endX, double endY)
        {
            Line axis = new Line(new Point3d(startX, startY, 0), new Point3d(endX, endY, 0));
            axis.Layer = LayerManager.GetLayerName(GeoLayers.Rulers);
            _dc.ModelSpace.AppendEntity(axis);
            _dc.Transaction.AddNewlyCreatedDBObject(axis, true);

            return axis;
        }
    }
}
