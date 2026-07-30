using System.Windows;
using System.Windows.Controls;

namespace LegendDesignWpf.Controls
{
    public class CardButton : Button
    {
        static CardButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(CardButton),
                new FrameworkPropertyMetadata(typeof(CardButton)));
        }

        #region IsLoading

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(
                nameof(IsLoading),
                typeof(bool),
                typeof(CardButton),
                new PropertyMetadata(false));

        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }
        #endregion

        #region CornerRadius
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(CardButton),
                new PropertyMetadata(new CornerRadius(0)));

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }
        #endregion
    }
}
