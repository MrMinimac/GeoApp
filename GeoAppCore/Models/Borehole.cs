using Newtonsoft.Json;
using System.Diagnostics;

namespace GeoAppCore
{
    public class Borehole
    {
        private List<LithologyInterval>? _lithologyIntervals;
        private double _deapth = 0;

        public string Key { get; set; } = "";
        public int LineNumber { get; set; }
        public int Id { get; set; }

        public List<Sample> Samples = new();

        [JsonIgnore]
        public double Deapth
        {
            get
            {
                if (Samples.Count == 0)
                    return _deapth;

                return double.Round(Samples.Sum(x => x.Length), 3);
            }
            set => _deapth = value;
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

            var intervalsList = new List<string>();

            foreach (var interval in _lithologyIntervals)
            {
                var s = string.Join(",", interval.Lithologies.Select(x => (int)x));
                intervalsList.Add($"{interval.From} - {interval.To} = {s}");
            }

            Debug.WriteLine($"\nСкв. {Id}\n [ {string.Join("; ", intervalsList)} ]\n");

            return _lithologyIntervals;
        }
    }
}
