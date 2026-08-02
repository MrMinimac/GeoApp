using netDxf;
using System.Collections.ObjectModel;

namespace GeoAppWpf.Models
{
    public class DxfDocumentNode : GeoTreeNode
    {
        public DxfDocument Document { get; }
        public ObservableCollection<DxfEntityView> Entities { get; } = new();

        public DxfDocumentNode(DxfDocument doc)
        {
            Document = doc;
            Name = doc.Name;

            foreach (var entity in doc.Entities.All)
            {
                Children.Add(new EntitiesNode(entity));
                Entities.Add(new DxfEntityView(entity));
            }
        }
    }
}
