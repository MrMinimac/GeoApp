using GeoAppCore.Abstractions.Document;
using GeoAppCore.Objects;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace GeoAppCore.Workspace
{
    public class Workspace
    {
        public string Name { get; set; }

        public ObservableCollection<WorkspaceObject> Objects { get; } = new();

        public Workspace()
        {
            Name = "Новый документ";
        }

        public void Add(WorkspaceObject obj)
        {
            Objects.Add(obj);
        }

        public void AddRange(IEnumerable<WorkspaceObject> objects)
        {
            foreach (var obj in objects)
                Objects.Add(obj);
        }

        public void Remove(WorkspaceObject obj)
        {
            Objects.Remove(obj);
        }

        public void LoadDocument(IDocument document)
        {
            var objects = document.GetWorkspaceObjects();
            AddRange(objects);
        }
    }
}
