using GeoAppCore.Services;
using System.Net;

namespace GeoAppCore
{
    public static class BoreholeMainOreIntervalExtensions
    {
        public static OreInterval? GetMainOreInterval(
            this Borehole borehole,
            double minGrade,
            double? maxWasteThickness = null)
        {
            var intervals = borehole.BuildOreIntervals(minGrade, maxWasteThickness);

            if (intervals.Count == 0)
                return null;

            return intervals
                .OrderByDescending(x => x.Length)
                .ThenByDescending(x => x.AvgGrade)
                .First();
        }
    }

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

        public double OreIntervalMinGrade { get; set; } = 0.15;
        public double? OreIntervalMaxWasteThickness { get; set; } = null;

        public List<LithologyInterval> LithologiesIntervals => _source.LithologyIntervals;

        public IEnumerable<Sample> Samples => _source.Samples;

        public OreInterval? OreInterval
        {
            get 
            {
                if (_oreIterval == null)
                {
                    _oreIterval = _source
                        .GetMainOreInterval(OreIntervalMinGrade, OreIntervalMaxWasteThickness);
                }

                return _oreIterval;
            }
        }

        public SectionBorehole(Borehole borehole, double distance, double elevation)
        {
            _source = borehole;
            Distance = distance;
            Elevation = elevation;
        }
    }
}
