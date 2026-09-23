using GeoAppCore;
using GeoAppWpf.ViewModels;

namespace GeoAppWpf.Models
{
    public class BoreholeLineNode : Node
    {
        public BoreholeLine Line { get; }

        public BoreholeLineNode(BoreholeLine line, CommandsProvider cmdProvider)
            : base(line.Id, cmdProvider)
        {
            Line = line;

            foreach (var borehole in line.Boreholes)
                Children.Add(new BoreholeNode(borehole, cmdProvider));
        }
    }
}
