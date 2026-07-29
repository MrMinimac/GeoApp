using LegendDesignWpf.Core.WinApi;
using System.Windows;
using System.Windows.Media;

namespace LegendDesignWpf.Core.Theme
{
    public class ColorsStore
    {
        public Color WindowsColor => _windowsColor;
        public Color WindowsDarkColor => _windowsDarkColor;
        public Color WindowsLightColor => _windowsLightColor;

        public Brush WindowsBrush => _windowsBrush;
        public Brush WindowsDarkBrush => _windowsDarkBrush;
        public Brush WindowsLightBrush => _windowsLightBrush;

        public Color DefaultColor => _defaultColor;
        public Color DefaultDarkColor => _defaultDarkColor;
        public Color DefaultLightColor => _defaultLightColor;

        public Brush DefaultBrush => _defaultBrush;
        public Brush DefaultDarkBrush => _defaultDarkBrush;
        public Brush DefaultLightBrush => _defaultLightBrush;


        private readonly Color _windowsColor;
        private readonly Color _windowsDarkColor;
        private readonly Color _windowsLightColor;

        private Brush _windowsBrush;
        private Brush _windowsDarkBrush;
        private Brush _windowsLightBrush;

        private readonly Color _defaultColor;
        private readonly Color _defaultDarkColor;
        private readonly Color _defaultLightColor;

        private readonly Brush _defaultBrush;
        private readonly Brush _defaultDarkBrush;
        private readonly Brush _defaultLightBrush;

        private Application _app;

        public ColorsStore(Application app)
        {
            _app = app;

            _windowsColor = ColorHelper.GetAccent();
            _windowsDarkColor = ColorHelper.GetAccentDark();
            _windowsLightColor = ColorHelper.GetAccentLight();

            _windowsBrush = new SolidColorBrush(Color.FromArgb(_windowsColor.A, _windowsColor.R, _windowsColor.G, _windowsColor.B));
            _windowsDarkBrush = new SolidColorBrush(Color.FromArgb(_windowsDarkColor.A, _windowsDarkColor.R, _windowsDarkColor.G, _windowsDarkColor.B));
            _windowsLightBrush = new SolidColorBrush(Color.FromArgb(_windowsLightColor.A, _windowsLightColor.R, _windowsLightColor.G, _windowsLightColor.B));

            _defaultColor = GetResource("AccentPaint", Colors.DodgerBlue);
            _defaultDarkColor = GetResource("AccentPaintDark", Colors.MidnightBlue);
            _defaultLightColor = GetResource("AccentPaintLight", Colors.LightSkyBlue);

            _defaultBrush = new SolidColorBrush(_defaultColor);
            _defaultDarkBrush = new SolidColorBrush(_defaultDarkColor);
            _defaultLightBrush = new SolidColorBrush(_defaultLightColor);
        }

        public T GetResource<T>(string key, T fallback)
        {
            if (_app.Resources.Contains(key))
            {
                var value = _app.Resources[key];
                if (value is T typed)
                    return typed;
            }

            throw new NullReferenceException($"Resource {key} was null. " +
                $"Make sure you've added styles to your resources.\n\n" +
                $"<ResourceDictionary Source=\"/LegendDesignWpf;component/Themes/DarkTheme.xaml\" />\n" +
                $"and\n" +
                $"<ResourceDictionary Source=\"/LegendDesignWpf;component/Themes/Generic.xaml\" />\n");
        }
    }
}
