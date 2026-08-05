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
        private DrawerObject? _hoveredObject;

        public bool IsAttached => _viewport != null;

        public void Attach(HelixViewport3D viewport)
        {
            _viewport = viewport;
        }

        public void Detach()
        {
            _viewport = null;
        }

        #region Select / Unselect

        public void Select(Visual3D visual)
        {
            var obj = _visuals.FirstOrDefault(x => x.Actual == visual);
            if (obj == null) return;
            Select(obj);
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
            var obj = _visuals.FirstOrDefault(x => x.Actual == visual);
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
            var obj = _visuals.FirstOrDefault(x => x.Actual == visual);
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
            var obj = _visuals.FirstOrDefault(x => x.Actual == visual);

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

        public void Add(DrawerObject obj)
        {
            if (_viewport == null)
                throw new ArgumentNullException("Viewport");

            obj.Changed += Update;

            _visuals.Add(obj);

            if (!_viewport.Children.Contains(obj.Actual))
            {
                _viewport.Children.Add(obj.Actual);
                if (obj.LastVisual != null)
                    _viewport.Children.Add(obj.LastVisual);
            }

            obj.RequestUpdate();
        }

        public void Remove(DrawerObject obj)
        {
            if (_viewport == null)
                throw new ArgumentNullException("Viewport");

            obj.Changed -= Update;

            _visuals.Remove(obj);

            if (_viewport.Children.Contains(obj.Actual))
            {
                _viewport.Children.Remove(obj.Actual);
                _viewport.Children.Remove(obj.LastVisual);
            }
        }

        public void Update(DrawerObject obj)
        {

            switch (obj.Entity)
            {
                case Face3D:
                    UpdateFace3D(obj, (Face3D)obj.Entity);
                    break;
                case Polyline3D:
                    UpdatePolyline3D(obj, (Polyline3D)obj.Entity);
                    break;
            }

            _viewport?.UpdateLayout();
        }

        private void UpdatePolyline3D(DrawerObject obj, Polyline3D polyline3D)
        {
            if (obj.LastVisual is not ScreenSpaceVisual3D lastVisual)
                return;

            if (obj.Actual is not ScreenSpaceVisual3D actual)
                return;

            lastVisual.Points.Clear();

            foreach (var p in actual.Points)
                lastVisual.Points.Add(p);

            actual.Points.Clear();

            var points = polyline3D.Vertexes
                .Select(v => new Point3D(v.X, v.Y, v.Z))
                .ToList();

            for (int i = 0; i < points.Count - 1; i++)
            {
                actual.Points.Add(points[i]);
                actual.Points.Add(points[i + 1]);
            }
        }

        private void UpdateFace3D(DrawerObject obj, Face3D face3D)
        {
            if (obj.LastVisual is not ScreenSpaceVisual3D lastVisual)
                return;

            if (obj.Actual is not ScreenSpaceVisual3D actual)
                return;

            lastVisual.Points.Clear();

            foreach (var p in actual.Points)
                lastVisual.Points.Add(p);

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
