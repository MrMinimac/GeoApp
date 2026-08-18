using netDxf.Entities;
using Newtonsoft.Json;

namespace GeoAppWpf.Models
{
    public abstract class MacromineEntity
    {

    }

    public class MacrominePoint : MacromineEntity
    {
        public double X { get; set; } // EAST
        public double Y { get; set; } // NORTH
        public double Z { get; set; } // RL
        public string StringCode { get; set; } = string.Empty; // STRING
        public string JoinId { get; set; } = string.Empty;     // JOIN
    }

    public class MacromineSample
    {
        public string HoleId { get; set; } = "";
        public double From { get; set; }
        public double To { get; set; }
        public double Length { get; set; }

        public string SampleId { get; set; } = "";

        public double Au { get; set; }
        public double Restriction { get; set; }

        public string Ovp { get; set; } = "";
        public string Code { get; set; } = "";

        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public double MinAu { get; set; }
        public bool IsVisible => Au > MinAu;

        // DXF геометрия этой пробы
        [JsonIgnore]
        public Polyline3D? Entity { get; set; }
    }
}
