using Clipper2Lib;

namespace GeoCadPlugin.HatchService
{
    public sealed class HatchGeometry
    {
        public List<PathsD> FilledChunks { get; } = new();
        public PathsD Strokes { get; } = new();
        public int Placed { get; set; }
        public bool Truncated { get; set; } // сработал лимит времени/количества
    }
}