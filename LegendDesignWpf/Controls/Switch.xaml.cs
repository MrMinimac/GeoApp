using LegendDesignWpf.Core;
using LegendDesignWpf.Core.Theme;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace LegendDesignWpf.Controls
{
    public partial class Switch : UserControl
    {
        private readonly ITheme _theme;

        public Switch()
        {
            InitializeComponent();

            _theme = LegendDesign.Theme
                ?? throw new InvalidOperationException
                ("LegendDesignWpf not initialized. Call LegendDesign.Initialize(theme) before using LDWindow.");

            _theme.OnAccentSourceChanged += (s) => UpdateVisual(true);
            _theme.OnThemeChanged += (s) => UpdateVisual(true);

            Thumb.Background = new SolidColorBrush(Colors.Transparent);
            Body.BorderBrush = new SolidColorBrush(Colors.Transparent);

            UpdateVisual(false);
        }

        #region IsCheckedProperty
        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(Switch), 
                new FrameworkPropertyMetadata(false,FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,OnIsCheckedChanged));

        public bool IsChecked
        {
            get => (bool)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }
        #endregion

        #region ActiveBrush
        public static readonly DependencyProperty ActiveBrushProperty =
            DependencyProperty.Register(nameof(ActiveBrush), typeof(Brush),
                typeof(Switch), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(255, 255, 255)), OnBrushChanged));

        public Brush ActiveBrush
        {
            get => (Brush)GetValue(ActiveBrushProperty);
            set => SetValue(ActiveBrushProperty, value);
        }
        #endregion

        #region InactiveBrush
        public static readonly DependencyProperty InactiveBrushProperty =
            DependencyProperty.Register(nameof(InactiveBrush), typeof(Brush),
                typeof(Switch), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(44, 44, 44)), OnBrushChanged));

        public Brush InactiveBrush
        {
            get => (Brush)GetValue(InactiveBrushProperty);
            set => SetValue(InactiveBrushProperty, value);
        }
        #endregion

        private static void OnBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Switch)d).UpdateVisual(false);
        }

        private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (Switch)d;
            control.UpdateVisual(true);
        }

        private void OnToggle(object sender, MouseButtonEventArgs e)
        {
            IsChecked = !IsChecked;
        }

        private void UpdateVisual(bool animate)
        {
            var thumbTarget = IsChecked ? 17.0 : 3.0;

            var targetBrush = IsChecked ? ActiveBrush : InactiveBrush;
            var targetColor = GetColor(targetBrush);

            if (!animate)
            {
                Canvas.SetLeft(Thumb, thumbTarget);

                ((SolidColorBrush)Thumb.Background).Color = targetColor;
                ((SolidColorBrush)Body.BorderBrush).Color = targetColor;
                return;
            }

            var duration = TimeSpan.FromMilliseconds(180);

            // Позиция
            Thumb.BeginAnimation(Canvas.LeftProperty,
                new DoubleAnimation(thumbTarget, duration)
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                });

            // Цвет thumb
            ((SolidColorBrush)Thumb.Background).BeginAnimation(
                SolidColorBrush.ColorProperty,
                new ColorAnimation(targetColor, duration));

            // Цвет рамки
            ((SolidColorBrush)Body.BorderBrush).BeginAnimation(
                SolidColorBrush.ColorProperty,
                new ColorAnimation(targetColor, duration));
        }

        private static Color GetColor(Brush brush)
        {
            if (brush is SolidColorBrush scb)
                return scb.Color;

            throw new InvalidOperationException("Brush должен быть SolidColorBrush");
        }
    }
}
