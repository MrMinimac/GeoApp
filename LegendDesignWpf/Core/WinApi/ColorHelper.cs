using Microsoft.Win32;
using System.Windows.Media;

namespace LegendDesignWpf.Core.WinApi
{
    public static class ColorHelper
    {
        public static Color GetAccent() => GetRawAccentColor();
        public static Color GetAccentLight() => Blend(GetRawAccentColor(), Colors.White, 0.7f);
        public static Color GetAccentDark() => Blend(GetRawAccentColor(), Colors.Black, 0.7f);

        public static Color GetRandomColor()
        {
            var random = new Random();

            double h = random.NextDouble() * 360;
            double s = 0.7 + random.NextDouble() * 0.3; // 0.7 - 1.0
            double v = 0.8 + random.NextDouble() * 0.2; // 0.8 - 1.0

            return ColorFromHSV(h, s, v);
        }

        private static Color Blend(Color baseColor, Color target, float intensity)
        {
            intensity = Math.Clamp(intensity, 0f, 1f);

            byte r = (byte)(baseColor.R * intensity + target.R * (1 - intensity));
            byte g = (byte)(baseColor.G * intensity + target.G * (1 - intensity));
            byte b = (byte)(baseColor.B * intensity + target.B * (1 - intensity));

            return Color.FromArgb(255, r, g, b);
        }

        private static Color GetRawAccentColor()
        {
            const String DWM_KEY = @"Software\Microsoft\Windows\DWM";
            using (RegistryKey dwmKey = Registry.CurrentUser.OpenSubKey(DWM_KEY, RegistryKeyPermissionCheck.ReadSubTree))
            {
                const String KEY_EX_MSG = "The \"HKCU\\" + DWM_KEY + "\" registry key does not exist.";
                if (dwmKey is null) throw new InvalidOperationException(KEY_EX_MSG);

                Object accentColorObj = dwmKey.GetValue("AccentColor");
                if (accentColorObj is Int32 accentColorDword)
                {
                    return ParseDWordColor(accentColorDword);
                }
                else
                {
                    const String VALUE_EX_MSG = "The \"HKCU\\" + DWM_KEY + "\\AccentColor\" registry key value could not be parsed as an ABGR color.";
                    throw new InvalidOperationException(VALUE_EX_MSG);
                }
            }


        }
        private static Color ParseDWordColor(Int32 color)
        {
            Byte
                a = (byte)((color >> 24) & 0xFF),
                b = (byte)((color >> 16) & 0xFF),
                g = (byte)((color >> 8) & 0xFF),
                r = (byte)((color >> 0) & 0xFF);

            return Color.FromArgb(a, r, g, b);
        }

        private static Color ColorFromHSV(double hue, double saturation, double value)
        {
            double c = value * saturation;
            double x = c * (1 - Math.Abs((hue / 60) % 2 - 1));
            double m = value - c;

            double r = 0, g = 0, b = 0;

            if (hue < 60)
                (r, g, b) = (c, x, 0);
            else if (hue < 120)
                (r, g, b) = (x, c, 0);
            else if (hue < 180)
                (r, g, b) = (0, c, x);
            else if (hue < 240)
                (r, g, b) = (0, x, c);
            else if (hue < 300)
                (r, g, b) = (x, 0, c);
            else
                (r, g, b) = (c, 0, x);

            return Color.FromRgb(
                (byte)((r + m) * 255),
                (byte)((g + m) * 255),
                (byte)((b + m) * 255));
        }
    }
}
