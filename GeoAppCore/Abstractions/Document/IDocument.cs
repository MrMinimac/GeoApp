using GeoAppCore.Models;

namespace GeoAppCore.Abstractions.Document
{
    public interface IDocument
    {
        string Name { get; }
        string? FilePath { get; }
        IEnumerable<GeoObject> GetObjects();
    }
}
