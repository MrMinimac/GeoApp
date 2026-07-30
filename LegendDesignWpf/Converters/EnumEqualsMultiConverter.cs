using System.Globalization;
using System.Windows.Data;

namespace LegendDesignWpf.Converters
{
    public class EnumEqualsMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] == null || values[1] == null)
                return false;

            return values[0].Equals(values[1]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if ((bool)value)
                return new[] { Binding.DoNothing, Binding.DoNothing };

            return null!;
        }
    }

    public class EqualityMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] = Value из VM, values[1] = элемент списка
            if (values.Length < 2) return false;
            return Equals(values[0], values[1]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // если выбран, возвращаем элемент списка в Value
            if ((bool)value)
            {
                return new object[] { value, Binding.DoNothing };
            }
            return new object[] { Binding.DoNothing, Binding.DoNothing };
        }
    }
}
