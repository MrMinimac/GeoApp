using Newtonsoft.Json;

namespace GeoAppCore
{
    public class BoreholeLine
    {
        public int Number { get; set; }

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

                    distance += Math.Sqrt(
                        dx * dx +
                        dy * dy);
                }

                sections.Add(new SectionBorehole
                {
                    Source = bh,

                    // X в разрезе
                    Distance = distance,

                    // Y в разрезе
                    Elevation = bh.Z
                });
            }

            return sections;
        }
    }
}
