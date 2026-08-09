using System.Windows;

namespace GeoCadWpf.Helpers
{
    public static class TreeViewHelper
    {
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.RegisterAttached(
                "Header",
                typeof(object),
                typeof(TreeViewHelper),
                new FrameworkPropertyMetadata(null));

        public static void SetHeader(DependencyObject element, object value)
            => element.SetValue(HeaderProperty, value);

        public static object GetHeader(DependencyObject element)
            => element.GetValue(HeaderProperty);
    }
}
