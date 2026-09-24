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
        public double Capacity => (3.14 * Diametr * Diametr * Length) / 4;
        [JsonIgnore]
        public double AvgGrade => Grade == -1 ? -1 : Grade * 1000 / Capacity;
        [JsonIgnore]
        public double VertReserve => Grade == -1 ? -1 : Length * AvgGrade;
        [JsonIgnore]
        public double PureAvgGrade => Grade == -1 ? -1 : AvgGrade * Fineness;
        [JsonIgnore]
        public double PureVertReserve => Grade == -1 ? -1 : VertReserve * Fineness;

        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public List<Lithology> Lithologies { get; set; } = new();

        public double Diametr { get; set; }
        public double Fineness { get; set; }

        public string GetPureAvgGradeString(int round = 3)
        {
            return Grade switch
            {
                0 => "пс",
                -1 => "зн",
                _ => PureAvgGrade.ToString($"F{round}")
            };
        }
    }
}
