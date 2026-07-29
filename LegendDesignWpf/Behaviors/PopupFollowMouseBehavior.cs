using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace LegendDesignWpf.Behaviors
{
    public static class PopupFollowMouseBehavior
    {
        public static readonly DependencyProperty EnabledProperty =
            DependencyProperty.RegisterAttached(
                "Enabled",
                typeof(bool),
                typeof(PopupFollowMouseBehavior),
                new PropertyMetadata(false, OnChanged));

        private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Popup popup) return;

            if ((bool)e.NewValue)
            {
                popup.MouseMove += (_, __) =>
                {
                    var pos = Mouse.GetPosition(popup.PlacementTarget);
                    popup.HorizontalOffset = pos.X - 20;
                };
            }
        }

        public static void SetEnabled(Popup popup, bool value) =>
            popup.SetValue(EnabledProperty, value);
    }
}
