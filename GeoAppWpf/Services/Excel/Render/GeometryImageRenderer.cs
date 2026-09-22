using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GeoAppWpf.Services.Excel.Render
{
    public static class GeometryImageRenderer
    {
        public static byte[] RenderToPng(
            string geometryMarkup,
            int width = 32,
            int height = 32,
            System.Drawing.Color? color = null)
        {
            var geometry = Geometry.Parse(geometryMarkup);

            var drawColor = color.HasValue
                ? Color.FromArgb(color.Value.A, color.Value.R, color.Value.G, color.Value.B)
                : Colors.Black;

            var brush = new SolidColorBrush(drawColor);

            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                dc.DrawGeometry(brush, null, geometry);
            }

            var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using var ms = new MemoryStream();
            encoder.Save(ms);
            return ms.ToArray();
        }
    }
}
