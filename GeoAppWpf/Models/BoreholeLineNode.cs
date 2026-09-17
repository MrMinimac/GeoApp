using GeoAppCore;

namespace GeoAppWpf.Models
{
    public class BoreholeLineNode : GeoTreeNode
    {
        public BoreholeLine Line { get; }

        public BoreholeLineNode(BoreholeLine line)
        {
            Line = line;
            Header = $"БЛ-{line.Number}";

            foreach (var borehole in line.Boreholes)
                Children.Add(new BoreholeNode(borehole));
        }
    }
}
