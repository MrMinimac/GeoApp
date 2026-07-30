using LegendDesignWpf.Core.Enums;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace LegendDesignWpf.Controls
{
    public class NavigationItem : DependencyObject
    {
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
            nameof(Icon), typeof(PackIconKind), typeof(NavigationItem));

        public PackIconKind Icon
        {
            get => (PackIconKind)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register(
            nameof(Target), typeof(Type), typeof(NavigationItem));

        public Type Target
        {
            get => (Type)GetValue(TargetProperty);
            set => SetValue(TargetProperty, value);
        }
    }

    public class NavigationView : ContentControl
    {
        private readonly Dictionary<Type, FrameworkElement> _cache = new();

        static NavigationView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(NavigationView),
                new FrameworkPropertyMetadata(typeof(NavigationView)));
        }

        public NavigationView()
        {
            SetValue(NavigationItemsPropertyKey, new ObservableCollection<NavigationItem>());
        }

        public ObservableCollection<NavigationItem> NavigationItems
        {
            get => (ObservableCollection<NavigationItem>)GetValue(NavigationItemsProperty);
        }

        private static readonly DependencyPropertyKey NavigationItemsPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(NavigationItems), typeof(ObservableCollection<NavigationItem>),
                typeof(NavigationView), new PropertyMetadata());

        public static readonly DependencyProperty NavigationItemsProperty =
            NavigationItemsPropertyKey.DependencyProperty;

        public NavigationItem SelectedItem
        {
            get { return (NavigationItem)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(NavigationItem), typeof(NavigationView),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = (NavigationView)d;

            if (e.NewValue is NavigationItem item)
            {
                panel.Navigate(item);
            }
        }

        private void Navigate(NavigationItem item)
        {
            if (!_cache.TryGetValue(item.Target, out var page))
            {
                page = (FrameworkElement)Activator.CreateInstance(item.Target)!;
                _cache[item.Target] = page;
            }

            Content = page;
        }
    }
}
