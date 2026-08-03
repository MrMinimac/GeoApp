namespace GeoAppWpf.Models
{
    public class MacrominePoint
    {
        public double X { get; set; } // EAST
        public double Y { get; set; } // NORTH
        public double Z { get; set; } // RL
        public string StringCode { get; set; } = string.Empty; // STRING
        public string JoinId { get; set; } = string.Empty;     // JOIN
    }
}
