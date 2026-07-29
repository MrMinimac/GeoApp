using LegendDesignWpf.Core.Enums;

namespace GeoAppWpf.Models
{
    [Serializable]
    public sealed class SettingsModel
    {
        public AppThemes AppTheme { get; set; } = 0;
        public AccentSource AccentSource { get; set; } = AccentSource.Windows;
        public string LastDocPath { get; set; } = "";
    }
}
