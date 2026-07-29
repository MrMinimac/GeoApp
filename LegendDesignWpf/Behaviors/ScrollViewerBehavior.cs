using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LegendDesignWpf.Behaviors
{
    public static class ScrollViewerBehavior
    {
        #region DependencyProperty

        public static readonly DependencyProperty LoadMoreCommandProperty = DependencyProperty.RegisterAttached
            ("LoadMoreCommand", typeof(ICommand), typeof(ScrollViewerBehavior),new PropertyMetadata(null, OnChanged));

        public static void SetLoadMoreCommand(DependencyObject obj, ICommand value)
        => obj.SetValue(LoadMoreCommandProperty, value);

        public static ICommand GetLoadMoreCommand(DependencyObject obj)
            => (ICommand)obj.GetValue(LoadMoreCommandProperty);

        public static readonly DependencyProperty AutoScrollToEndProperty = DependencyProperty.RegisterAttached
            ("AutoScrollToEnd", typeof(bool), typeof(ScrollViewerBehavior), new PropertyMetadata(false, OnAutoScrollToEndChanged));

        private static readonly DependencyProperty IsUserLockedProperty = DependencyProperty.RegisterAttached
            ("IsUserLocked", typeof(bool), typeof(ScrollViewerBehavior), new PropertyMetadata(false));

        public static void SetAutoScrollToEnd(DependencyObject element, bool value)
            => element.SetValue(AutoScrollToEndProperty, value);

        public static bool GetAutoScrollToEnd(DependencyObject element)
            => (bool)element.GetValue(AutoScrollToEndProperty);

        #endregion

        private static void OnAutoScrollToEndChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ItemsControl itemsControl || !(bool)e.NewValue)
                return;

            itemsControl.Loaded += (_, __) =>
            {
                var scrollViewer = FindScrollViewer(itemsControl);
                if (scrollViewer == null) return;

                // Обработчик скролла: проверяем, где пользователь
                scrollViewer.ScrollChanged += (sender, args) =>
                {
                    // Если скролл изменился из-за добавления элементов (ExtentHeightChanged), не трогаем lock
                    // if (args.ExtentHeightChange != 0) return;

                    // Проверяем, внизу ли (с epsilon для флоат-ошибок)
                    const double epsilon = 0;
                    bool atBottom = scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight - epsilon;
                    scrollViewer.SetValue(IsUserLockedProperty, !atBottom);
                };

                // Если ItemsSource — ObservableCollection, подписываемся на изменения
                if (itemsControl.ItemsSource is INotifyCollectionChanged notifyCollection)
                {
                    notifyCollection.CollectionChanged += (s, collectionArgs) =>
                    {
                        if (collectionArgs.Action == NotifyCollectionChangedAction.Add &&
                            !(bool)scrollViewer.GetValue(IsUserLockedProperty))
                        {
                            scrollViewer.ScrollToEnd();
                        }
                    };
                }
            };
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
        
        private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer sv)
            {
                sv.ScrollChanged -= OnScrollChanged;
                sv.ScrollChanged += OnScrollChanged;
            }
        }
        private static void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var sv = (ScrollViewer)sender;
            if (e.ExtentHeightChange != 0) return;

            // почти внизу
            if (sv.VerticalOffset + sv.ViewportHeight >= sv.ExtentHeight - 30)
            {
                var cmd = GetLoadMoreCommand(sv);
                if (cmd?.CanExecute(null) == true)
                    cmd.Execute(null);
            }
            // почти вверху
            if (sv.VerticalOffset + sv.ViewportHeight >= sv.ExtentHeight - 30)
            {
                var cmd = GetLoadMoreCommand(sv);
                if (cmd?.CanExecute(null) == true)
                    cmd.Execute(null);
            }
        }
    }
}