using Newtonsoft.Json;

namespace GeoAppCore
{
    public class Borehole
    {
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
    }
}
