using netDxf;
using System.Collections.ObjectModel;
using System.IO;

namespace GeoAppWpf.Services
{
    public interface IDocument
    {
        string Name { get; }
        string? FilePath { get; }
    }

    public interface IDocumentLoader
    {
        IReadOnlyList<string> Extensions { get; }

        IDocument Load(string filePath);
    }

    public class DocumentManager
    {
        private readonly IReadOnlyList<IDocumentLoader> _loaders;
        public event Action<IDocument>? DocumentLoaded;

        public ObservableCollection<IDocument> Documents { get; } = new();

        public DocumentManager(IEnumerable<IDocumentLoader> loaders)
        {
            _loaders = loaders.ToList();
        }

        public IDocument Open(string filePath)
        {
            var extension = Path.GetExtension(filePath);

            var loader = _loaders.FirstOrDefault(x =>
                x.Extensions.Any(e =>
                    e.Equals(extension, StringComparison.OrdinalIgnoreCase)));

            if (loader == null)
                throw new NotSupportedException(
                    $"Формат '{extension}' не поддерживается.");

            var document = loader.Load(filePath);

            Documents.Add(document);

            DocumentLoaded?.Invoke(document);

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

    public class DxfDocumentLoader : IDocumentLoader
    {
        public IReadOnlyList<string> Extensions => [".dxf", ".dat"];

        public IDocument Load(string filePath)
        {
            var document = new DXFDocument
            {
                FilePath = filePath,
                Name = Path.GetFileNameWithoutExtension(filePath)
            };

            return document;
        }
    }

    public class DXFDocument : IDocument
    {
        private DxfDocument? _data;

        public required string Name { get; init; }
        public string? FilePath { get; init; }

        public DXFDocument()
        {
            switch (Path.GetExtension(FilePath))
            {
                case ".dxf":
                    LoadDxf();
                    break;

                case ".dat":
                    LoadDat();
                    break;
            }
        }

        private void LoadDat()
        {
            if (FilePath == null)
                return;

            var datReader = new MacromineDatReader();
            _data = datReader.ReadToDxf(FilePath);
        }

        private void LoadDxf()
        {
            if (FilePath == null)
                return;

            _data = DxfDocument.Load(FilePath);
        }
    }

    //internal class DxfLoderService
    //{
    //    public event Action<DxfDocument>? DocumentChanged;

    //    public void Load()
    //    {
    //        try
    //        {
    //            OpenFileDialog dialog = new OpenFileDialog();

    //            dialog.Filter = "DXF files (*.dxf)|*.dxf";

    //            if (dialog.ShowDialog() == true)
    //            {
    //                string filePath = dialog.FileName;
    //                var doc = DxfDocument.Load(filePath);
    //                doc.Name = Path.GetFileNameWithoutExtension(filePath);
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            LocaleService.ShowError(ex);
    //        }
    //    }

    //    public void ImportDat()
    //    {
    //        try
    //        {
    //            OpenFileDialog dialog = new OpenFileDialog();

    //            dialog.Filter = "DAT files (*.dat)|*.dat";

    //            if (dialog.ShowDialog() == true)
    //            {
    //                string filePath = dialog.FileName;

    //                var datReader = new MacromineDatReader();
    //                var doc = datReader.ReadToDxf(filePath);
    //                doc.Name = Path.GetFileNameWithoutExtension(filePath);
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            LocaleService.ShowError(ex);
    //        }
    //    }
    //}
}
