using Newtonsoft.Json;

namespace GeoAppCore
{
    public class Sample
    {
        public double Length { get; set; }
        public double From { get; set; }
        public double To { get; set; }

        public double Value { get; set; }


        [JsonIgnore]
        public double Capacity => Math.Round((3.14 * Diametr * Diametr * Length) / 4);
        [JsonIgnore]
        public double AvgValue => Value == -1 ? -1 : Math.Round(Value * 1000 / Capacity, 3);
        [JsonIgnore]
        public double VertReserve => Value == -1 ? -1 : Math.Round(Length * AvgValue, 3);
        [JsonIgnore]
        public double CleanedAvgValue => Value == -1 ? -1 : Math.Round(AvgValue * Fineness, 3);
        [JsonIgnore]
        public double CleanedVertReserve => Value == -1 ? -1 : Math.Round(VertReserve * Fineness, 3);

        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public List<Lithology> Lithologies { get; set; } = new();

        public double Diametr { get; set; }
        public double Fineness { get; set; }

        public string GetValueString()
        {
            return Value switch
            {
                0 => "пс",
                -1 => "зн",
                _ => AvgValue.ToString()
            };
        }
    }
}
