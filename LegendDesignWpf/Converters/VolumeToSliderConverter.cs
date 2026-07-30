using System.Globalization;
using System.Windows.Data;

namespace LegendDesignWpf.Converters
{
    public sealed class VolumeToSliderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is float v)
                return v * 100.0;

            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
                return (float)(d / 100.0);

            return 0f;
        }
    }
    public sealed class VolumeToPercentsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is float v)
                return (int)(v * 100);

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
