using System.Globalization;
using System.Windows.Data;

namespace LegendDesignWpf.Converters
{
    public class ContentToWidthConverter : IValueConverter
    {
        public double CharWidth { get; set; } = 6;
        public double Padding { get; set; } = 10;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value as string;
            if (string.IsNullOrEmpty(text))
                return 0d;

            return text.Length * CharWidth + Padding;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
