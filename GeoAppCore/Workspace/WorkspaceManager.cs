using GeoAppCore.Abstractions.Document;
using System.Collections.ObjectModel;

namespace GeoAppCore.Workspace
{
    public class WorkspaceManager
    {
        private readonly ObservableCollection<Workspace> _workspaces = new();

        public ReadOnlyObservableCollection<Workspace> Workspaces { get; }

        private Workspace? _currentWorkspace;

        public Workspace? CurrentWorkspace
        {
            get => _currentWorkspace;
            private set
            {
                if (_currentWorkspace == value)
                    return;

                _currentWorkspace = value;
                CurrentWorkspaceChanged?.Invoke();
            }
        }

        public event Action? CurrentWorkspaceChanged;

        public WorkspaceManager()
        {
            Workspaces = new ReadOnlyObservableCollection<Workspace>(_workspaces);
        }

        public Workspace Create()
        {
            var workspace = new Workspace();

            _workspaces.Add(workspace);
            CurrentWorkspace = workspace;

            return workspace;
        }

        public bool Remove(Workspace workspace)
        {
            if (!_workspaces.Remove(workspace))
                return false;

            if (CurrentWorkspace == workspace)
                CurrentWorkspace = _workspaces.LastOrDefault();

            return true;
        }

        public void Select(Workspace workspace)
        {
            if (_workspaces.Contains(workspace))
                CurrentWorkspace = workspace;
        }

        public void LoadDocument(IDocument document)
        {
            CurrentWorkspace?.LoadDocument(document);
        }
    }
}
