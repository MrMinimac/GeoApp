namespace GeoCadPlugin.HatchService
{
    public sealed class ParsedShape
    {
        public List<ShapePath> Paths { get; } = new();
        public double BaseArea { get; set; } // чистая площадь (even-odd) в единицах геометрии
    }
}