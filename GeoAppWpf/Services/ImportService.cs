using GeoAppCore.Abstractions.Document;
using System.IO;

namespace GeoAppWpf.Services
{
    public class ImportService
    {
        private readonly IReadOnlyList<IDocumentLoader> _loaders;

        public ImportService(IEnumerable<IDocumentLoader> loaders)
        {
            _loaders = loaders.ToList();
        }

        public IDocument? Import(string filePath)
        {
            var extension = Path.GetExtension(filePath);

            var loader = _loaders
                .FirstOrDefault(x => x.CanLoad(extension));

            if (loader == null)
                throw new NotSupportedException(
                    $"Формат '{extension}' не поддерживается.");

            var document = loader.Load(filePath);

            return document;
        }

        public string GetOpenFileFilter()
        {
            var extensions = _loaders
                .SelectMany(x => x.Extensions)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var patterns = string.Join(";", extensions.Select(x => $"*{x}"));

            return $"Поддерживаемые файлы ({patterns})|{patterns}|" +
                   "Все файлы (*.*)|*.*";
        }
    }
}
