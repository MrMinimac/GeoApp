using System.Globalization;
using System.Windows.Data;

namespace LegendDesignWpf.Converters
{
    public class IntToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVisible = false;

            if (value is int i)
                isVisible = i > 0;

            // Проверяем параметр (если он равен "Reversed" — меняем логику)
            if (parameter?.ToString()?.Equals("Reversed", StringComparison.OrdinalIgnoreCase) == true)
                isVisible = !isVisible;

            return isVisible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) 
            => throw new NotImplementedException();
    }
}
