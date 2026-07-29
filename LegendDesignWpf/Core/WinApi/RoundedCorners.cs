using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace LegendDesignWpf.WinApi
{
    public static class RoundedCorners
    {
        public enum DWMWINDOWATTRIBUTE
        {
            DWMWA_WINDOW_CORNER_PREFERENCE = 33
        }

        public enum DWM_WINDOW_CORNER_PREFERENCE
        {
            DWMWCP_DEFAULT = 0,     // по умолчанию (зависит от темы Windows)
            DWMWCP_DONOTROUND = 1,  // без скругления (прямые)
            DWMWCP_ROUND = 2,       // обычные скругления
            DWMWCP_ROUNDSMALL = 3   // маленькие скругления
        }

        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern long DwmSetWindowAttribute(IntPtr hwnd,
                                                         DWMWINDOWATTRIBUTE attribute,
                                                         ref DWM_WINDOW_CORNER_PREFERENCE pvAttribute,
                                                         uint cbAttribute);

        public static void Apply(Window window, CornerPreference preference)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
                throw new InvalidOperationException("HWND ещё не создан");

            var attribute = DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE;
            var mapped = (DWM_WINDOW_CORNER_PREFERENCE)(int)preference;
            DwmSetWindowAttribute(hwnd, attribute, ref mapped, sizeof(uint));
        }

        public static uint GetDpi(IntPtr hwnd)
        {
            return GetDpiForWindow(hwnd);
        }

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);
    }
}
