using Microsoft.Win32;
using System.Windows.Media;

namespace LegendDesignWpf.Core.WinApi
{
    public static class ColorHelper
    {
        public static Color GetAccent() => GetRawAccentColor();
        public static Color GetAccentLight() => Blend(GetRawAccentColor(), Colors.White, 0.7f);
        public static Color GetAccentDark() => Blend(GetRawAccentColor(), Colors.Black, 0.7f);

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
    }
}
