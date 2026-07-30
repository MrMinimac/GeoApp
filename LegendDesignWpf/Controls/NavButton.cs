using LegendDesignWpf.Core;
using LegendDesignWpf.Core.Theme;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace LegendDesignWpf.Controls
{
    public class NavButton : RadioButton
    {
        private readonly ITheme _theme;

        private Border? _border;
        private Border? _marker;

        private SolidColorBrush _stateBrush;

        private Color _accentColor => (Color)Application.Current.Resources["AccentPaintLight"];
        private Color _inactiveColor => (Color)Application.Current.Resources["InactivePaint"];

        public NavButton()
        {
            Style = (Style)Application.Current.Resources["NavigationButtonStyle"];

            _theme = LegendDesign.Theme
                ?? throw new InvalidOperationException
                ("LegendDesignWpf not initialized. Call LegendDesign.Initialize(theme) before using LDWindow.");

            _theme.OnAccentSourceChanged += (s) => UpdateVisualState(true);
            _theme.OnThemeChanged += (s) => UpdateVisualState(true);

            _stateBrush = new SolidColorBrush(((SolidColorBrush)Foreground).Color);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _border = GetTemplateChild("border") as Border;
            _marker = GetTemplateChild("marker") as Border;

            if (Foreground is SolidColorBrush brush)
                _stateBrush = new SolidColorBrush(brush.Color);
            else
                _stateBrush = new SolidColorBrush(Colors.White);

            Foreground = _stateBrush;

            UpdateVisualState(false);
        }

        protected override void OnChecked(RoutedEventArgs e)
        {
            base.OnChecked(e);
            UpdateVisualState(true);
        }

        protected override void OnUnchecked(RoutedEventArgs e)
        {
            base.OnUnchecked(e);
            UpdateVisualState(true);
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);

            if (IsChecked ?? true)
                return;

            var animation = new DoubleAnimation
            {
                From = _border?.Opacity ?? 0,
                To = 0.5,
                Duration = TimeSpan.FromMilliseconds(200)
            };

            _border?.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            if (IsChecked ?? true)
                return;

            var animation = new DoubleAnimation
            {
                From = _border?.Opacity ?? 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200)
            };

            _border?.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        private void UpdateVisualState(bool animated)
        {
            if (_border == null || _marker == null)
                return;

            if (IsChecked == true)
            {
                if (animated)
                {
                    _border.BeginAnimation(
                        OpacityProperty,
                        new DoubleAnimation(1, TimeSpan.FromMilliseconds(200)));

                    _marker.BeginAnimation(
                        OpacityProperty,
                        new DoubleAnimation(1, TimeSpan.FromMilliseconds(200)));

                    if (Foreground is SolidColorBrush brush)
                    {
                        brush.BeginAnimation(
                        SolidColorBrush.ColorProperty,
                        new ColorAnimation
                        {
                            To = _accentColor,
                            Duration = TimeSpan.FromMilliseconds(200)
                        });
                    }
                }
                else
                {
                    Foreground = new SolidColorBrush(_accentColor);
                    _border.Opacity = 1;
                    _marker.Opacity = 1;
                }
            }
            else
            {
                if (animated)
                {
                    _border.BeginAnimation(
                        OpacityProperty,
                        new DoubleAnimation(0, TimeSpan.FromMilliseconds(200)));

                    _marker.BeginAnimation(
                        OpacityProperty,
                        new DoubleAnimation(0, TimeSpan.FromMilliseconds(200)));

                    if (Foreground is SolidColorBrush brush)
                    {
                        brush.BeginAnimation(
                        SolidColorBrush.ColorProperty,
                        new ColorAnimation
                        {
                            To = _inactiveColor,
                            Duration = TimeSpan.FromMilliseconds(200)
                        });
                    }
                }
                else
                {
                    Foreground = new SolidColorBrush(_inactiveColor);
                    _border.Opacity = 0;
                    _marker.Opacity = 0;
                }
            }
        }
    }
}
