using GeoAppCore.Hatch;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GeoAppWpf.Services.Excel.Render
{
    public record GeoColumnHatchConfig(
        IReadOnlyList<(double Start, double End, HatchConfig Hatch)> Bands,
        double MinDepth,
        double MaxDepth,
        System.Drawing.Color? BoundaryColor = null,
        double BoundaryThickness = 1.0);

    public static class HatchImageRenderer
    {
        public static byte[] RenderColumn(int width, int height, GeoColumnHatchConfig column)
        {
            var visual = new DrawingVisual();
            double totalDepth = column.MaxDepth - column.MinDepth;

            using (var dc = visual.RenderOpen())
            {
                foreach (var band in column.Bands)
                {
                    double yStart = (band.Start - column.MinDepth) / totalDepth * height;
                    double yEnd = (band.End - column.MinDepth) / totalDepth * height;
                    var area = new Rect(0, yStart, width, Math.Max(1, yEnd - yStart));

                    dc.PushClip(new RectangleGeometry(area));
                    DrawBand(dc, area, width, height, band.Hatch.Elements, band.Hatch.Color, band.Hatch.MaxClippedFraction);
                    dc.Pop();
                }
            }

            // Границы — отдельным дочерним визуалом с отключённым AA
            var boundariesVisual = new DrawingVisual();
            RenderOptions.SetEdgeMode(boundariesVisual, EdgeMode.Aliased);

            using (var dc = boundariesVisual.RenderOpen())
            {
                DrawBoundaries(dc, column, width, height);
            }

            visual.Children.Add(boundariesVisual);

            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = new MemoryStream();
            encoder.Save(stream);
            return stream.ToArray();
        }

        private static void DrawBoundaries(DrawingContext dc, GeoColumnHatchConfig column, int width, int height)
        {
            if (column.Bands.Count == 0)
                return;

            var drawColor = column.BoundaryColor.HasValue
                ? Color.FromArgb(
                    column.BoundaryColor.Value.A,
                    column.BoundaryColor.Value.R,
                    column.BoundaryColor.Value.G,
                    column.BoundaryColor.Value.B)
                : Colors.Black;

            var pen = new Pen(new SolidColorBrush(drawColor), column.BoundaryThickness);
            pen.Freeze(); // не обязательно, но полезно для производительности при частой отрисовке

            double totalDepth = column.MaxDepth - column.MinDepth;

            var boundaries = column.Bands
                .SelectMany(b => new[] { b.Start, b.End })
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            foreach (var depth in boundaries)
            {
                double y = (depth - column.MinDepth) / totalDepth * height;

                if (y <= 0 || y >= height)
                    continue;

                // Снэппинг к сетке пикселей: округляем до целого пикселя
                // и сдвигаем на половину толщины пера, чтобы штрих не
                // растягивался на два ряда пикселей.
                double snappedY = Math.Round(y) + (column.BoundaryThickness / 2.0 % 1.0);

                dc.DrawLine(pen, new Point(0, snappedY), new Point(width, snappedY));
            }
        }

        private static void DrawBand(
            DrawingContext dc,
            Rect area,
            int canvasWidth,
            int canvasHeight,
            IReadOnlyList<HatchElement> elements,
            System.Drawing.Color? color,
            double maxClippedFraction = 0.5)
        {
            var occupied = new List<Rect>();

            foreach (var element in elements)
            {
                if (!GeometriesData.Geometries.TryGetValue(element.Geometry, out var definition))
                    continue;

                var geometry = Geometry.Parse(definition.Markup);
                var rawBounds = geometry.Bounds;
                var rawCenterX = rawBounds.X + rawBounds.Width / 2.0;
                var rawCenterY = rawBounds.Y + rawBounds.Height / 2.0;

                var unrotatedBounds = GetGeometryBounds(geometry, element.Scale, 0, 0, 0);

                double baseOffsetX = unrotatedBounds.Width <= element.StepX
                    ? element.StepX / 2.0 - (unrotatedBounds.X + unrotatedBounds.Width / 2.0)
                    : 0;

                double baseOffsetY = unrotatedBounds.Height <= element.StepY
                    ? element.StepY / 2.0 - (unrotatedBounds.Y + unrotatedBounds.Height / 2.0)
                    : 0;

                var drawColor = color.HasValue
                    ? Color.FromArgb(color.Value.A, color.Value.R, color.Value.G, color.Value.B)
                    : Colors.Black;

                var brush = new SolidColorBrush(drawColor);
                var pen = new Pen(brush, 1);

                // ВАЖНО: сетка теперь строится в границах полосы (area), а не всего изображения
                var cells = new List<(int X, int Y)>();
                for (int y = (int)area.Top; y < area.Bottom; y += element.StepY)
                {
                    for (int x = 0; x < canvasWidth; x += element.StepX)
                    {
                        cells.Add((x, y));
                    }
                }

                for (int i = cells.Count - 1; i > 0; i--)
                {
                    int j = Random.Shared.Next(i + 1);
                    (cells[i], cells[j]) = (cells[j], cells[i]);
                }

                foreach (var (x, y) in cells)
                {
                    int attempts = element.RandomOffset ? Math.Max(1, element.PlacementAttempts) : 1;

                    for (int attempt = 0; attempt < attempts; attempt++)
                    {
                        int offsetX = (int)Math.Round(baseOffsetX);
                        int offsetY = (int)Math.Round(baseOffsetY);

                        if (element.RandomOffset)
                        {
                            offsetX += Random.Shared.Next(-element.StepX / 4, element.StepX / 4 + 1);
                            offsetY += Random.Shared.Next(-element.StepY / 4, element.StepY / 4 + 1);
                        }

                        double rotation = element.RandomRotation
                            ? Random.Shared.NextDouble() * 360.0
                            : 0.0;

                        Rect bounds = GetGeometryBounds(geometry, element.Scale, x + offsetX, y + offsetY, rotation);

                        // клип считаем относительно ВСЕГО холста, а не только полосы —
                        // иначе фигуры у верхней/нижней границы полосы будут излишне обрезаться
                        if (GetClippedFraction(bounds, canvasWidth, canvasHeight) > maxClippedFraction)
                            continue;

                        if (IntersectsOccupied(bounds, occupied) && !element.IngnoreInrersections)
                            continue;

                        dc.PushTransform(new TranslateTransform(x + offsetX, y + offsetY));

                        if (element.Scale != 1.0)
                            dc.PushTransform(new ScaleTransform(element.Scale, element.Scale));

                        if (element.RandomRotation)
                            dc.PushTransform(new RotateTransform(rotation, rawCenterX, rawCenterY));

                        dc.DrawGeometry(
                            definition.Filled ? brush : null,
                            definition.Filled ? null : pen,
                            geometry);

                        if (element.RandomRotation) dc.Pop();
                        if (element.Scale != 1.0) dc.Pop();
                        dc.Pop();

                        occupied.Add(bounds);
                        break;
                    }
                }
            }
        }

        public static byte[] Render(
            int width,
            int height,
            IReadOnlyList<HatchElement> elements,
            System.Drawing.Color? color = null,
            double maxClippedFraction = 0.5)
        {
            width = Math.Max(1, width);
            height = Math.Max(1, height);

            maxClippedFraction = Math.Clamp(
                maxClippedFraction,
                0.0,
                1.0);

            var visual = new DrawingVisual();

            // Уже занятые области.
            var occupied = new List<Rect>();

            using (var dc = visual.RenderOpen())
            {
                foreach (var element in elements)
                {
                    if (!GeometriesData.Geometries.TryGetValue(
                            element.Geometry,
                            out var definition))
                    {
                        continue;
                    }

                    var geometry = Geometry.Parse(definition.Markup);

                    var rawBounds = geometry.Bounds;
                    var rawCenterX = rawBounds.X + rawBounds.Width / 2.0;
                    var rawCenterY = rawBounds.Y + rawBounds.Height / 2.0;

                    var unrotatedBounds = GetGeometryBounds(
                        geometry,
                        element.Scale,
                        0,
                        0,
                        0);

                    double baseOffsetX = unrotatedBounds.Width <= element.StepX
                        ? element.StepX / 2.0
                            - (unrotatedBounds.X + unrotatedBounds.Width / 2.0)
                        : 0;

                    double baseOffsetY = unrotatedBounds.Height <= element.StepY
                        ? element.StepY / 2.0
                            - (unrotatedBounds.Y + unrotatedBounds.Height / 2.0)
                        : 0;

                    var drawColor = color.HasValue
                        ? Color.FromArgb(
                            color.Value.A,
                            color.Value.R,
                            color.Value.G,
                            color.Value.B)
                        : Colors.Black;

                    var brush = new SolidColorBrush(drawColor);
                    var pen = new Pen(brush, 1);

                    var cells = new List<(int X, int Y)>();
                    for (int y = 0; y < height; y += element.StepY)
                    {
                        for (int x = 0; x < width; x += element.StepX)
                        {
                            cells.Add((x, y));
                        }
                    }

                    // Fisher–Yates shuffle.
                    for (int i = cells.Count - 1; i > 0; i--)
                    {
                        int j = Random.Shared.Next(i + 1);
                        (cells[i], cells[j]) = (cells[j], cells[i]);
                    }

                    foreach (var (x, y) in cells)
                    {
                        int attempts = element.RandomOffset
                            ? Math.Max(1, element.PlacementAttempts)
                            : 1;

                        for (int attempt = 0; attempt < attempts; attempt++)
                        {
                            int offsetX = (int)Math.Round(baseOffsetX);
                            int offsetY = (int)Math.Round(baseOffsetY);

                            if (element.RandomOffset)
                            {
                                offsetX += Random.Shared.Next(
                                    -element.StepX / 4,
                                    element.StepX / 4 + 1);

                                offsetY += Random.Shared.Next(
                                    -element.StepY / 4,
                                    element.StepY / 4 + 1);
                            }

                            double rotation = element.RandomRotation
                                ? Random.Shared.NextDouble() * 360.0
                                : 0.0;

                            Rect bounds = GetGeometryBounds(
                                geometry,
                                element.Scale,
                                x + offsetX,
                                y + offsetY,
                                rotation);

                            if (GetClippedFraction(
                                    bounds,
                                    width,
                                    height) > maxClippedFraction)
                            {
                                continue;
                            }

                            if (IntersectsOccupied(bounds, occupied) && !element.IngnoreInrersections)
                                continue;

                            dc.PushTransform(
                                new TranslateTransform(
                                    x + offsetX,
                                    y + offsetY));

                            if (element.Scale != 1.0)
                            {
                                dc.PushTransform(
                                    new ScaleTransform(
                                        element.Scale,
                                        element.Scale));
                            }

                            if (element.RandomRotation)
                            {
                                dc.PushTransform(
                                    new RotateTransform(
                                        rotation,
                                        rawCenterX,
                                        rawCenterY));
                            }

                            dc.DrawGeometry(
                                definition.Filled ? brush : null,
                                definition.Filled ? null : pen,
                                geometry);

                            if (element.RandomRotation)
                                dc.Pop();

                            if (element.Scale != 1.0)
                                dc.Pop();

                            dc.Pop();

                            occupied.Add(bounds);

                            break;
                        }

                        // Не удалось найти свободное место —
                        // просто пропускаем элемент.
                    }
                }
            }

            var bitmap = new RenderTargetBitmap(
                width,
                height,
                96,
                96,
                PixelFormats.Pbgra32);

            bitmap.Render(visual);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using var stream = new MemoryStream();
            encoder.Save(stream);

            return stream.ToArray();
        }

        private static Rect GetGeometryBounds(
            Geometry geometry,
            double scale,
            double x,
            double y,
            double rotation = 0)
        {
            var bounds = geometry.Bounds;

            Rect current = bounds;

            if (rotation != 0)
            {
                var centerX = bounds.X + bounds.Width / 2.0;
                var centerY = bounds.Y + bounds.Height / 2.0;

                var rotate = new RotateTransform(
                    rotation,
                    centerX,
                    centerY);

                current = rotate.TransformBounds(current);
            }

            if (scale != 1.0)
            {
                var scaleTransform = new ScaleTransform(scale, scale);
                current = scaleTransform.TransformBounds(current);
            }

            return new Rect(
                x + current.X,
                y + current.Y,
                current.Width,
                current.Height);
        }

        private static double GetClippedFraction(Rect bounds, int width, int height)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return 1.0;

            var canvas = new Rect(
                0,
                0,
                width,
                height);

            var visible = Rect.Intersect(
                bounds,
                canvas);

            if (visible.IsEmpty)
                return 1.0;

            double totalArea = bounds.Width * bounds.Height;
            double visibleArea = visible.Width * visible.Height;

            return 1.0 - visibleArea / totalArea;
        }

        private static bool IntersectsOccupied(Rect candidate, List<Rect> occupied)
        {
            foreach (var existing in occupied)
            {
                if (candidate.IntersectsWith(existing))
                    return true;
            }

            return false;
        }
    }
}