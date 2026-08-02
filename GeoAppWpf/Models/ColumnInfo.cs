using System.Windows;
using System.Windows.Data;

namespace GeoAppWpf.Models
{
    public class ColumnInfo
    {
        public string PropertyName { get; init; }
        public string Header { get; init; }
        public IValueConverter? Converter { get; init; }
        public bool Visible { get; init; } = true;

        public string? ResourceTemplate { get; init; }
        public DataTemplate? CellTemplate => ResourceTemplate != null ? (DataTemplate)Application.Current.FindResource(ResourceTemplate) : null;

        public static ColumnInfo Create(string name, string header, IValueConverter? converter = null, string? cellTemplate = null)
        {
            return new ColumnInfo
            {
                PropertyName = name,
                Header = header,
                Converter = converter,
                ResourceTemplate = cellTemplate
            };
        }

        public static ColumnInfo CreateHidden(string propertyName)
        {
            return new ColumnInfo
            {
                PropertyName = propertyName,
                Visible = false
            };
        }
    }
}
