using LegendDesignWpf.Core.Enums;
using System.Windows;

namespace LegendDesignWpf.Core.Theme
{
    public class ThemeManager : ITheme
    {
        public ColorsStore ColorsStore { get; }

        private Application _app;
        private AppThemes _currentTheme;
        private AccentSource _accentSource;

        public AppThemes CurrentTheme => _currentTheme;
        public AccentSource AccentSource => _accentSource;

        public event Action<AppThemes>? OnThemeChanged;
        public event Action<AccentSource>? OnAccentSourceChanged;

        public ThemeManager()
        {
            _app = Application.Current;
            ColorsStore = new ColorsStore(_app);
        }

        public bool CanHotSwap(AppThemes theme)
        {
            var fromKind = ThemeRegistry.Themes[CurrentTheme].Kind;
            var toKind = ThemeRegistry.Themes[theme].Kind;

            return fromKind == toKind;
        }

        public void SetAccentSource(AccentSource accentSource)
        {
            if (accentSource == AccentSource.Default)
            {
                _app.Resources["AccentPaint"] = ColorsStore.DefaultColor;
                _app.Resources["AccentPaintDark"] = ColorsStore.DefaultDarkColor;
                _app.Resources["AccentPaintLight"] = ColorsStore.DefaultLightColor;
                _app.Resources["AccentBrush"] = ColorsStore.DefaultBrush;
                _app.Resources["AccentBrushDark"] = ColorsStore.DefaultDarkBrush;
                _app.Resources["AccentBrushLight"] = ColorsStore.DefaultLightBrush;
            }
            else if (accentSource == AccentSource.Windows)
            {
                _app.Resources["AccentPaint"] = ColorsStore.WindowsColor;
                _app.Resources["AccentPaintDark"] = ColorsStore.WindowsDarkColor;
                _app.Resources["AccentPaintLight"] = ColorsStore.WindowsLightColor;
                _app.Resources["AccentBrush"] = ColorsStore.WindowsBrush;
                _app.Resources["AccentBrushDark"] = ColorsStore.WindowsDarkBrush;
                _app.Resources["AccentBrushLight"] = ColorsStore.WindowsLightBrush;
            }

            _accentSource = accentSource;
            OnAccentSourceChanged?.Invoke(accentSource);
        }

        public void ApplyTheme(AppThemes theme)
        {
            if (CurrentTheme == theme)
                return;

            string themePath = ThemeRegistry.Themes[theme].ResourcePath;
            var dict = new ResourceDictionary { Source = new Uri(themePath, UriKind.Relative) };
            var resources = _app.Resources.MergedDictionaries;
            var oldTheme = resources.FirstOrDefault(d => d.Contains("ThemeMarker"));

            if (oldTheme != null)
            {
                resources.Remove(oldTheme);
            }

            resources.Add(dict);

            OnThemeChanged?.Invoke(theme);
            _currentTheme = theme;
        }
    }
}
