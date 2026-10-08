namespace GeoCadPlugin.Topography
{
    internal static class ContourDespiker
    {
        private const double MetersPerDegreeLat = 111_320.0;

        public static List<TopographyLine> Despike(
            List<TopographyLine> lines,
            double sharpAngleDeg = 100.0,
            double mildAngleDeg = 50.0,
            double shortSegmentMeters = 40.0
        )
        {
            if (lines == null)
                throw new ArgumentNullException(nameof(lines));

            double cosSharp = Math.Cos(sharpAngleDeg * Math.PI / 180.0);
            double cosMild = Math.Cos(mildAngleDeg * Math.PI / 180.0);

            var result = new List<TopographyLine>(lines.Count);

            foreach (TopographyLine line in lines)
            {
                List<ContourPoint> pts = DespikeLine(line, cosSharp, cosMild, shortSegmentMeters);

                int min = line.IsClosed ? 3 : 2;

                if (pts.Count >= min)
                    result.Add(new TopographyLine(line.Elevation, pts, line.IsClosed));
            }

            return result;
        }

        private static List<ContourPoint> DespikeLine(
            TopographyLine line,
            double cosSharp,
            double cosMild,
            double shortSeg
        )
        {
            var pts = new List<ContourPoint>(line.Points);

            if (line.IsClosed && pts.Count > 1 && pts[0] == pts[^1])
                pts.RemoveAt(pts.Count - 1);

            if (pts.Count < 3)
                return pts;

            double lat0 = pts[0].Latitude;
            double lon0 = pts[0].Longitude;
            double mx = MetersPerDegreeLat * Math.Cos(lat0 * Math.PI / 180.0);
            double my = MetersPerDegreeLat;

            bool closed = line.IsClosed;
            bool changed = true;

            while (changed && pts.Count >= (closed ? 4 : 3))
            {
                changed = false;

                int from = closed ? 0 : 1;
                int to = closed ? pts.Count : pts.Count - 1;

                for (int i = from; i < to; i++)
                {
                    int n = pts.Count;
                    ContourPoint prev = pts[(i - 1 + n) % n];
                    ContourPoint cur = pts[i];
                    ContourPoint next = pts[(i + 1) % n];

                    double ax = (cur.Longitude - prev.Longitude) * mx;
                    double ay = (cur.Latitude - prev.Latitude) * my;
                    double bx = (next.Longitude - cur.Longitude) * mx;
                    double by = (next.Latitude - cur.Latitude) * my;

                    double la = Math.Sqrt(ax * ax + ay * ay);
                    double lb = Math.Sqrt(bx * bx + by * by);

                    if (la < 1e-6 || lb < 1e-6)
                    {
                        pts.RemoveAt(i);
                        changed = true;
                        break;
                    }

                    // cos угла между направлениями: 1 = прямо, -1 = разворот
                    double cos = (ax * bx + ay * by) / (la * lb);

                    bool sharp = cos < cosSharp;
                    bool mildAndShort = cos < cosMild && Math.Min(la, lb) < shortSeg;

                    if (sharp || mildAndShort)
                    {
                        pts.RemoveAt(i);
                        changed = true;
                        break; // индексы сдвинулись, начинаем проход заново
                    }
                }

                if (pts.Count < (closed ? 3 : 2))
                    break;
            }

            return pts;
        }
    }
}
