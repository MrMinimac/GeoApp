using GeoAppCore.Geometry;
using System.Collections.ObjectModel;

namespace GeoAppCore.Objects
{
    public class PolylineObject : WorkspaceObject
    {
        public ObservableCollection<GeoVector3> Vertexes { get; } = new();

        public PolylineObject()
        {
            Name = "Полилиния";
        }

        public PolylineObject(IEnumerable<GeoVector3> vertexes)
        {
            Name = "Полилиния";
            Vertexes = new ObservableCollection<GeoVector3>(vertexes);
        }
    }

    public class FaceObject : WorkspaceObject
    {
        public GeoVector3 FirstVertex { get; set; } = GeoVector3.Zero;

        public GeoVector3 SecondVertex { get; set; } = GeoVector3.Zero;

        public GeoVector3 ThirdVertex { get; set; } = GeoVector3.Zero;

        public GeoVector3 FourthVertex { get; set; } = GeoVector3.Zero;

        public FaceObject()
        {
            Name = "3D-грань";
        }
    }

    public class PointObject : WorkspaceObject
    {
        public GeoVector3 Position { get; set; } = GeoVector3.Zero;

        public PointObject()
        {
            Name = "Точка";
        }
    }
}
