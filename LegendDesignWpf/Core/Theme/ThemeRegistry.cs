using LegendDesignWpf.Core.Enums;

namespace LegendDesignWpf.Core.Theme
{
    public static class ThemeRegistry
    {
        public static readonly IReadOnlyDictionary<AppThemes, ThemeDescriptor> Themes = new Dictionary<AppThemes, ThemeDescriptor>
        {
            {
                AppThemes.Dark,
                new ThemeDescriptor(
                    AppThemes.Dark,
                    ThemeKind.Opaque,
                    "/LegendDesignWpf;component/Themes/DarkTheme.xaml")
            },
            {
                AppThemes.Light,
                new ThemeDescriptor(
                    AppThemes.Light,
                    ThemeKind.Opaque,
                    "/LegendDesignWpf;component/Themes/LightTheme.xaml")
            },
            {
                AppThemes.Acrylic,
                new ThemeDescriptor(
                    AppThemes.Acrylic,
                    ThemeKind.Acrylic,
                    "/LegendDesignWpf;component/Themes/AcrylicTheme.xaml")
            }
        };
    }
}
