using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace LegendDesignWpf.Behaviors
{
    public static class SliderPopupFollowMouseBehavior
    {
        public static readonly DependencyProperty EnabledProperty =
            DependencyProperty.RegisterAttached(
                "Enabled",
                typeof(bool),
                typeof(SliderPopupFollowMouseBehavior),
                new PropertyMetadata(false, OnChanged));

        public static void SetEnabled(DependencyObject d, bool value) =>
            d.SetValue(EnabledProperty, value);

        private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Slider slider || !(bool)e.NewValue)
                return;

            slider.MouseMove += (_, __) =>
            {
                if (slider.Template?.FindName("HoverToolTip", slider) is not Popup popup)
                    return;

                if (!popup.IsOpen)
                    return;

                var pos = Mouse.GetPosition(slider);

                // 🔒 защита
                var width = slider.ActualWidth;
                if (width <= 0)
                    return;

                var percent = Math.Clamp(pos.X / width, 0, 1);

                var hoverValue =
                    slider.Minimum +
                    percent * (slider.Maximum - slider.Minimum);

                // 1️⃣ двигаем tooltip
                popup.HorizontalOffset = percent * width - 20;

                // 2️⃣ сохраняем hover value
                SliderHoverTimeBehavior.SetHoverValue(slider, hoverValue);
            };
        }
    }

}
