using LegendDesignWpf.Core;
using LegendDesignWpf.Core.Theme;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace LegendDesignWpf.Controls
{
    public class FlatButton : Button
    {
        private readonly ITheme _theme;
        private Border? _border;

        private SolidColorBrush _stateBrush = new SolidColorBrush(Colors.White);
        private Color _backgorundColor
        {
            get
            {
                if (Background is SolidColorBrush brush)
                {
                    var baseColor = brush.Color;

                    if (baseColor.A == 0)
                    {
                        return WithAlpha(_backgorundHoverColor, 0);
                    }
                    else
                    {
                        return baseColor;
                    }
                }
                else
                {
                    return WithAlpha(_backgorundHoverColor, 0);
                }
            }
        }

        private Color _backgorundHoverColor
        {
            get
            {
                if (HoverBrush is SolidColorBrush hoverBrush)
                    return hoverBrush.Color;

                return Colors.Gray;
            }
        }

        static FlatButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FlatButton), new FrameworkPropertyMetadata(typeof(FlatButton)));
        }

        public FlatButton()
        {
            _theme = LegendDesign.Theme
                ?? throw new InvalidOperationException
                ("LegendDesignWpf not initialized. Call LegendDesign.Initialize(theme) before using LDWindow.");

            _theme.OnAccentSourceChanged += (s) =>
            {
                UpdateVisualState(true);
            };

            _theme.OnThemeChanged += (s) =>
            {
                UpdateVisualState(true);
            };
        }

        #region ToolTipText
        public static readonly DependencyProperty ToolTipTextProperty =
            DependencyProperty.Register(
                nameof(ToolTipText),
                typeof(string),
                typeof(FlatButton),
                new PropertyMetadata(string.Empty));

        public string ToolTipText
        {
            get => (string)GetValue(ToolTipTextProperty);
            set => SetValue(ToolTipTextProperty, value);
        }
        #endregion

        #region ToolTipFontSize
        public static readonly DependencyProperty ToolTipFontSizeProperty =
            DependencyProperty.Register(
                nameof(ToolTipFontSize),
                typeof(double),
                typeof(FlatButton),
                new PropertyMetadata(10.0));

        public double ToolTipFontSize
        {
            get => (double)GetValue(ToolTipFontSizeProperty);
            set => SetValue(ToolTipFontSizeProperty, value);
        }
        #endregion

        #region ToolTipMaxWidth
        public static readonly DependencyProperty ToolTipMaxWidthProperty =
            DependencyProperty.Register(
                nameof(ToolTipMaxWidth),
                typeof(double),
                typeof(FlatButton),
                new PropertyMetadata(100.0));

        public double ToolTipMaxWidth
        {
            get => (double)GetValue(ToolTipMaxWidthProperty);
            set => SetValue(ToolTipMaxWidthProperty, value);
        }
        #endregion

        #region ToolTipCornerRadius
        public static readonly DependencyProperty ToolTipCornerRadiusProperty =
            DependencyProperty.Register(
                nameof(ToolTipCornerRadius),
                typeof(CornerRadius),
                typeof(FlatButton),
                new PropertyMetadata(new CornerRadius(8)));

        public CornerRadius ToolTipCornerRadius
        {
            get => (CornerRadius)GetValue(ToolTipCornerRadiusProperty);
            set => SetValue(ToolTipCornerRadiusProperty, value);
        }
        #endregion

        #region ToolTipBackground
        public static readonly DependencyProperty ToolTipBackgroundProperty =
            DependencyProperty.Register(
                nameof(ToolTipBackground),
                typeof(Brush),
                typeof(FlatButton),
                new PropertyMetadata(Brushes.Transparent));

        public Brush ToolTipBackground
        {
            get => (Brush)GetValue(ToolTipBackgroundProperty);
            set => SetValue(ToolTipBackgroundProperty, value);
        }
        #endregion

        #region ToolTipForeground
        public static readonly DependencyProperty ToolTipForegroundProperty =
            DependencyProperty.Register(
                nameof(ToolTipForeground),
                typeof(Brush),
                typeof(FlatButton),
                new PropertyMetadata(Brushes.White));

        public Brush ToolTipForeground
        {
            get => (Brush)GetValue(ToolTipForegroundProperty);
            set => SetValue(ToolTipForegroundProperty, value);
        }
        #endregion

        #region ToolTipPadding
        public static readonly DependencyProperty ToolTipPaddingProperty =
            DependencyProperty.Register(
                nameof(ToolTipPadding),
                typeof(Thickness),
                typeof(FlatButton),
                new PropertyMetadata(new Thickness(10, 2, 10, 2)));

        public Thickness ToolTipPadding
        {
            get => (Thickness)GetValue(ToolTipPaddingProperty);
            set => SetValue(ToolTipPaddingProperty, value);
        }
        #endregion

        #region CornerRadius
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(FlatButton),
                new PropertyMetadata(new CornerRadius(0)));

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }
        #endregion

        #region HoverBrush
        public static readonly DependencyProperty HoverBrushProperty =
            DependencyProperty.Register(
                nameof(HoverBrush),
                typeof(Brush),
                typeof(FlatButton),
                new PropertyMetadata(Brushes.Gray));

        public Brush HoverBrush
        {
            get => (Brush)GetValue(HoverBrushProperty);
            set => SetValue(HoverBrushProperty, value);
        }
        #endregion

        #region ContentHorizontalAlignment
        public static readonly DependencyProperty ContentHorizontalAlignmentProperty =
            DependencyProperty.Register(
                nameof(ContentHorizontalAlignment),
                typeof(HorizontalAlignment),
                typeof(FlatButton),
                new PropertyMetadata(HorizontalAlignment.Center));

        public HorizontalAlignment ContentHorizontalAlignment
        {
            get => (HorizontalAlignment)GetValue(ContentHorizontalAlignmentProperty);
            set => SetValue(ContentHorizontalAlignmentProperty, value);
        }
        #endregion

        #region ContentVerticalAlignment
        public static readonly DependencyProperty ContentVerticalAlignmentProperty =
            DependencyProperty.Register(
                nameof(ContentVerticalAlignment),
                typeof(VerticalAlignment),
                typeof(FlatButton),
                new PropertyMetadata(VerticalAlignment.Center));

        public VerticalAlignment ContentVerticalAlignment
        {
            get => (VerticalAlignment)GetValue(ContentVerticalAlignmentProperty);
            set => SetValue(ContentVerticalAlignmentProperty, value);
        }
        #endregion

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _border = GetTemplateChild("border") as Border;

            if (Background is SolidColorBrush brush)
                _stateBrush = new SolidColorBrush(brush.Color);
            else
                _stateBrush = new SolidColorBrush(Colors.White);

            if (_border != null)
            {
                _border.Background = _stateBrush;
            }

            if (ToolTip is ToolTip tooltip)
            {
                tooltip.Opened += OnToolTipOpened;
            }

            UpdateVisualState(false);
        }

        private void OnToolTipOpened(object sender, RoutedEventArgs e)
        {
            if (sender is not ToolTip tooltip)
                return;

            tooltip.UpdateLayout();

            tooltip.HorizontalOffset =
                (ActualWidth - tooltip.ActualWidth) / 2;
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            UpdateVisualState(true);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            UpdateVisualState(true);
        }

        private void UpdateVisualState(bool animated)
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
                            To = _backgorundHoverColor,
                            Duration = TimeSpan.FromMilliseconds(200)
                        });
                    }
                }
                else
                {
                    _border.Background = new SolidColorBrush(_backgorundHoverColor);
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
                            To = _backgorundColor,
                            Duration = TimeSpan.FromMilliseconds(200)
                        });
                    }
                }
                else
                {
                    _border.Background = new SolidColorBrush(_backgorundColor);
                }
            }
        }
        private static Color WithAlpha(Color color, byte alpha)
        {
            return Color.FromArgb(alpha, color.R, color.G, color.B);
        }
    }
}
