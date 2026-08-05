using System.Collections.ObjectModel;
using System.Windows.Input;

namespace GeoAppWpf.Models
{
    public class TreeMenuItem
    {
        public string Header { get; init; }

        public ICommand Command { get; init; }

        public object? CommandParameter { get; init; }

        public ObservableCollection<TreeMenuItem> Items { get; init; } = [];
    }
}
