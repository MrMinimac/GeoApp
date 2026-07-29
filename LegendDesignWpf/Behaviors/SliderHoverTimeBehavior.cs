using System.Windows;

namespace LegendDesignWpf.Behaviors
{
    public static class SliderHoverTimeBehavior
    {
        public static readonly DependencyProperty HoverValueProperty =
            DependencyProperty.RegisterAttached(
                "HoverValue",
                typeof(double),
                typeof(SliderHoverTimeBehavior),
                new FrameworkPropertyMetadata(0d));

        public static void SetHoverValue(DependencyObject d, double value) =>
            d.SetValue(HoverValueProperty, value);

        public static double GetHoverValue(DependencyObject d) =>
            (double)d.GetValue(HoverValueProperty);
    }

}
