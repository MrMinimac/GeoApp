using GeoAppCore.Abstractions.Document;

namespace GeoAppWpf.Services
{
    public class WorkspaceManager
    {
        private readonly List<IDocument> _documents = new();

        public ImportService Importer { get; }
        public IReadOnlyList<IDocument> Documents => _documents;

        public event Action? OnDocumentsChanged;

        public WorkspaceManager(ImportService importer)
        {
            Importer = importer;
        }

        public string GetOpenFileFilter()
        {
            return Importer.GetOpenFileFilter();
        }

        public void ImportFiles(string[] filePaths)
        {
            foreach (var file in filePaths)
            {
                var document = Importer.Import(file);

                if (document == null)
                    return;

                _documents.Add(document);
                OnDocumentsChanged?.Invoke();
            }
        }
    }
}
