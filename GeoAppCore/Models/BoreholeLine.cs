using GeoAppCore.Models;
using Newtonsoft.Json;

namespace GeoAppCore
{
    public class BoreholeLine : GeoObject
    {
        public string Id { get; set; }

        public List<Borehole> Boreholes { get; set; } = new();

        public double Azimuth
        {
            get
            {
                double dx = Last.X - First.X;
                double dy = Last.Y - First.Y;

                double azimuth = Math.Atan2(dx, dy);

                if (azimuth < 0)
                    azimuth += Math.PI * 2;

                // перевод в градусы
                return Math.Round(azimuth * 180 / Math.PI, 1);
            }
        }

        [JsonIgnore]
        public double MaxZ => Boreholes.Max(x => x.Z);

        [JsonIgnore]
        public double MinZ => Boreholes.Min(x => x.Z - x.Deapth);

        [JsonIgnore]
        public Borehole First => Boreholes.OrderBy(x => x.Id).First();

        [JsonIgnore]
        public Borehole Last => Boreholes.OrderBy(x => x.Id).Last();

        public List<SectionBorehole> BuildSections()
        {
            var sections = new List<SectionBorehole>();

            double distance = 0;

            var boreholes = Boreholes
                .OrderBy(x => x.Id)
                .ToList();

            for (int i = 0; i < boreholes.Count; i++)
            {
                var bh = boreholes[i];

                if (i > 0)
                {
                    Borehole prev = boreholes[i - 1];

                    double dx = bh.X - prev.X;
                    double dy = bh.Y - prev.Y;

                    distance += Math.Sqrt(dx * dx + dy * dy);
                }

                sections.Add(new SectionBorehole(bh, distance, bh.Z));
            }

            return sections;
        }

        public (string Front, string Backward) GetDirection()
        {
            string[] directions =
            {
                "С", "СВ", "В", "ЮВ",
                "Ю", "ЮЗ", "З", "СЗ"
            };

            int azimuth = (int)Math.Round((Azimuth % 360 + 360) % 360, 0);

            int front = azimuth switch
            {
                0 => 0, // С
                < 90 => 1, // СВ
                90 => 2, // В
                < 180 => 3, // ЮВ
                180 => 4, // Ю
                < 270 => 5, // ЮЗ
                270 => 6, // З
                _ => 7 // СЗ
            };

            int back = (front + 4) % 8;

            return (directions[front], directions[back]);
        }
    }
}
