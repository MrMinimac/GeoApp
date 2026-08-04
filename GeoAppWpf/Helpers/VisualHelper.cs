using System.Windows;
using System.Windows.Media;

namespace GeoAppWpf.Helpers
{
    public class VisualHelper
    {
        public static T? FindParent<T>(DependencyObject obj) where T : DependencyObject
        {
            while (obj != null)
            {
                if (obj is T result)
                    return result;

                obj = VisualTreeHelper.GetParent(obj);
            }

            return null;
        }
    }
}
