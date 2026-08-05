using GeoAppCore;
using GeoAppWpf.Interfaces;
using GeoAppWpf.ViewModels;

namespace GeoAppWpf.Models
{
    public class BoreholeNode : GeoTreeNode
    {
        public Borehole Borehole { get; }

        public BoreholeNode(Borehole borehole, ITreeCommandProvider commandProvider) : base(commandProvider)
        {
            Borehole = borehole;
            Name = $"Скв. {borehole.Id}";
        }
    }
}
