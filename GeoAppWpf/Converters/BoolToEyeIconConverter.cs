using LegendIcons.Core;
using System.Globalization;
using System.Windows.Data;

namespace GeoAppWpf.Converters
{
    public class BoolToEyeIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? PackIconKind.OpenedEyeIcon : PackIconKind.ClosedEyeIcon;
            }

            return PackIconKind.Close;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
