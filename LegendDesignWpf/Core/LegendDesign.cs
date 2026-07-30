using LegendDesignWpf.Core.Theme;

namespace LegendDesignWpf.Core
{
    public static class LegendDesign
    {
        private static ITheme _theme;
        public static ITheme Theme => _theme;

        static LegendDesign()
        {
            _theme = new ThemeManager();
        }
    }
}
