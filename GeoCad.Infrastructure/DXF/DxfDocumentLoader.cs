using GeoAppCore.Abstractions.Document;

namespace GeoCad.Infrastructure.DXF
{
    public class DxfDocumentLoader : IDocumentLoader
    {
        public IEnumerable<string> Extensions => [".dxf"];

        public bool CanLoad(string extension)
            => extension.Equals(".dxf", StringComparison.OrdinalIgnoreCase);

        public IDocument Load(string path)
        {
            throw new NotImplementedException();
        }
    }
}
