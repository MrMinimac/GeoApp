using System.Globalization;
using System.Windows.Data;

namespace GeoAppWpf.Converters
{
    public class ValueConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            double d = (double)value;

            return d switch
            {
                0 => "пс",
                -1 => "зн",
                _ => d.ToString()
            };
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
