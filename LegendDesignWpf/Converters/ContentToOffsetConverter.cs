using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace LegendDesignWpf.Converters
{
    public class WidthToOffsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width)
                return -width / 2;

            return 0d;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class ContentToOffsetConverter : IValueConverter
    {
        public double PaddingCompensation { get; set; } = 0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string text || string.IsNullOrWhiteSpace(text))
                return 0d;

            string longestLine = text
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .OrderByDescending(x => x.Length)
                .FirstOrDefault() ?? string.Empty;

            var dpi = 1.0;

            if (Application.Current?.MainWindow != null)
                dpi = VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip;

            var formattedText = new FormattedText(
                longestLine,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                10, // FontSize из TextBlock
                Brushes.Black,
                dpi);

            return -(formattedText.Width / 2) + PaddingCompensation;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    
}
