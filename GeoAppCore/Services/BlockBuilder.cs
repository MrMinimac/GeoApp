using GeoAppCore.Models;
using System.Diagnostics;

namespace GeoAppCore.Services
{
    public class Block
    {
        public string Id { get; set; } = string.Empty;
        public List<SectionBorehole> Boreholes { get; set; } = new();
        public BoundaryData? Boundary { get; set; }
        public double Area => Boundary?.Area ?? 0;

        public double ValidBoreholesCount => Boreholes.Where(x => x.OreInterval != null).Count();
        public double TotalThickness => Boreholes.Sum(x => x.OreInterval?.Thickness ?? 0);
        public double TotalPureVertReserve => Boreholes.Sum(x => x.OreInterval?.PureVertReserve ?? 0);
        public double AvgThickness => TotalThickness / ValidBoreholesCount;
        public double PureAvgGrade => TotalPureVertReserve / TotalThickness;
        public double Volume => Area * AvgThickness;

        public double ReserveG => PureAvgGrade * Volume;

        public double ReserveKg => ReserveG / 1000;
    }

    public class BlockBuilder
    {
        private const double BoundaryTolerance = 0.75;

        public static List<Block> Build(IEnumerable<BoreholeLine> lines, List<BoundaryData> boundaries)
        {
            var blocks = new List<Block>();

            foreach (var boundary in boundaries)
            {
                var boreholes = lines
                    .SelectMany(x => x.Boreholes)
                    .Where(bh => IsInsideBoundary(bh.X, bh.Y, boundary))
                    .ToList();

                var sections = BoreholeLine.BuildSections(boreholes)
                    .OrderBy(w =>
                        w.Source.BoreholeLineId.Any(char.IsDigit) ? 0 : 1)
                    .ThenBy(
                        w => w.Source.BoreholeLineId,
                        new NaturalNameComparer())
                    .ToList();

                blocks.Add(new Block
                {
                    Boreholes = sections,
                    Boundary = boundary
                });
            }

            // Сначала определяем порядок блоков.
            blocks = blocks
                .OrderBy(GetFirstLineId, new NaturalNameComparer())
                .ThenBy(GetLineCount)
                .ToList();

            // После сортировки назначаем номера.
            for (int i = 0; i < blocks.Count; i++)
                blocks[i].Id = (i + 1).ToString();

            return blocks;
        }

        private static string GetFirstLineId(Block block)
        {
            return block.Boreholes
                .Select(x => x.Source.BoreholeLineId)
                .Distinct()
                .OrderBy(x => x, new NaturalNameComparer())
                .FirstOrDefault() ?? string.Empty;
        }

        private static int GetLineCount(Block block)
        {
            return block.Boreholes
                .Select(x => x.Source.BoreholeLineId)
                .Distinct()
                .Count();
        }

        #region Boundary

        private static bool IsInsideBoundary(double x, double y, BoundaryData boundary)
        {
            if (!boundary.Closed)
            {
                Debug.WriteLine("Контур не закрыт.");
                return false;
            }

            if (boundary.Points.Count < 3)
            {
                Debug.WriteLine("У контура не достаточно точек.");
                return false;
            }

            if (IsPointInsideOrNearPolygon(x, y, boundary.Points, BoundaryTolerance))
                return true;

            return false;
        }

        private static bool IsPointInsideOrNearPolygon(double x, double y, IReadOnlyList<Point2D> points, double tolerance)
        {
            if (IsPointInsidePolygon(x, y, points))
                return true;

            double toleranceSquared =
                tolerance * tolerance;

            for (int i = 0; i < points.Count; i++)
            {
                var a = points[i];
                var b = points[(i + 1) % points.Count];

                double distanceSquared = DistanceSquaredToSegment(x, y, a.X, a.Y, b.X, b.Y);

                if (distanceSquared <= toleranceSquared)
                    return true;
            }

            return false;
        }

        private static bool IsPointInsidePolygon(
            double x,
            double y,
            IReadOnlyList<Point2D> points)
        {
            bool inside = false;

            for (int i = 0, j = points.Count - 1;
                 i < points.Count;
                 j = i++)
            {
                double xi = points[i].X;
                double yi = points[i].Y;

                double xj = points[j].X;
                double yj = points[j].Y;

                bool intersect =
                    ((yi > y) != (yj > y)) &&
                    x <
                    (xj - xi) *
                    (y - yi) /
                    (yj - yi) +
                    xi;

                if (intersect)
                    inside = !inside;
            }

            return inside;
        }

        private static double DistanceSquaredToSegment(
            double px,
            double py,
            double x1,
            double y1,
            double x2,
            double y2)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;

            if (dx == 0 && dy == 0)
            {
                double ddx = px - x1;
                double ddy = py - y1;

                return ddx * ddx + ddy * ddy;
            }

            double t =
                ((px - x1) * dx +
                 (py - y1) * dy) /
                (dx * dx + dy * dy);

            t = Math.Clamp(t, 0.0, 1.0);

            double closestX =
                x1 + t * dx;

            double closestY =
                y1 + t * dy;

            double diffX =
                px - closestX;

            double diffY =
                py - closestY;

            return diffX * diffX +
                   diffY * diffY;
        }

        #endregion
    }
}
