using GeoAppCore.Objects;

namespace GeoAppCore.Abstractions.Document
{
    public interface IDocument
    {
        string Name { get; }
        string? FilePath { get; }
        IEnumerable<WorkspaceObject> GetWorkspaceObjects();
    }
}
