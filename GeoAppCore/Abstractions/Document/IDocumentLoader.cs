namespace GeoAppCore.Abstractions.Document
{
    public interface IDocumentLoader
    {
        IEnumerable<string> Extensions { get; }

        bool CanLoad(string extension);

        IDocument Load(string path);
    }
}
