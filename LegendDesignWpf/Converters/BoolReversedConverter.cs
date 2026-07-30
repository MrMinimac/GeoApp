using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LegendDesignWpf.Converters
{
    public class BoolReversedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = value as bool? ?? false;
            return !isChecked;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
