using GeoAppCore.Services;

namespace GeoAppCore.Models
{
    public class BoundaryData
    {
        public bool Closed { get; set; }
        public List<Point2D> Points { get; set; } = new();

        public double Area => GetBoundaryArea(this);

        public static double GetBoundaryArea(BoundaryData boundary)
        {
            var points = boundary.Points;

            if (points.Count < 3)
                return 0;

            double area = 0;

            for (int i = 0; i < points.Count; i++)
            {
                var p1 = points[i];
                var p2 = points[(i + 1) % points.Count];

                area += p1.X * p2.Y - p2.X * p1.Y;
            }

            return Math.Abs(area) / 2.0;
        }
    }
}
