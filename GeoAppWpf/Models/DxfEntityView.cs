using netDxf.Entities;
using System.Windows.Media;

namespace GeoAppWpf.Models
{
    public class DxfEntityView
    {
        public EntityObject Entity { get; }
        public int Vertexes { get; set; }
        public string Layer => Entity.Layer.Name;
        public string Handle => Entity.Handle;
        public Color Color => Color.FromArgb(255, Entity.Color.R, Entity.Color.G, Entity.Color.B);

        public DxfEntityView(EntityObject entity)
        {
            Entity = entity;

            Vertexes = Entity switch
            {
                Polyline3D p => p.Vertexes.Count,
                Polyline2D p => p.Vertexes.Count,
                Line => 2,
                Circle => 1,
                _ => 0
            };
        }
    }
}
