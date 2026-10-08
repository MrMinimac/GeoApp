using GeoUIWpf.Views.Windows;
using System.Globalization;
using System.Windows.Data;

namespace GeoUIWpf.Converters
{
    public class SelectionItemDisplayConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[1] is SelectionPropertyItem propertyItem)
            {
                return propertyItem.FormatItem(values[0]);
            }

            return values[0]?.ToString() ?? string.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
