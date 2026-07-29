using LegendDesignWpf.Core.Enums;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace LegendDesignWpf.Controls
{
    public partial class CustomPage : UserControl
    {
        public CustomPage()
        {
            InitializeComponent();
        }

        #region TitleProperty
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(CustomPage),
                new PropertyMetadata(string.Empty));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
        #endregion

        #region SubtitleProperty
        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register(
                nameof(Subtitle),
                typeof(string),
                typeof(CustomPage),
                new PropertyMetadata(string.Empty));

        public string Subtitle
        {
            get => (string)GetValue(SubtitleProperty);
            set => SetValue(SubtitleProperty, value);
        }
        #endregion

        #region SubtitleForegroundProperty
        public static readonly DependencyProperty SubtitleForegroundProperty =
            DependencyProperty.Register(nameof(SubtitleForeground), typeof(Brush),
                typeof(CustomPage), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(200, 200, 200))));

        public Brush SubtitleForeground
        {
            get => (Brush)GetValue(SubtitleForegroundProperty);
            set => SetValue(SubtitleForegroundProperty, value);
        }
        #endregion

        #region PageContentProperty
        public static readonly DependencyProperty PageContentProperty =
            DependencyProperty.Register(
                nameof(PageContent),
                typeof(object),
                typeof(CustomPage),
                new PropertyMetadata(null));

        public object PageContent
        {
            get => GetValue(PageContentProperty);
            set => SetValue(PageContentProperty, value);
        }
        #endregion

        #region HeaderMaxWidthProperty
        public static readonly DependencyProperty HeaderMaxWidthProperty =
            DependencyProperty.Register(
                nameof(HeaderMaxWidth),
                typeof(double),
                typeof(CustomPage),
                new PropertyMetadata(1000.0));

        public double HeaderMaxWidth
        {
            get => (double)GetValue(HeaderMaxWidthProperty);
            set => SetValue(HeaderMaxWidthProperty, value);
        }
        #endregion
        #region HeaderContentProperty
        public static readonly DependencyProperty HeaderContentProperty =
            DependencyProperty.Register(
                nameof(HeaderContent),
                typeof(object),
                typeof(CustomPage),
                new PropertyMetadata(null));

        public object HeaderContent
        {
            get => GetValue(HeaderContentProperty);
            set => SetValue(HeaderContentProperty, value);
        }
        #endregion

        #region PlaceholderProperty
        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(
                nameof(Placeholder),
                typeof(string),
                typeof(CustomPage),
                new PropertyMetadata(string.Empty));

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }
        #endregion

        #region PlaceholderIconProperty
        public static readonly DependencyProperty PlaceholderIconProperty =
            DependencyProperty.Register(
                nameof(PlaceholderIcon),
                typeof(PackIconKind),
                typeof(CustomPage),
                new PropertyMetadata(PackIconKind.Close));

        public PackIconKind PlaceholderIcon
        {
            get => (PackIconKind)GetValue(PlaceholderIconProperty);
            set => SetValue(PlaceholderIconProperty, value);
        }
        #endregion

        #region PlaceholderVisibilityProperty
        public static readonly DependencyProperty PlaceholderVisibilityProperty =
            DependencyProperty.Register(
                nameof(PlaceholderVisibility),
                typeof(Visibility),
                typeof(CustomPage),
                new PropertyMetadata(Visibility.Collapsed));

        public Visibility PlaceholderVisibility
        {
            get => (Visibility)GetValue(PlaceholderVisibilityProperty);
            set => SetValue(PlaceholderVisibilityProperty, value);
        }
        #endregion

    }
}
