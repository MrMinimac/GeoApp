using GeoAppCore.Geometry;
using GeoAppCore.Objects;
using System.Collections.ObjectModel;

namespace GeoCadWpf.Models
{
    public class WorkspacePolylineNode : WorkspaceObjectNode
    {
        //public ObservableCollection<GeoVector3> Vertexes =>
        //    ((PolylineObject)Object).Vertexes;

        public int VertexesCount => ((PolylineObject)Object).Vertexes.Count;

        public string LayerName
        {
            get => ((PolylineObject)Object).LayerName;
            set
            {
                ((PolylineObject)Object).LayerName = value;
                OnPropertyChanged();
            }
        }

        public WorkspacePolylineNode(PolylineObject obj)
            : base(obj)
        {
        }
    }
}
