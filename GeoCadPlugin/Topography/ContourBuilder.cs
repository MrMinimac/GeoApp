using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace GeoCadPlugin.Topography
{
    public readonly record struct ContourPoint(
        double Longitude,
        double Latitude);

    public sealed class ContourLine
    {
        public double Elevation { get; }

        public List<ContourPoint> Points { get; }

        public bool IsClosed { get; set; }

        public ContourLine(
            double elevation,
            List<ContourPoint> points,
            bool isClosed)
        {
            Elevation = elevation;
            Points = points;
            IsClosed = isClosed;
        }
    }

    internal static class ContourBuilder
    {
        private const double NoData = -32768.0;

        private const double PointTolerance = 1e-10;

        private readonly record struct Segment(
            ContourPoint A,
            ContourPoint B);

        private readonly record struct PointKey(
            long X,
            long Y);

        public static List<ContourLine> Build(
            DemGrid dem,
            double interval)
        {
            if (dem == null)
                throw new ArgumentNullException(nameof(dem));

            if (interval <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(interval));

            double minElevation =
                double.MaxValue;

            double maxElevation =
                double.MinValue;

            for (int row = 0;
                 row < dem.Height;
                 row++)
            {
                for (int column = 0;
                     column < dem.Width;
                     column++)
                {
                    double z =
                        dem.Elevation[row, column];

                    if (IsNoData(z))
                        continue;

                    minElevation =
                        Math.Min(
                            minElevation,
                            z);

                    maxElevation =
                        Math.Max(
                            maxElevation,
                            z);
                }
            }

            if (minElevation == double.MaxValue)
                return new List<ContourLine>();

            // Например:
            // min = 805
            // interval = 10
            // первая горизонталь = 810
            double firstLevel =
                Math.Ceiling(
                    minElevation / interval) *
                interval;

            var result =
                new List<ContourLine>();

            for (double level = firstLevel;
                 level <= maxElevation;
                 level += interval)
            {
                List<Segment> segments =
                    BuildLevel(
                        dem,
                        level);

                if (segments.Count == 0)
                    continue;

                List<ContourLine> lines =
                    ConnectSegments(
                        segments,
                        level);

                result.AddRange(lines);
            }

            return result;
        }

        private static List<Segment> BuildLevel(
            DemGrid dem,
            double level)
        {
            var segments =
                new List<Segment>();

            for (int row = 0;
                 row < dem.Height - 1;
                 row++)
            {
                for (int column = 0;
                     column < dem.Width - 1;
                     column++)
                {
                    double z00 =
                        dem.Elevation[
                            row,
                            column];

                    double z10 =
                        dem.Elevation[
                            row,
                            column + 1];

                    double z11 =
                        dem.Elevation[
                            row + 1,
                            column + 1];

                    double z01 =
                        dem.Elevation[
                            row + 1,
                            column];

                    // NoData — такую ячейку пропускаем.
                    if (IsNoData(z00) ||
                        IsNoData(z10) ||
                        IsNoData(z11) ||
                        IsNoData(z01))
                    {
                        continue;
                    }

                    int caseIndex = 0;

                    if (z00 >= level)
                        caseIndex |= 1;

                    if (z10 >= level)
                        caseIndex |= 2;

                    if (z11 >= level)
                        caseIndex |= 4;

                    if (z01 >= level)
                        caseIndex |= 8;

                    if (caseIndex == 0 ||
                        caseIndex == 15)
                    {
                        continue;
                    }

                    ContourPoint top =
                        Interpolate(
                            dem,
                            row,
                            column,
                            row,
                            column + 1,
                            z00,
                            z10,
                            level);

                    ContourPoint right =
                        Interpolate(
                            dem,
                            row,
                            column + 1,
                            row + 1,
                            column + 1,
                            z10,
                            z11,
                            level);

                    ContourPoint bottom =
                        Interpolate(
                            dem,
                            row + 1,
                            column,
                            row + 1,
                            column + 1,
                            z01,
                            z11,
                            level);

                    ContourPoint left =
                        Interpolate(
                            dem,
                            row,
                            column,
                            row + 1,
                            column,
                            z00,
                            z01,
                            level);

                    switch (caseIndex)
                    {
                        case 1:
                            AddSegment(
                                segments,
                                left,
                                top);
                            break;

                        case 2:
                            AddSegment(
                                segments,
                                top,
                                right);
                            break;

                        case 3:
                            AddSegment(
                                segments,
                                left,
                                right);
                            break;

                        case 4:
                            AddSegment(
                                segments,
                                right,
                                bottom);
                            break;

                        case 5:
                            {
                                // Неоднозначный случай.
                                double center =
                                    (z00 +
                                     z10 +
                                     z11 +
                                     z01) / 4.0;

                                if (center >= level)
                                {
                                    AddSegment(
                                        segments,
                                        left,
                                        top);

                                    AddSegment(
                                        segments,
                                        right,
                                        bottom);
                                }
                                else
                                {
                                    AddSegment(
                                        segments,
                                        left,
                                        bottom);

                                    AddSegment(
                                        segments,
                                        top,
                                        right);
                                }

                                break;
                            }

                        case 6:
                            AddSegment(
                                segments,
                                top,
                                bottom);
                            break;

                        case 7:
                            AddSegment(
                                segments,
                                left,
                                bottom);
                            break;

                        case 8:
                            AddSegment(
                                segments,
                                bottom,
                                left);
                            break;

                        case 9:
                            AddSegment(
                                segments,
                                top,
                                bottom);
                            break;

                        case 10:
                            {
                                double center =
                                    (z00 +
                                     z10 +
                                     z11 +
                                     z01) / 4.0;

                                if (center >= level)
                                {
                                    AddSegment(
                                        segments,
                                        top,
                                        right);

                                    AddSegment(
                                        segments,
                                        bottom,
                                        left);
                                }
                                else
                                {
                                    AddSegment(
                                        segments,
                                        top,
                                        left);

                                    AddSegment(
                                        segments,
                                        right,
                                        bottom);
                                }

                                break;
                            }

                        case 11:
                            AddSegment(
                                segments,
                                right,
                                left);
                            break;

                        case 12:
                            AddSegment(
                                segments,
                                right,
                                left);
                            break;

                        case 13:
                            AddSegment(
                                segments,
                                top,
                                right);
                            break;

                        case 14:
                            AddSegment(
                                segments,
                                left,
                                top);
                            break;
                    }
                }
            }

            return segments;
        }

        private static ContourPoint Interpolate(
            DemGrid dem,
            int row1,
            int column1,
            int row2,
            int column2,
            double z1,
            double z2,
            double level)
        {
            double t;

            if (Math.Abs(z2 - z1) < 1e-12)
            {
                t = 0.5;
            }
            else
            {
                t =
                    (level - z1) /
                    (z2 - z1);

                t =
                    Math.Clamp(
                        t,
                        0.0,
                        1.0);
            }

            double row =
                row1 +
                (row2 - row1) * t;

            double column =
                column1 +
                (column2 - column1) * t;

            var coordinate =
                dem.GetCoordinate(
                    row,
                    column);

            return new ContourPoint(
                coordinate.Longitude,
                coordinate.Latitude);
        }

        private static void AddSegment(
            List<Segment> segments,
            ContourPoint a,
            ContourPoint b)
        {
            if (DistanceSquared(a, b) <
                PointTolerance * PointTolerance)
            {
                return;
            }

            segments.Add(
                new Segment(a, b));
        }

        private static List<ContourLine> ConnectSegments(
            List<Segment> segments,
            double level)
        {
            var result =
                new List<ContourLine>();

            var adjacency =
                new Dictionary<
                    PointKey,
                    List<int>>();

            var points =
                new Dictionary<
                    PointKey,
                    ContourPoint>();

            for (int i = 0;
                 i < segments.Count;
                 i++)
            {
                Segment segment =
                    segments[i];

                PointKey keyA =
                    GetKey(segment.A);

                PointKey keyB =
                    GetKey(segment.B);

                if (!adjacency.TryGetValue(
                        keyA,
                        out List<int>? listA))
                {
                    listA =
                        new List<int>();

                    adjacency[keyA] =
                        listA;

                    points[keyA] =
                        segment.A;
                }

                if (!adjacency.TryGetValue(
                        keyB,
                        out List<int>? listB))
                {
                    listB =
                        new List<int>();

                    adjacency[keyB] =
                        listB;

                    points[keyB] =
                        segment.B;
                }

                listA.Add(i);
                listB.Add(i);
            }

            var visited =
                new bool[segments.Count];

            for (int i = 0;
                 i < segments.Count;
                 i++)
            {
                if (visited[i])
                    continue;

                PointKey startKey =
                    GetStartKey(
                        segments[i],
                        adjacency);

                var path =
                    new List<ContourPoint>();

                PointKey current =
                    startKey;

                while (true)
                {
                    if (!points.TryGetValue(
                            current,
                            out ContourPoint currentPoint))
                    {
                        break;
                    }

                    if (path.Count == 0 ||
                        DistanceSquared(
                            path[^1],
                            currentPoint) >
                        PointTolerance *
                        PointTolerance)
                    {
                        path.Add(currentPoint);
                    }

                    int nextSegmentIndex =
                        -1;

                    List<int> connectedSegments =
                        adjacency[current];

                    foreach (int segmentIndex
                             in connectedSegments)
                    {
                        if (!visited[segmentIndex])
                        {
                            nextSegmentIndex =
                                segmentIndex;

                            break;
                        }
                    }

                    if (nextSegmentIndex < 0)
                        break;

                    visited[nextSegmentIndex] =
                        true;

                    Segment next =
                        segments[
                            nextSegmentIndex];

                    PointKey keyA =
                        GetKey(next.A);

                    PointKey keyB =
                        GetKey(next.B);

                    current =
                        current.Equals(keyA)
                            ? keyB
                            : keyA;
                }

                if (path.Count < 2)
                    continue;

                bool closed =
                    DistanceSquared(
                        path[0],
                        path[^1]) <
                    PointTolerance *
                    PointTolerance;

                result.Add(
                    new ContourLine(
                        level,
                        path,
                        closed));
            }

            return result;
        }

        private static PointKey GetStartKey(
            Segment segment,
            Dictionary<PointKey, List<int>> adjacency)
        {
            PointKey a =
                GetKey(segment.A);

            PointKey b =
                GetKey(segment.B);

            int countA =
                adjacency[a].Count;

            int countB =
                adjacency[b].Count;

            // Для открытой линии начинаем с конца.
            if (countA != 2)
                return a;

            if (countB != 2)
                return b;

            // Для замкнутой линии можно с любого узла.
            return a;
        }

        private static PointKey GetKey(
            ContourPoint point)
        {
            return new PointKey(
                (long)Math.Round(
                    point.Longitude /
                    PointTolerance),

                (long)Math.Round(
                    point.Latitude /
                    PointTolerance));
        }

        private static double DistanceSquared(
            ContourPoint a,
            ContourPoint b)
        {
            double dx =
                a.Longitude -
                b.Longitude;

            double dy =
                a.Latitude -
                b.Latitude;

            return dx * dx + dy * dy;
        }

        private static bool IsNoData(
            double value)
        {
            return value <= NoData;
        }
    }
}
