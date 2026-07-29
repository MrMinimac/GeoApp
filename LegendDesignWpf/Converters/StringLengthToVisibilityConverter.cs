using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LegendDesignWpf.Converters
{
    public class StringLengthToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVisible = false;

            if (value is string s)
                isVisible = s.Length > 0;

            // Проверяем параметр (если он равен "Reversed" — меняем логику)
            if (parameter?.ToString()?.Equals("Reversed", StringComparison.OrdinalIgnoreCase) == true)
                isVisible = !isVisible;

            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
