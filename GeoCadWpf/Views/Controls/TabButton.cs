using LegendDesignWpf.Core;
using LegendDesignWpf.Core.Theme;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace GeoCadWpf.Views.Controls
{
    public class TabButton : RadioButton
    {

        #region Fields

        private readonly ITheme _theme;

        private Border? _border;

        private SolidColorBrush? _borderBrush;
        private SolidColorBrush? _backroundBrush;

        private Color _accentColor => (Color)Application.Current.Resources["AccentPaintLight"];
        private Color _inactiveColor => (Color)Application.Current.Resources["InactivePaint"];
        private Color _secondaryColor => (Color)Application.Current.Resources["SecondaryPaint"];
        private Color _hoverColor => (Color)Application.Current.Resources["HoverPaint"];

        #endregion

        public TabButton()
        {
            Style = (Style)Application.Current.Resources["TabButtonStyle"];

            _theme = LegendDesign.Theme
                ?? throw new InvalidOperationException
                ("LegendDesignWpf not initialized. Call LegendDesign.Initialize(theme) before using LDWindow.");

            _theme.OnAccentSourceChanged += (s) => UpdateBorderBrushVisualState(true);
            _theme.OnThemeChanged += (s) => UpdateBorderBrushVisualState(true);

        }

        #region Events

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _border = GetTemplateChild("border") as Border;

            if (_border == null)
                return;

            _borderBrush = _border.BorderBrush is SolidColorBrush borderBrush
                ? new SolidColorBrush(borderBrush.Color)
                : new SolidColorBrush(Colors.Transparent);

            _backroundBrush = _border.Background is SolidColorBrush backgroundBrush
                ? new SolidColorBrush(backgroundBrush.Color)
                : new SolidColorBrush(Colors.Transparent);

            _border.BorderBrush = _borderBrush;
            _border.Background = _backroundBrush;

            UpdateBorderBrushVisualState(false);
            UpdateBorderBackgroundVisualState(false);
        }

        protected override void OnChecked(RoutedEventArgs e)
        {
            base.OnChecked(e);
            UpdateBorderBrushVisualState(true);

            if (!Equals(Data, CurrentData))
                CurrentData = Data;
        }

        protected override void OnUnchecked(RoutedEventArgs e)
        {
            base.OnUnchecked(e);
            UpdateBorderBrushVisualState(true);
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);

            UpdateBorderBackgroundVisualState(true);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            UpdateBorderBackgroundVisualState(true);
        }

        private void UpdateBorderBackgroundVisualState(bool animated)
        {
            if (_border == null)
                return;

            if (IsMouseOver == true)
            {
                if (animated)
                {
                    if (_border.Background is SolidColorBrush brush)
                    {
                        brush.BeginAnimation(
                            SolidColorBrush.ColorProperty,
                            new ColorAnimation
                            {
                                To = _hoverColor,
                                Duration = TimeSpan.FromMilliseconds(200)
                            });
                    }
                }
                else
                {
                    _border.Background = new SolidColorBrush(_hoverColor);
                }
            }
            else
            {
                if (animated)
                {
                    if (_border.Background is SolidColorBrush brush)
                    {
                        brush.BeginAnimation(
                        SolidColorBrush.ColorProperty,
                        new ColorAnimation
                        {
                            To = _secondaryColor,
                            Duration = TimeSpan.FromMilliseconds(200)
                        });
                    }
                }
                else
                {
                    _border.Background = new SolidColorBrush(_secondaryColor);
                }
            }
        }

        private void UpdateBorderBrushVisualState(bool animated)
        {
            if (_border == null)
                return;

            if (IsChecked == true)
            {
                if (animated)
                {
                    if (_border.BorderBrush is SolidColorBrush brush)
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
                    _border.BorderBrush = new SolidColorBrush(_accentColor);
                }
            }
            else
            {
                if (animated)
                {
                    if (_border.BorderBrush is SolidColorBrush brush)
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
                    _border.BorderBrush = new SolidColorBrush(_inactiveColor);
                }
            }
        }

        #endregion

        #region Dependency Properties

        public object? Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(object),
                typeof(TabButton));


        public object? CurrentData
        {
            get => GetValue(CurrentDataProperty);
            set => SetValue(CurrentDataProperty, value);
        }

        public static readonly DependencyProperty CurrentDataProperty =
            DependencyProperty.Register(
                nameof(CurrentData),
                typeof(object),
                typeof(TabButton),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == CurrentDataProperty ||
                e.Property == DataProperty)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Data: {Data}, CurrentData: {CurrentData}, Equal: {Equals(Data, CurrentData)}");

                SetCurrentValue(
                    IsCheckedProperty,
                    Equals(Data, CurrentData));
            }
        }

        public static readonly DependencyProperty CloseCommandProperty =
            DependencyProperty.Register(
                nameof(CloseCommand),
                typeof(ICommand),
                typeof(TabButton));

        public ICommand? CloseCommand
        {
            get => (ICommand?)GetValue(CloseCommandProperty);
            set => SetValue(CloseCommandProperty, value);
        }

        #endregion
    }
}
