using GeoAppCore.Objects;
using GeoAppCore.Workspace;
using GeoCadWpf.Models;
using GeoCadWpf.Services;
using LegendDesignWpf.Core.MVVM;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;

namespace GeoCadWpf.ViewModels
{
    public class WorkspaceViewModel : BaseViewModel
    {
        public readonly Workspace Workspace;
        private WorkspaceObjectNode? _selectedObject;
        public ViewportController ViewportController { get; }
        public ObservableCollection<WorkspaceObjectNode> Nodes { get; } = new();

        public string Name
        {
            get => Workspace.Name;
            set
            {
                if (Workspace.Name == value)
                    return;

                Workspace.Name = value;
                OnPropertyChanged();
            }
        }

        public WorkspaceObjectNode? SelectedObject
        {
            get => _selectedObject;
            set 
            {
                if (_selectedObject == value)
                    return;

                _selectedObject = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<WorkspaceObject> Objects => Workspace.Objects;

        public WorkspaceViewModel(Workspace workspace)
        {
            Workspace = workspace;

            Workspace.Objects.CollectionChanged += Objects_CollectionChanged;

            foreach (var obj in Workspace.Objects)
                Nodes.Add(CreateNode(obj));

            ViewportController = new ViewportController();
            ViewportController.OnAttachedChanged += OnViewportAttachedChanged;
        }

        public void Add(WorkspaceObject obj)
        {
            Workspace.Add(obj);
            Nodes.Add(CreateNode(obj));
        }

        private void Objects_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (WorkspaceObject obj in e.NewItems!)
                {
                    ViewportController.Add(new ViewportObject(obj));
                    Nodes.Add(CreateNode(obj));
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (WorkspaceObject obj in e.NewItems!)
                {
                    ViewportController.Remove(new ViewportObject(obj));
                    Nodes.Remove(CreateNode(obj));
                }
            }
        }

        private WorkspaceObjectNode CreateNode(WorkspaceObject obj)
        {
            return obj switch
            {
                PolylineObject polyline => new WorkspacePolylineNode(polyline),
                _ => new WorkspaceObjectNode(obj)
            };
        }

        private void OnViewportAttachedChanged(bool isAttached)
        {
            if (isAttached)
            {
                ApplySettings();
            }
        }

        private void ApplySettings()
        {
            if (ViewportController.Viewport == null)
                return;

            var wp = ViewportController.Viewport;

            wp.RotateGesture = new MouseGesture(MouseAction.MiddleClick, ModifierKeys.Shift);
            wp.PanGesture = new MouseGesture(MouseAction.MiddleClick);
        }
    }
}
