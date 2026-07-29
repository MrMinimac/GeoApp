using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeoCadPlugin.Managers;

namespace GeoCadPlugin.Drawers
{
    public class GeoTableDrawer
    {
        private DrawContext _dc;
        private GeoTable _table;

        public GeoTableDrawer(GeoTable table, DrawContext drawContext)
        {
            _table = table;
            _dc = drawContext;
            LayerManager.CreateLayer(_dc.Database, _dc.Transaction, GeoLayers.Tables);
        }

        public void DrawTable()
        {
            // Left Line
            AddLine(_table.StartX, _table.StartY, _table.StartX, _table.StartY - _table.Height);

            // Right Line
            AddLine(_table.EndX, _table.StartY, _table.EndX, _table.StartY - _table.Height);

            var rowLineY = _table.StartY;

            for (int i = 0; i < _table.Rows.Count + 1; i++)
            {
                AddLine(_table.StartX, rowLineY, _table.EndX, rowLineY);
                rowLineY -= _table.RowHeight;
            }

            // Unit Column Left Line
            AddLine(
                _table.StartX + _table.TitlesColumnWidth, 
                _table.StartY, 
                _table.StartX + _table.TitlesColumnWidth, 
                _table.StartY - _table.Height);

            // Unit Column Right Line
            AddLine(
                _table.StartX + _table.TitlesColumnWidth + _table.UnitsColumnWidth, 
                _table.StartY, 
                _table.StartX + _table.TitlesColumnWidth + _table.UnitsColumnWidth, 
                _table.StartY - _table.Height);

            ///
            /// Table Text
            ///

            var textY = _table.StartY - _table.RowHeight;
            foreach (var row in _table.Rows)
            {
                // Row Title
                AddText(row.Title, _table.StartX + _table.TextLeftMargin, textY + _table.RowHeight / 2, TextHorizontalMode.TextLeft);

                // Unit of measurement
                AddText(row.UnitText, _table.StartX + _table.TitlesColumnWidth + _table.UnitsColumnWidth / 2, textY + _table.RowHeight / 2);

                foreach (var value in row.Values)
                {
                    if (value.Text != null)
                        AddText(value.Text, value.X, textY + _table.RowHeight / 2);

                    if (value.BordersX != null)
                        foreach (var borderX in value.BordersX)
                            AddLine(borderX, textY, borderX, textY + _table.RowHeight);
                }

                textY -= _table.RowHeight;
            }
        }

        private DBText AddText(string text, double X, double Y, TextHorizontalMode horizontalMode = TextHorizontalMode.TextCenter)
        {
            DBText dbtext = new DBText
            {
                TextString = text,
                Height = _table.TextHeight,
                HorizontalMode = horizontalMode,
                VerticalMode = TextVerticalMode.TextVerticalMid,
            };

            dbtext.AlignmentPoint = new Point3d(X, Y, 0);
            dbtext.Layer = LayerManager.GetLayerName(GeoLayers.Tables);
            _dc.ModelSpace.AppendEntity(dbtext);
            _dc.Transaction.AddNewlyCreatedDBObject(dbtext, true);

            return dbtext;
        }

        private Line AddLine(double startX, double startY, double endX, double endY)
        {
            var line = new Line(new Point3d(startX, startY, 0), new Point3d(endX, endY, 0));
            line.Layer = LayerManager.GetLayerName(GeoLayers.Tables);
            _dc.ModelSpace.AppendEntity(line);
            _dc.Transaction.AddNewlyCreatedDBObject(line, true);
            return line;
        }
    }
}
