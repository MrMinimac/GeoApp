using GeoAppCore.Objects;
using GeoCadWpf.Models;
using HelixToolkit.Wpf;
using netDxf.Entities;
using System.Collections.ObjectModel;
using System.Windows.Media.Media3D;

namespace GeoCadWpf.Services
{
    public class ViewportController
    {
        private HelixViewport3D? _viewport;
        public HelixViewport3D? Viewport => _viewport;

        private readonly ObservableCollection<ViewportObject> _visuals = new();
        public IReadOnlyCollection<ViewportObject> Visuals => _visuals;


        private readonly ObservableCollection<ViewportObject> _selectedObjects = new();
        public IReadOnlyCollection<ViewportObject> SelectedObjects => _selectedObjects;

        private readonly HashSet<ViewportObject> _hiddenObjects = new();
        private ViewportObject? _hoveredObject;

        public bool IsAttached => _viewport != null;

        public event Action<bool>? OnAttachedChanged;

        public void Attach(HelixViewport3D viewport)
        {
            _viewport = viewport;

            if (IsAttached)
                OnAttachedChanged?.Invoke(true);
        }

        public void Detach()
        {
            _viewport = null;
            OnAttachedChanged?.Invoke(false);
        }

        public void ZoomExtents()
        {
            _viewport?.ZoomExtents();
        }

        #region Hide / Show

        public void HideSelectedObjects()
        {
            if (_selectedObjects.Count == 0)
                return;

            foreach (var obj in _selectedObjects.ToList())
            {
                if (!obj.IsVisible)
                    continue;

                obj.IsVisible = false;
                _hiddenObjects.Add(obj);

                Unselect(obj);
                Remove(obj);
            }
        }

        public void ShowAllObjects()
        {
            foreach (var obj in _hiddenObjects)
            {
                obj.IsVisible = true;
                Add(obj);
            }

            _hiddenObjects.Clear();
        }

        #endregion

        #region Select / Unselect

        public void Select(Visual3D visual)
        {
            var obj = _visuals.FirstOrDefault(x => x.Visual == visual);
            if (obj == null) return;
            Select(obj);
        }

        public void SelectRange(List<ViewportObject> objs)
        {
            foreach (var obj in objs)
            {
                Select(obj);
            }
        }

        public void Select(ViewportObject obj)
        {
            if (_selectedObjects.Contains(obj))
                return;

            _selectedObjects.Add(obj);
            obj.IsSelected = true;
        }

        public void SelectAll()
        {
            foreach (var obj in _visuals)
                Select(obj);
        }

        public void Unselect(Visual3D visual)
        {
            var obj = _visuals.FirstOrDefault(x => x.Visual == visual);
            if (obj == null) return;
            Unselect(obj);
        }

        public void Unselect(ViewportObject obj)
        {
            _selectedObjects.Remove(obj);
            obj.IsSelected = false;
        }

        public void UnselectLast()
        {
            var obj = _selectedObjects.LastOrDefault();
            if (obj == null) return;
            Unselect(obj);
        }

        public void UnselectAll()
        {
            while (_selectedObjects.Count > 0)
                Unselect(_selectedObjects[^1]);
        }

        public void ToggleSelection(Visual3D visual)
        {
            var obj = _visuals.FirstOrDefault(x => x.Visual == visual);
            if (obj == null) return;
            ToggleSelection(obj);
        }

        public void ToggleSelection(ViewportObject obj)
        {
            if (_selectedObjects.Contains(obj))
                Unselect(obj);
            else
                Select(obj);
        }

        #endregion

        #region Hover / Unhover

        public void Hover(Visual3D visual)
        {
            var obj = _visuals.FirstOrDefault(x => x.Visual == visual);

            if (obj == null || obj == _hoveredObject)
                return;

            if (_hoveredObject != null)
                _hoveredObject.IsHovered = false;

            _hoveredObject = obj;
            _hoveredObject.IsHovered = true;
        }

        public void Unhover()
        {
            if (_hoveredObject == null)
                return;

            _hoveredObject.IsHovered = false;
            _hoveredObject = null;
        }

        #endregion

        public void Add(ViewportObject obj)
        {
            if (_viewport == null)
                throw new ArgumentNullException("Viewport");

            bool viewportEmpty = _visuals.Count == 0;

            AddInternal(obj);
            obj.RequestUpdate();

            if (viewportEmpty)
                _viewport.ZoomExtents();
        }

        public void AddRange(IEnumerable<ViewportObject> objs)
        {
            if (_viewport == null)
                throw new ArgumentNullException("Viewport");

            bool viewportEmpty = _visuals.Count == 0;

            foreach (var obj in objs)
            {
                AddInternal(obj);
                obj.RequestUpdate();
            }

            if (viewportEmpty)
                _viewport.ZoomExtents();
        }

        private void AddInternal(ViewportObject obj)
        {
            if (obj.Visual == null)
                return;

            if (_visuals.Contains(obj) && _viewport.Children.Contains(obj.Visual))
                return;

            obj.Changed += Update;

            if (!_visuals.Contains(obj))
                _visuals.Add(obj);

            if (!_viewport.Children.Contains(obj.Visual))
                _viewport.Children.Add(obj.Visual);
        }

        public void Remove(ViewportObject obj)
        {
            if (obj.Visual == null)
                return;

            if (_viewport == null)
                throw new ArgumentNullException("Viewport");

            obj.Changed -= Update;

            _visuals.Remove(obj);

            if (_viewport.Children.Contains(obj.Visual))
                _viewport.Children.Remove(obj.Visual);
        }

        public void Update(ViewportObject obj)
        {
            switch (obj.Entity)
            {
                case FaceObject:
                    UpdateFace3D(obj);
                    break;
                case PolylineObject:
                    UpdatePolyline3D(obj);
                    break;
            }

            _viewport?.UpdateLayout();
        }

        private void UpdatePolyline3D(ViewportObject obj)
        {
            if (obj.Visual is not ScreenSpaceVisual3D actual)
                return;

            actual.Points.Clear();

            var points = ((PolylineObject)obj.Entity).Vertexes
                .Select(v => new Point3D(v.X, v.Y, v.Z))
                .ToList();

            for (int i = 0; i < points.Count - 1; i++)
            {
                actual.Points.Add(points[i]);
                actual.Points.Add(points[i + 1]);
            }
        }

        private void UpdateFace3D(ViewportObject obj)
        {
            if (obj.Visual is not ScreenSpaceVisual3D actual)
                return;

            if (obj.Entity is not FaceObject face)
                return;

            actual.Points.Clear();

            var p1 = new Point3D(face.FirstVertex.X, face.FirstVertex.Y, face.FirstVertex.Z);
            var p2 = new Point3D(face.SecondVertex.X, face.SecondVertex.Y, face.SecondVertex.Z);
            var p3 = new Point3D(face.ThirdVertex.X, face.ThirdVertex.Y, face.ThirdVertex.Z);

            // Добавляем 3 линии (6 точек), чтобы нарисовать контур треугольника
            // Линия 1
            actual.Points.Add(p1);
            actual.Points.Add(p2);
            // Линия 2
            actual.Points.Add(p2);
            actual.Points.Add(p3);
            // Линия 3 (замыкаем обратно на первую вершину)
            actual.Points.Add(p3);
            actual.Points.Add(p1);
        }
    }
}
