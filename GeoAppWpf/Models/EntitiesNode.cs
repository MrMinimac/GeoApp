using GeoAppWpf.Interfaces;
using GeoAppWpf.ViewModels;
using netDxf;
using netDxf.Entities;

namespace GeoAppWpf.Models
{
    public class EntitiesNode : GeoTreeNode
    {
        public EntityObject Entity { get; }

        public IEnumerable<Vector3> Vertexes { get; }

        public override IReadOnlyList<TreeMenuItem> MenuItems =>
        [
            new()
            {
                Header = "Открыть 3D просмотр",
                Command = CommandProvider.Open3DCommand
            }
        ];

        public EntitiesNode(EntityObject entity, ITreeCommandProvider commandProvider) : base(commandProvider)
        {
            Entity = entity;
            Name = entity.Type.ToString();
            Vertexes = GetVertexes();
        }

        private IEnumerable<Vector3> GetVertexes()
        {
            switch (Entity)
            {
                case Polyline3D pl3D:
                    return pl3D.Vertexes;

                case Polyline2D pl2D:
                    return pl2D.Vertexes.Select(v => new Vector3(v.Position.X, v.Position.Y, 0));

                case Line l:
                    return new[]
                    {
                        l.StartPoint,
                        l.EndPoint
                    };

                case Circle circle:
                    return new[]
                    {
                         circle.Center
                    };

                default:
                    return Enumerable.Empty<Vector3>();
            }
        }
    }
}
