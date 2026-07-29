using System.Globalization;
using System.Windows.Data;

namespace LegendDesignWpf.Converters
{
    public class SecondsToTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                var ts = TimeSpan.FromSeconds(d);
                return ts.ToString(@"mm\:ss");
            }

            if (value is int i)
            {
                var ts = TimeSpan.FromSeconds(i);
                return ts.ToString(@"mm\:ss");
            }

            return "--:--";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
