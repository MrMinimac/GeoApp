using GeoAppCore;

namespace GeoAppWpf.Models
{
    public class SampleNode : GeoTreeNode
    {
        public Sample Sample { get; }

        public SampleNode(Sample sample)
        {
            Sample = sample;
            Name = $"{sample.From}-{sample.To}";
        }
    }
}
