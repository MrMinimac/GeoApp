using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace LegendDesignWpf.Controls
{
    public class FlatToggleButton : ToggleButton
    {
        static FlatToggleButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FlatToggleButton), new FrameworkPropertyMetadata(typeof(FlatToggleButton)));
        }

        #region CornerRadiusProperty
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(FlatToggleButton),
                new PropertyMetadata(new CornerRadius(0)));

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }
        #endregion

        #region HoverBrushProperty
        public static readonly DependencyProperty HoverBrushProperty =
            DependencyProperty.Register(
                nameof(HoverBrush),
                typeof(Brush),
                typeof(FlatToggleButton),
                new PropertyMetadata(Brushes.Gray));

        public Brush HoverBrush
        {
            get => (Brush)GetValue(HoverBrushProperty);
            set => SetValue(HoverBrushProperty, value);
        }
        #endregion

        #region HoverOpacityProperty
        public static readonly DependencyProperty HoverOpacityProperty =
            DependencyProperty.Register(
                nameof(HoverOpacity),
                typeof(float),
                typeof(FlatToggleButton),
                new PropertyMetadata(1f));

        public float HoverOpacity
        {
            get => (float)GetValue(HoverOpacityProperty);
            set => SetValue(HoverOpacityProperty, value);
        }
        #endregion
    }
}
