using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace LegendDesignWpf.WinApi
{
    public sealed class WindowResizeHelper
        {
            private readonly Window _window;

            private const int WM_NCHITTEST = 0x0084;

            private const int HTCLIENT = 1;
            private const int HTCAPTION = 2;
            private const int HTLEFT = 10;
            private const int HTRIGHT = 11;
            private const int HTTOP = 12;
            private const int HTTOPLEFT = 13;
            private const int HTTOPRIGHT = 14;
            private const int HTBOTTOM = 15;
            private const int HTBOTTOMLEFT = 16;
            private const int HTBOTTOMRIGHT = 17;

            private const int ResizeBorder = 8;
            private const int CaptionHeight = 30;

            public WindowResizeHelper(Window window)
            {
                _window = window;
                var hwnd = new WindowInteropHelper(_window).Handle;
                HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);
            }

            private IntPtr WndProc(
                IntPtr hwnd,
                int msg,
                IntPtr wParam,
                IntPtr lParam,
                ref bool handled)
            {
                if (msg != WM_NCHITTEST)
                    return IntPtr.Zero;

                handled = true;

                var mouseScreen = GetMousePosition(lParam);
                var mouseWindow = _window.PointFromScreen(mouseScreen);

                double width = _window.ActualWidth;
                double height = _window.ActualHeight;

                // 1. УГЛЫ
                if (mouseWindow.X <= ResizeBorder && mouseWindow.Y <= ResizeBorder)
                    return (IntPtr)HTTOPLEFT;

                if (mouseWindow.X >= width - ResizeBorder && mouseWindow.Y <= ResizeBorder)
                    return (IntPtr)HTTOPRIGHT;

                if (mouseWindow.X <= ResizeBorder && mouseWindow.Y >= height - ResizeBorder)
                    return (IntPtr)HTBOTTOMLEFT;

                if (mouseWindow.X >= width - ResizeBorder && mouseWindow.Y >= height - ResizeBorder)
                    return (IntPtr)HTBOTTOMRIGHT;

                // 2. СТОРОНЫ
                if (mouseWindow.Y <= ResizeBorder)
                    return (IntPtr)HTTOP;

                if (mouseWindow.Y >= height - ResizeBorder)
                    return (IntPtr)HTBOTTOM;

                if (mouseWindow.X <= ResizeBorder)
                    return (IntPtr)HTLEFT;

                if (mouseWindow.X >= width - ResizeBorder)
                    return (IntPtr)HTRIGHT;

                // 3. Если курсор над кнопками — это CLIENT
                if (IsOverInteractiveElement(mouseWindow))
                    return (IntPtr)HTCLIENT;

                // 4. CAPTION
                if (mouseWindow.Y <= CaptionHeight)
                    return (IntPtr)HTCAPTION;

                return (IntPtr)HTCLIENT;
            }

            private bool IsOverInteractiveElement(Point p)
            {
                var element = _window.InputHitTest(p) as DependencyObject;

                while (element != null)
                {
                    if (element is Button || element is ToggleButton)
                        return true;

                    if (element is Visual || element is Visual3D)
                    {
                        element = VisualTreeHelper.GetParent(element);
                    }
                    else
                    {
                        element = LogicalTreeHelper.GetParent(element);
                    }
                }

                return false;
            }

            private static Point GetMousePosition(IntPtr lParam)
            {
                int x = unchecked((short)(lParam.ToInt32() & 0xFFFF));
                int y = unchecked((short)(lParam.ToInt32() >> 16));
                return new Point(x, y);
            }
        }
}
