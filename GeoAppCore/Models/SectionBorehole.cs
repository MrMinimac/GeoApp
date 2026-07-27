namespace GeoAppCore
{
    public class SectionBorehole
    {
        public Borehole Source { get; set; }

        public double Distance { get; set; }

        public double Elevation { get; set; }


        public double X => Distance;
        public double Top => Elevation;
        public double Bottom => Elevation - Source.Deapth;
        public double Deapth => Top - Bottom;
    }
}
