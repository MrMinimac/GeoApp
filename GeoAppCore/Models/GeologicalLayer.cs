namespace GeoAppCore.Models
{
    public class GeologicalLayer
    {
        public List<Lithology> Lithologies { get; set; } = new();

        public List<LayerPoint> Points { get; set; } = new();
    }

    public class LayerPoint
    {
        public double X { get; set; }

        public double Top { get; set; }

        public double Bottom { get; set; }

        public bool Exists { get; set; }
    }
}
