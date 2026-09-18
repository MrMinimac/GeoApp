using GeoAppCore;
using GeoAppWpf.ViewModels;

namespace GeoAppWpf.Models
{
    public class BoreholeNode : Node
    {
        public Borehole Borehole { get; }

        public BoreholeNode(Borehole borehole, CommandsProvider cmdProvider)
            : base($"Скв. {borehole.Id}", cmdProvider)
        {
            Borehole = borehole;
        }
    }
}
