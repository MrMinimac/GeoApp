using GeoAppCore.Abstractions.Document;
using GeoAppCore.Models;

namespace GeoAppWpf.Services
{
    public class ExcelDocument : IDocument
    {
        public List<GeoObject> Objects { get; init; } = new();

        public string Name { get; set; } = "Untitled";

        public string? FilePath { get; set; }

        public IEnumerable<GeoObject> GetObjects() => Objects;
    }
}
