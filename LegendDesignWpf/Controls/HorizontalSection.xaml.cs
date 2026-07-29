using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LegendDesignWpf.Controls
{
    public partial class HorizontalSection : UserControl
    {
        public HorizontalSection()
        {
            InitializeComponent();
        }

        #region Title
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(HorizontalSection), new PropertyMetadata(string.Empty));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
        #endregion

        #region IsVisible
        public static new readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.Register(nameof(IsVisible), typeof(bool), typeof(HorizontalSection), new PropertyMetadata(true));
        
        public new bool IsVisible
        {
            get => (bool)GetValue(IsVisibleProperty);
            set => SetValue(IsVisibleProperty, value);
        }
        #endregion

        #region IsPlayPauseVisible
        public static readonly DependencyProperty IsPlayPauseVisibleProperty =
            DependencyProperty.Register(nameof(IsPlayPauseVisible), typeof(bool), typeof(HorizontalSection), new PropertyMetadata(false));
        
        public bool IsPlayPauseVisible
        {
            get => (bool)GetValue(IsPlayPauseVisibleProperty);
            set => SetValue(IsPlayPauseVisibleProperty, value);
        }
        #endregion

        #region IsLoadingVisible
        public static readonly DependencyProperty IsLoadingVisibleProperty =
            DependencyProperty.Register(nameof(IsLoadingVisible), typeof(bool), typeof(HorizontalSection), new PropertyMetadata(false));
        
        public bool IsLoadingVisible
        {
            get => (bool)GetValue(IsLoadingVisibleProperty);
            set => SetValue(IsLoadingVisibleProperty, value);
        }
        #endregion

        #region IsPlaying
        public static readonly DependencyProperty IsPlayingProperty =
            DependencyProperty.Register(nameof(IsPlaying), typeof(bool), typeof(HorizontalSection), new PropertyMetadata(false));

        public bool IsPlaying
        {
            get => (bool)GetValue(IsPlayingProperty);
            set => SetValue(IsPlayingProperty, value);
        }
        #endregion

        #region ItemsSource
        public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(HorizontalSection));

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }
        #endregion

        #region ItemTemplate
        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(HorizontalSection));

        public DataTemplate ItemTemplate
        {
            get => (DataTemplate)GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }
        #endregion

        private void HorizontalScroll_DisableMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not ScrollViewer scroll)
                return;

            // Пробрасываем событие наверх, чтобы сработал вертикальный ScrollViewer
            var parentScroll = FindParent<ScrollViewer>(scroll);
            if (parentScroll != null)
            {
                var args = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = UIElement.MouseWheelEvent,
                    Source = e.Source
                };
                parentScroll.RaiseEvent(args);
            }

            // Событие обработано, горизонтальный ScrollViewer не скроллится
            e.Handled = true;
        }


        // Вспомогательный метод поиска родителя ScrollViewer
        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject? parent = VisualTreeHelper.GetParent(child);
            while (parent != null && parent is not T)
                parent = VisualTreeHelper.GetParent(parent);
            return parent as T;
        }
    }
}
