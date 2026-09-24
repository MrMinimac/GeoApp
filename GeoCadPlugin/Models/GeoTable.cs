using GeoCadPlugin.Drawers;

namespace GeoCadPlugin
{
    public class GeoTable
    {
        private double _startX;
        private double _endX;
        private double _startY;

        public double StartX => _startX;
        public double EndX => _endX;
        public double StartY => _startY * VerticalScale + MarginTop;

        public double VerticalScale { get; set; }
        public double RowHeight { get; set; } = 5;
        public double Height => RowHeight * Rows.Count;
        public double MarginTop { get; set; } = -3;

        public double TextHeight { get; set; } = 2.5;
        public double TextLeftMargin { get; set; } = 3;

        public double TitlesColumnWidth = 85;
        public double UnitsColumnWidth = 10;

        public List<GeoTableRow> Rows { get; } = new();

        public GeoTable(double startX, double endX, double startY, int vertScale)
        {
            _startX = startX;
            _endX = endX;
            _startY = startY;
            VerticalScale = vertScale;
        }
    }
}
