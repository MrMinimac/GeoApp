using System.Collections.ObjectModel;

namespace GeoAppWpf.Models
{
    public abstract class GeoTreeNode
    {
        public string Name { get; set; }

        public ObservableCollection<GeoTreeNode> Children { get; } = new();
    }
}
