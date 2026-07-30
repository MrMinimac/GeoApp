using System.Windows;
using System.Windows.Controls;

namespace LegendDesignWpf.Controls
{
    public class AspectRatioContainer : Decorator
    {
        public double Ratio { get; set; } = 16.0 / 9.0;

        protected override Size MeasureOverride(Size constraint)
        {
            double width = constraint.Width;
            double height = width / Ratio;

            Child?.Measure(new Size(width, height));
            return new Size(width, height);
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            double height = arrangeSize.Width / Ratio;
            Child?.Arrange(new Rect(0, 0, arrangeSize.Width, height));
            return new Size(arrangeSize.Width, height);
        }
    }
}
