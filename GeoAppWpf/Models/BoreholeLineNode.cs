using GeoAppCore;
using GeoAppWpf.ViewModels;

namespace GeoAppWpf.Models
{
    public class BoreholeLineNode : GeoTreeNode
    {
        public BoreholeLine Line { get; }

        public BoreholeLineNode(BoreholeLine line, ITreeCommandProvider commandProvider) : base(commandProvider)
        {
            Line = line;
            Name = $"БЛ-{line.Number}";

            foreach (var borehole in line.Boreholes)
                Children.Add(new BoreholeNode(borehole, CommandProvider));
        }
    }
}
