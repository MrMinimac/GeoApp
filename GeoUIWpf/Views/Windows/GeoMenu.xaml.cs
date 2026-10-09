using LegendDesignWpf.Controls;
using System.Collections.ObjectModel;

namespace GeoUIWpf.Views.Windows
{
    
    public class GeoMenuItem
    {
        public string Title { get; init; }
        public string Description { get; init; }
        public Action Action { get; init; }
    }

    public partial class GeoMenu : LDWindow
    {
        public ObservableCollection<GeoMenuItem> Items { get; } = new();

        public GeoMenu(IEnumerable<GeoMenuItem> items)
        {
            InitializeComponent();

            DataContext = this;

            foreach (var item in items)
                Items.Add(item);
        }

        private void FlatButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is FlatButton btn && btn.DataContext is GeoMenuItem item)
            {
                item.Action.Invoke();
            }
        }
    }
}
