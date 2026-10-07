namespace GeoAppCore.Models
{
    public class GetBoundaryResult
    {
        public bool Success { get; set; }

        public bool Cancelled { get; set; }

        public string? Error { get; set; }

        public List<BoundaryData> Boundaries { get; set; } = new();
    }
}
