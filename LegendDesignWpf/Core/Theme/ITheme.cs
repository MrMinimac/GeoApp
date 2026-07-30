using LegendDesignWpf.Core.Enums;
using System.Windows;

namespace LegendDesignWpf.Core.Theme
{
    public interface ITheme
    {
        AppThemes CurrentTheme { get; }
        ColorsStore ColorsStore { get; }

        event Action<AppThemes>? OnThemeChanged;
        event Action<AccentSource>? OnAccentSourceChanged;

        bool CanHotSwap(AppThemes theme);
        void ApplyTheme(AppThemes theme);
        void SetAccentSource(AccentSource accentSource);
    }
}
