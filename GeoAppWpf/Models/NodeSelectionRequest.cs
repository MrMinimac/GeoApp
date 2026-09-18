using System.Windows.Input;

namespace GeoAppWpf.Models
{
    public readonly record struct NodeSelectionRequest(Node Node, ModifierKeys Modifiers);
}
