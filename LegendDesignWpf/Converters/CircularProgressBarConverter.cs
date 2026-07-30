using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LegendDesignWpf.Converters
{
    public class StartPointConverter : IValueConverter
    {
        public static readonly StartPointConverter Instance = new();

        [Obsolete]
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double v && (v > 0.0))
            {
                return new Point(v / 2, 0);
            }

            return new Point();
        }

        [Obsolete]
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;

    }

    public class RotateTransformCentreConverter : IValueConverter
    {
        public static readonly RotateTransformCentreConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            (double)value / 2; //value == actual width

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }

    public class ArcSizeConverter : IValueConverter
    {
        public static readonly ArcSizeConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is double v && (v > 0.0) ? new Size(v / 2, v / 2) : new Point();

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }

    public class ArcEndPointConverter : IMultiValueConverter
    {
        public static readonly ArcEndPointConverter Instance = new();
        
        public const string ParameterMidPoint = "MidPoint";

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double actualWidth = values[0].ExtractDouble();
            double value = values[1].ExtractDouble();
            double minimum = values[2].ExtractDouble();
            double maximum = values[3].ExtractDouble();

            if (new[] { actualWidth, value, minimum, maximum }.AnyNan())
                return Binding.DoNothing;

            if (values.Length == 5)
            {
                double fullIndeterminateScaling = values[4].ExtractDouble();
                if (!double.IsNaN(fullIndeterminateScaling) && fullIndeterminateScaling > 0.0)
                {
                    value = (maximum - minimum) * fullIndeterminateScaling;
                }
            }

            double percent = maximum <= minimum ? 1.0 : (value - minimum) / (maximum - minimum);
            if (Equals(parameter, ParameterMidPoint))
                percent /= 2;

            double degrees = 360 * percent;
            double radians = degrees * (Math.PI / 180);

            var centre = new Point(actualWidth / 2, actualWidth / 2);
            double hypotenuseRadius = (actualWidth / 2);

            double adjacent = Math.Cos(radians) * hypotenuseRadius;
            double opposite = Math.Sin(radians) * hypotenuseRadius;

            return new Point(centre.X + opposite, centre.Y - adjacent);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class NotZeroConverter : IValueConverter
    {
        public static readonly NotZeroConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (double.TryParse((value ?? "").ToString(), out double val))
            {
                return Math.Abs(val) > 0.0;
            }
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    internal static class LocalEx
    {
        public static double ExtractDouble(this object value)
        {
            double d = value as double? ?? double.NaN;
            return double.IsInfinity(d) ? double.NaN : d;
        }


        public static bool AnyNan(this IEnumerable<double> values) => values.Any(double.IsNaN);
    }
}
