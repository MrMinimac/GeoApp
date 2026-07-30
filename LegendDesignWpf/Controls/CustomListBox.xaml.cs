using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LegendDesignWpf.Controls
{
    public partial class CustomListBox : UserControl
    {
        private bool _isUserLocked;
        private ScrollViewer? _scrollViewer;

        public CustomListBox()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _scrollViewer = FindScrollViewer(PART_ItemsControl);
            _scrollViewer.ScrollChanged += _scrollViewer_ScrollChanged;

            if (PART_ItemsControl.ItemsSource is INotifyCollectionChanged nc)
            {
                nc.CollectionChanged += OnCollectionChanged;
            }
        }

        public void ScrollToEnd()
        {
            _scrollViewer?.ScrollToEnd();
        }

        public void ScrollToHome()
        {
            _scrollViewer?.ScrollToHome();
        }

        public void ScrollToHorizontalOffset(double offset)
        {
            _scrollViewer?.ScrollToHorizontalOffset(offset);
        }

        #region ItemsSourceProperty
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(CustomListBox));

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }
        #endregion

        #region ItemTemplateProperty
        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(CustomListBox));

        public DataTemplate ItemTemplate
        {
            get => (DataTemplate)GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }
        #endregion

        #region IsAutoScrollProperty
        public static readonly DependencyProperty IsAutoScrollProperty =
            DependencyProperty.Register(nameof(IsAutoScroll), typeof(bool), typeof(CustomListBox));

        public bool IsAutoScroll
        {
            get => (bool)GetValue(IsAutoScrollProperty);
            set => SetValue(IsAutoScrollProperty, value);
        }
        #endregion

        #region ContentMaxWidthProperty
        public static readonly DependencyProperty ContentMaxWidthProperty =
            DependencyProperty.Register(
                nameof(ContentMaxWidth),
                typeof(double),
                typeof(CustomListBox),
                new FrameworkPropertyMetadata(double.PositiveInfinity));

        public double ContentMaxWidth
        {
            get => (double)GetValue(ContentMaxWidthProperty);
            set => SetValue(ContentMaxWidthProperty, value);
        }
        #endregion

        #region ContentMarginProperty
        public static readonly DependencyProperty ContentMarginProperty =
            DependencyProperty.Register(
                nameof(ContentMargin), 
                typeof(Thickness), 
                typeof(CustomListBox),
                new FrameworkPropertyMetadata(new Thickness(0)));

        public Thickness ContentMargin
        {
            get => (Thickness)GetValue(ContentMarginProperty);
            set => SetValue(ContentMarginProperty, value);
        }
        #endregion

        private void _scrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!IsAutoScroll)
                return;

            if (sender is not ScrollViewer sw)
                return;

            // Если скролл изменился из-за добавления элементов (ExtentHeightChanged), не трогаем lock
            if (e.ExtentHeightChange != 0) return;

            // Проверяем, внизу ли (с epsilon для флоат-ошибок)
            const double epsilon = 0;
            bool atBottom = sw.VerticalOffset >= sw.ScrollableHeight - epsilon;
            _isUserLocked = !atBottom;
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (!IsAutoScroll)
                return;

            if (e.Action == NotifyCollectionChangedAction.Add && !_isUserLocked)
                _scrollViewer?.ScrollToEnd();
        }

        private static ScrollViewer FindScrollViewer(DependencyObject d)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++)
            {
                var child = VisualTreeHelper.GetChild(d, i);

                if (child is ScrollViewer sv)
                    return sv;

                var result = FindScrollViewer(child);

                if (result != null)
                    return result;
            }
            return null;
        }
    }
}
