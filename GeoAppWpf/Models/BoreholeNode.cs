using GeoAppCore;

namespace GeoAppWpf.Models
{
    public class BoreholeNode : GeoTreeNode
    {
        public Borehole Borehole { get; }

        public BoreholeNode(Borehole borehole)
        {
            Borehole = borehole;
            Header = $"Скв. {borehole.Id}";
        }
    }
}
