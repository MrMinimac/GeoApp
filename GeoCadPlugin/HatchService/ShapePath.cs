using Clipper2Lib;

namespace GeoCadPlugin.HatchService
{
    public sealed class ShapePath
    {
        public List<PointD> Points { get; } = new();
        public bool Closed { get; set; }
    }
}