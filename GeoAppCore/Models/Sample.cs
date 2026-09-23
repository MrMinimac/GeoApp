using Newtonsoft.Json;

namespace GeoAppCore
{
    public class Sample
    {
        public double Length { get; set; }
        public double From { get; set; }
        public double To { get; set; }

        public double Grade { get; set; }


        [JsonIgnore]
        public double Capacity => Math.Round((3.14 * Diametr * Diametr * Length) / 4);
        [JsonIgnore]
        public double AvgValue => Grade == -1 ? -1 : Math.Round(Grade * 1000 / Capacity, 3);
        [JsonIgnore]
        public double VertReserve => Grade == -1 ? -1 : Math.Round(Length * AvgValue, 3);
        [JsonIgnore]
        public double CleanedAvgValue => Grade == -1 ? -1 : Math.Round(AvgValue * Fineness, 3);
        [JsonIgnore]
        public double CleanedVertReserve => Grade == -1 ? -1 : Math.Round(VertReserve * Fineness, 3);

        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public List<Lithology> Lithologies { get; set; } = new();

        public double Diametr { get; set; }
        public double Fineness { get; set; }

        public string GetValueString()
        {
            return Grade switch
            {
                0 => "пс",
                -1 => "зн",
                _ => AvgValue.ToString()
            };
        }
    }
}
