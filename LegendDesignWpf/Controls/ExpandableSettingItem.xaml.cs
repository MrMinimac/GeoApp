using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace LegendDesignWpf.Controls
{
    public partial class ExpandableSettingItem : UserControl
    {
        private bool _isExpanded;

        public ExpandableSettingItem()
        {
            InitializeComponent();
        }

        public UIElement ExpandedContent
        {
            get => (UIElement)ContentPresenter.Content;
            set => ContentPresenter.Content = value;
        }

        #region TitleProperty
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(ExpandableSettingItem));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
        #endregion

        #region DescriptionProperty
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(ExpandableSettingItem));

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }
        #endregion

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject source)
            {
                // Если клик внутри ContentPresenter — игнорируем
                if (IsDescendantOf(source, ContentPresenter))
                    return;
            }

            _isExpanded = !_isExpanded;

            AnimateContent();
            AnimateArrow();
        }

        private static bool IsDescendantOf(DependencyObject source, DependencyObject parent)
        {
            while (source != null)
            {
                if (ReferenceEquals(source, parent))
                    return true;

                source = VisualTreeHelper.GetParent(source);
            }

            return false;
        }

        private void AnimateContent()
        {
            double from = _isExpanded ? 0 : ContentHost.ActualHeight;
            double to = 0;

            if (_isExpanded)
            {
                // ❗ Измеряем КОНТЕНТ, а не Host
                ContentPresenter.Measure(
                    new Size(ContentHost.ActualWidth, double.PositiveInfinity));

                to = ContentPresenter.DesiredSize.Height;
            }

            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new CubicEase
                {
                    EasingMode = EasingMode.EaseInOut
                }
            };

            // ❗ После раскрытия возвращаем Height = Auto
            animation.Completed += (sender, args) =>
            {
                if (_isExpanded)
                    ContentHost.Height = double.NaN; // Auto
            };

            ContentHost.BeginAnimation(HeightProperty, animation);
        }

        private void AnimateArrow()
        {
            var animation = new DoubleAnimation
            {
                To = _isExpanded ? 180 : 0,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            ArrowRotate.BeginAnimation(RotateTransform.AngleProperty, animation);
        }
    }
}
