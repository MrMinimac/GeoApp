namespace GeoCadPlugin.Topography
{
    internal static class ContourJoiner
    {
        private const double MetersPerDegreeLat = 111_320.0;

        public static List<TopographyLine> Join(
            List<TopographyLine> lines,
            double maxGapMeters = 100.0,
            double maxTailMeters = 300.0,
            int smoothRadius = 6, // Радиус сглаживания: сколько точек влево и вправо от стыка сглаживать
            int smoothIterations = 5 // Количество проходов сглаживания (чем больше, тем плавнее)
        )
        {
            if (lines == null)
                throw new ArgumentNullException(nameof(lines));

            var result = new List<TopographyLine>();

            // Замкнутые и вырожденные линии не трогаем
            foreach (TopographyLine line in lines)
            {
                if (line.IsClosed || line.Points.Count < 2)
                    result.Add(line);
            }

            var groups = lines
                .Where(l => !l.IsClosed && l.Points.Count >= 2)
                .GroupBy(l => l.Elevation);

            foreach (var group in groups)
            {
                var parts = group.Select(l => new List<ContourPoint>(l.Points)).ToList();

                parts = JoinGroup(
                    parts,
                    maxGapMeters,
                    maxTailMeters,
                    smoothRadius,
                    smoothIterations
                );

                foreach (var pts in parts)
                    result.Add(new TopographyLine(group.Key, pts, false));
            }

            return result;
        }

        private static List<List<ContourPoint>> JoinGroup(
            List<List<ContourPoint>> parts,
            double maxGap,
            double maxTail,
            int smoothRadius,
            int smoothIterations
        )
        {
            double lat0 = parts[0][0].Latitude;
            double mx = MetersPerDegreeLat * Math.Cos(lat0 * Math.PI / 180.0);
            double my = MetersPerDegreeLat;

            while (parts.Count > 1)
            {
                double best = maxGap;
                int bx = -1,
                    by = -1,
                    bj = -1;
                bool bxAtStart = false;

                for (int x = 0; x < parts.Count; x++)
                {
                    List<ContourPoint> px = parts[x];

                    for (int e = 0; e < 2; e++)
                    {
                        ContourPoint p = e == 0 ? px[0] : px[^1];

                        for (int y = 0; y < parts.Count; y++)
                        {
                            if (y == x)
                                continue;

                            List<ContourPoint> py = parts[y];

                            for (int j = 0; j < py.Count; j++)
                            {
                                double d = Dist(p, py[j], mx, my);

                                if (d >= best)
                                    continue;

                                // Хвост, который придётся отрезать у линии y
                                double tail = Math.Min(
                                    PathLength(py, 0, j, mx, my),
                                    PathLength(py, j, py.Count - 1, mx, my)
                                );

                                if (tail > maxTail)
                                    continue;

                                best = d;
                                bx = x;
                                by = y;
                                bj = j;
                                bxAtStart = e == 0;
                            }
                        }
                    }
                }

                if (bx < 0)
                    break;

                List<ContourPoint> X = parts[bx];
                List<ContourPoint> Y = parts[by];

                // Ориентируем X так, чтобы нужный конец был последним
                var merged = new List<ContourPoint>(X);
                if (bxAtStart)
                    merged.Reverse();

                double headLen = PathLength(Y, 0, bj, mx, my);
                double tailLen = PathLength(Y, bj, Y.Count - 1, mx, my);

                // Оставляем длинную часть, начиная с точки соединения
                List<ContourPoint> keep;

                if (tailLen >= headLen)
                {
                    keep = Y.GetRange(bj, Y.Count - bj);
                }
                else
                {
                    keep = Y.GetRange(0, bj + 1);
                    keep.Reverse();
                }

                // Если точки совпадают, дубль не добавляем
                int startIndex = Dist(merged[^1], keep[0], mx, my) < 1e-3 ? 1 : 0;

                // Запоминаем индекс стыка перед добавлением второй части
                int jointIndex = merged.Count - 1;

                for (int i = startIndex; i < keep.Count; i++)
                    merged.Add(keep[i]);

                // Локально сглаживаем только место соединения (шов)
                if (smoothIterations > 0 && smoothRadius > 0)
                {
                    SmoothJoint(merged, jointIndex, smoothRadius, smoothIterations);
                }

                // Удаляем две исходные линии (сначала с большим индексом)
                parts.RemoveAt(Math.Max(bx, by));
                parts.RemoveAt(Math.Min(bx, by));
                parts.Add(merged);
            }

            return parts;
        }

        // --- Метод для ЛОКАЛЬНОГО сглаживания стыка ---
        private static void SmoothJoint(
            List<ContourPoint> pts,
            int jointIndex,
            int radius,
            int iterations
        )
        {
            // Определяем безопасные границы для сглаживания (не трогаем самые края линии)
            int start = Math.Max(1, jointIndex - radius);
            int end = Math.Min(pts.Count - 2, jointIndex + radius);

            if (start >= end)
                return; // Недостаточно точек для сглаживания

            var temp = new ContourPoint[pts.Count];

            for (int iter = 0; iter < iterations; iter++)
            {
                // Считаем новые координаты только для заданного участка
                for (int i = start; i <= end; i++)
                {
                    double avgLon =
                        (pts[i - 1].Longitude + pts[i].Longitude + pts[i + 1].Longitude) / 3.0;
                    double avgLat =
                        (pts[i - 1].Latitude + pts[i].Latitude + pts[i + 1].Latitude) / 3.0;

                    temp[i] = new ContourPoint { Longitude = avgLon, Latitude = avgLat };
                }

                // Применяем новые координаты к исходному списку (только в пределах окна сглаживания)
                for (int i = start; i <= end; i++)
                {
                    pts[i] = temp[i];
                }
            }
        }

        private static double PathLength(
            List<ContourPoint> pts,
            int from,
            int to,
            double mx,
            double my
        )
        {
            double sum = 0;

            for (int i = from; i < to; i++)
                sum += Dist(pts[i], pts[i + 1], mx, my);

            return sum;
        }

        private static double Dist(ContourPoint a, ContourPoint b, double mx, double my)
        {
            double dx = (a.Longitude - b.Longitude) * mx;
            double dy = (a.Latitude - b.Latitude) * my;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
