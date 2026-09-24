using GeoAppCore.Ore;

namespace GeoAppCore
{
    public class SectionBorehole
    {
        private Borehole _source;
        private OreInterval? _oreIterval;

        public Borehole Source => _source;

        public double Distance { get; }
        public double Elevation { get; }

        public int Id => _source.Id;
        public double X => Distance;
        public double Top => Elevation;
        public double Bottom => Elevation - _source.Deapth;
        public double Deapth => _source.Deapth;

        public IOreCondition OreCondition { get; set; } = new StrippingRatioCondition();

        public List<LithologyInterval> LithologiesIntervals => _source.LithologyIntervals;

        public IEnumerable<Sample> Samples => _source.Samples;

        public OreInterval? OreInterval => _oreIterval ??= _source.GetMainOreInterval(OreCondition);

        public SectionBorehole(Borehole borehole, double distance, double elevation)
        {
            _source = borehole;
            Distance = distance;
            Elevation = elevation;
        }
    }
}
