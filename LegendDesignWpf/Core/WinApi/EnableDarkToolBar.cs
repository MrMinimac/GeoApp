using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace LegendDesignWpf.WinApi
{
    public static class EnableDarkToolBar
        {
            public static void Enable(Window window, bool dark)
            {
                var hwnd = new WindowInteropHelper(window).Handle;

                // 0 = светлая тема, 1 = тёмная
                int darkModeEnabled = dark ? 1 : 0;

                // включает системное меню в нужном режиме
                SetPreferredAppMode(darkModeEnabled == 1 ? 2 : 0);
                FlushMenuThemes();

                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkModeEnabled, sizeof(int));
            }

            #region ImportDarkThemeDlls
            [DllImport("uxtheme.dll", EntryPoint = "#135", SetLastError = true, CharSet = CharSet.Unicode)]
            private static extern int SetPreferredAppMode(int preferredAppMode);

            [DllImport("uxtheme.dll", EntryPoint = "#136", SetLastError = true, CharSet = CharSet.Unicode)]
            private static extern void FlushMenuThemes();

            [DllImport("dwmapi.dll")]
            public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
            const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20; // Для Windows 10 версии 1809 и выше
            #endregion
        }
}
