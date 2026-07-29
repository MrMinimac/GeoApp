namespace GeoAppCore
{
    public class LithologyInterval
    {
        public double From { get; set; }
        public double To { get; set; }

        public List<Lithology> Lithologies { get; set; } = new();

        public double Length => To - From;
    }
}
