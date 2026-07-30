using Newtonsoft.Json;

namespace GeoAppCore
{
    public class Borehole
    {
        private List<LithologyInterval>? _lithologyIntervals;

        public string Key { get; set; } = "";
        public int LineNumber { get; set; }
        public int Id { get; set; }

        public List<Sample> Samples = new List<Sample>();

        [JsonIgnore]
        public double Deapth
        {
            get
            {
                double deapth = 0;

                foreach (var sample in Samples)
                    deapth += sample.Length;

                return Math.Round(deapth, 3);
            }
        }

        [JsonIgnore]
        public double AvgValue
        {
            get
            {
                double total = Samples.Sum(x => (x.Value == -1 || x.Value == 0) ? 0 : x.Length);

                if (total == 0)
                    return 0;

                var avg = Samples.Sum(x => (x.Value == -1 ? 0 : x.CleanedVertReserve)) / total;

                return Math.Round(avg, 3);
            }
        }

        [JsonIgnore]
        public int SamplesCount => Samples.Count();

        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public List<LithologyInterval> LithologyIntervals => BuildLithologyIntervals();

        private List<LithologyInterval> BuildLithologyIntervals()
        {
            if (_lithologyIntervals != null && _lithologyIntervals.Count != 0)
                return _lithologyIntervals;

            _lithologyIntervals = new List<LithologyInterval>();

            foreach (var sample in Samples.OrderBy(x => x.From))
            {
                var last = _lithologyIntervals.LastOrDefault();

                if (last != null &&
                    last.To == sample.From &&
                    last.Lithologies.SequenceEqual(sample.Lithologies))
                {
                    // продолжаем существующий интервал
                    last.To = sample.To;
                }
                else
                {
                    // создаем новый интервал
                    _lithologyIntervals.Add(new LithologyInterval
                    {
                        From = sample.From,
                        To = sample.To,
                        Lithologies = new List<Lithology>(sample.Lithologies)
                    });
                }
            }

            return _lithologyIntervals;
        }
    }
}
