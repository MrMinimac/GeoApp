using CommunityToolkit.Mvvm.Input;
using GeoAppWpf.Models;
using HelixToolkit.Wpf;
using netDxf.Entities;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace GeoAppWpf.Services
{
    public class ViewportController
    {
        private HelixViewport3D? _viewport;

        private readonly ObservableCollection<DrawerObject> _visuals = new();
        public IReadOnlyCollection<DrawerObject> Visuals => _visuals;


        private readonly ObservableCollection<DrawerObject> _selectedVisuals = new();
        public IReadOnlyCollection<DrawerObject> SelectedVisuals => _selectedVisuals;


        private readonly HashSet<DrawerObject> _hiddenVisuals = new();
        public IReadOnlyCollection<DrawerObject> HiddenVisuals => _hiddenVisuals;


        private DrawerObject? _hoveredVisuals;

        public bool IsAttached => _viewport != null;

        public void Attach(HelixViewport3D viewport)
        {
            _viewport = viewport;
        }

        public void Detach()
        {
            _viewport = null;
        }

        public void ZoomExtents()
        {
            _viewport?.ZoomExtents();
        }

        #region Hide / Show

        public void HideSelectedObjects()
        {
            if (_selectedVisuals.Count == 0)
                return;

            foreach (var obj in _selectedVisuals.ToList())
            {
                if (!obj.IsVisible)
                    continue;

                obj.IsVisible = false;
                _hiddenVisuals.Add(obj);

                Unselect(obj);
                Remove(obj);
            }
        }

        public void ShowAllObjects()
        {
            foreach (var obj in _hiddenVisuals)
            {
                obj.IsVisible = true;
                Add(obj);
            }

            _hiddenVisuals.Clear();
        }

        #endregion

        #region Select / Unselect

        public void Select(Visual3D visual)
        {
            var obj = _visuals.FirstOrDefault(x => x.Visual == visual);
            if (obj == null) return;
            Select(obj);
        }

        public void SelectRange(List<DrawerObject> objs)
        {
            foreach (var obj in objs)
            {
                Select(obj);
            }
        }

        public void Select(DrawerObject obj)
        {
            if (_selectedVisuals.Contains(obj))
                return;

            _selectedVisuals.Add(obj);
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

        public void Unselect(DrawerObject obj)
        {
            _selectedVisuals.Remove(obj);
            obj.IsSelected = false;
        }

        public void UnselectLast()
        {
            var obj = _selectedVisuals.LastOrDefault();
            if (obj == null) return;
            Unselect(obj);
        }

        public void UnselectAll()
        {
            while (_selectedVisuals.Count > 0)
                Unselect(_selectedVisuals[^1]);
        }

        public void ToggleSelection(Visual3D visual)
        {
            var obj = _visuals.FirstOrDefault(x => x.Visual == visual);
            if (obj == null) return;
            ToggleSelection(obj);
        }

        public void ToggleSelection(DrawerObject obj)
        {
            if (_selectedVisuals.Contains(obj))
                Unselect(obj);
            else
                Select(obj);
        }

        #endregion

        #region Hover / Unhover

        public void Hover(Visual3D visual)
        {
            var obj = _visuals.FirstOrDefault(x => x.Visual == visual);

            if (obj == null || obj == _hoveredVisuals)
                return;

            if (_hoveredVisuals != null)
                _hoveredVisuals.IsHovered = false;

            _hoveredVisuals = obj;
            _hoveredVisuals.IsHovered = true;
        }

        public void Unhover()
        {
            if (_hoveredVisuals == null)
                return;

            _hoveredVisuals.IsHovered = false;
            _hoveredVisuals = null;
        }

        #endregion

        public void Add(DrawerObject obj)
        {
            if (_viewport == null)
                throw new ArgumentNullException("Viewport");

            bool viewportEmpty = _visuals.Count == 0;

            AddInternal(obj);
            obj.RequestUpdate();

            if (viewportEmpty)
                _viewport.ZoomExtents();
        }

        public void AddRange(IEnumerable<DrawerObject> objs)
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

        private void AddInternal(DrawerObject obj)
        {
            if (obj == null || obj.Visual == null)
                return;

            if (_visuals.Contains(obj) && _viewport.Children.Contains(obj.Visual))
                return;

            obj.Changed += Update;

            if (!_visuals.Contains(obj))
                _visuals.Add(obj);

            if (!_viewport.Children.Contains(obj.Visual))
                _viewport.Children.Add(obj.Visual);
        }

        public void Remove(DrawerObject obj)
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

        public void Update(DrawerObject obj)
        {
            switch (obj.Entity)
            {
                case Face3D:
                    UpdateFace3D(obj);
                    break;
                case Polyline3D:
                    UpdatePolyline3D(obj);
                    break;
            }

            _viewport?.UpdateLayout();
        }

        private void UpdatePolyline3D(DrawerObject obj)
        {
            if (obj.Visual is not ScreenSpaceVisual3D actual)
                return;

            actual.Points.Clear();

            var points = ((Polyline3D)obj.Entity).Vertexes
                .Select(v => new Point3D(v.X, v.Y, v.Z))
                .ToList();

            for (int i = 0; i < points.Count - 1; i++)
            {
                actual.Points.Add(points[i]);
                actual.Points.Add(points[i + 1]);
            }
        }

        private void UpdateFace3D(DrawerObject obj)
        {
            if (obj.Visual is not ScreenSpaceVisual3D actual)
                return;

            if (obj.Entity is not Face3D face3D)
                return;

            actual.Points.Clear();

            var p1 = new Point3D(face3D.FirstVertex.X, face3D.FirstVertex.Y, face3D.FirstVertex.Z);
            var p2 = new Point3D(face3D.SecondVertex.X, face3D.SecondVertex.Y, face3D.SecondVertex.Z);
            var p3 = new Point3D(face3D.ThirdVertex.X, face3D.ThirdVertex.Y, face3D.ThirdVertex.Z);

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
