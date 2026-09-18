using System.Collections.ObjectModel;
using System.Windows.Input;

namespace GeoAppWpf.Models
{
    public class NodeMenuItem
    {
        public string Header { get; init; }

        public ICommand Command { get; init; }

        public object? CommandParameter { get; init; }

        public bool IsVisible { get; set; } = true;

        public ObservableCollection<NodeMenuItem> Items { get; init; } = [];
    }
}
