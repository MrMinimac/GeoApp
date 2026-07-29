using System.Windows.Media;

namespace LegendDesignWpf.Converters
{
    public static class ColorConverter
    {
        public static Color HsvToColor(double h, double s, double v)
        {
            double c = v * s;
            double x = c * (1 - Math.Abs((h / 60 % 2) - 1));
            double m = v - c;

            double r = 0, g = 0, b = 0;

            if (h < 60) { r = c; g = x; }
            else if (h < 120) { r = x; g = c; }
            else if (h < 180) { g = c; b = x; }
            else if (h < 240) { g = x; b = c; }
            else if (h < 300) { r = x; b = c; }
            else { r = c; b = x; }

            return Color.FromRgb(
                (byte)((r + m) * 255),
                (byte)((g + m) * 255),
                (byte)((b + m) * 255));
        }

        public static void ColorToHsv(Color color,
            out double h, out double s, out double v)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            h = delta == 0 ? 0 :
                max == r ? 60 * (((g - b) / delta) % 6) :
                max == g ? 60 * (((b - r) / delta) + 2) :
                           60 * (((r - g) / delta) + 4);

            if (h < 0) h += 360;

            s = max == 0 ? 0 : delta / max;
            v = max;
        }
    }
}
