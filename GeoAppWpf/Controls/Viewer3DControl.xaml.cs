using HelixToolkit.Wpf;
using netDxf.Entities;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace GeoAppWpf.Controls
{
    public class ViewportController
    {
        private HelixViewport3D? _viewport;
        private readonly ObservableCollection<DrawerObject> _visuals = new();

        public IReadOnlyCollection<DrawerObject> Visuals => _visuals;

        public bool IsAttached => _viewport != null;

        public void Attach(HelixViewport3D viewport)
        {
            _viewport = viewport;
        }

        public void Detach()
        {
            _viewport = null;
        }

        public void Add(DrawerObject obj)
        {
            if (_viewport == null)
                throw new ArgumentNullException("Viewport");

            obj.Changed += Update;

            _visuals.Add(obj);

            if (!_viewport.Children.Contains(obj.Actual))
            {
                _viewport.Children.Add(obj.Actual);
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
            obj.LastVisual.Points.Clear();

            foreach (var p in obj.Actual.Points)
                obj.LastVisual.Points.Add(p);

            obj.Actual.Points.Clear();

            var points = polyline3D.Vertexes
                .Select(v => new Point3D(v.X, v.Y, v.Z))
                .ToList();

            for (int i = 0; i < points.Count - 1; i++)
            {
                obj.Actual.Points.Add(points[i]);
                obj.Actual.Points.Add(points[i + 1]);
            }
        }

        private void UpdateFace3D(DrawerObject obj, Face3D face3D)
        {
            obj.LastVisual.Points.Clear();

            foreach (var p in obj.Actual.Points)
                obj.LastVisual.Points.Add(p);

            obj.Actual.Points.Clear();

            var p1 = new Point3D(face3D.FirstVertex.X, face3D.FirstVertex.Y, face3D.FirstVertex.Z);
            var p2 = new Point3D(face3D.SecondVertex.X, face3D.SecondVertex.Y, face3D.SecondVertex.Z);
            var p3 = new Point3D(face3D.ThirdVertex.X, face3D.ThirdVertex.Y, face3D.ThirdVertex.Z);

            // Добавляем 3 линии (6 точек), чтобы нарисовать контур треугольника
            // Линия 1
            obj.Actual.Points.Add(p1);
            obj.Actual.Points.Add(p2);
            // Линия 2
            obj.Actual.Points.Add(p2);
            obj.Actual.Points.Add(p3);
            // Линия 3 (замыкаем обратно на первую вершину)
            obj.Actual.Points.Add(p3);
            obj.Actual.Points.Add(p1);
        }

        public void Update()
        {
            foreach (var visual in _visuals)
            {
                if (visual.Entity is Face3D face3D)
                {
                    visual.LastVisual.Points.Clear();

                    foreach (var p in visual.Actual.Points)
                        visual.LastVisual.Points.Add(p);

                    visual.Actual.Points.Clear();

                    var p1 = new Point3D(face3D.FirstVertex.X, face3D.FirstVertex.Y, face3D.FirstVertex.Z);
                    var p2 = new Point3D(face3D.SecondVertex.X, face3D.SecondVertex.Y, face3D.SecondVertex.Z);
                    var p3 = new Point3D(face3D.ThirdVertex.X, face3D.ThirdVertex.Y, face3D.ThirdVertex.Z);

                    // Добавляем 3 линии (6 точек), чтобы нарисовать контур треугольника
                    // Линия 1
                    visual.Actual.Points.Add(p1);
                    visual.Actual.Points.Add(p2);
                    // Линия 2
                    visual.Actual.Points.Add(p2);
                    visual.Actual.Points.Add(p3);
                    // Линия 3 (замыкаем обратно на первую вершину)
                    visual.Actual.Points.Add(p3);
                    visual.Actual.Points.Add(p1);
                }

                if (visual.Entity is Polyline3D polyline3D)
                {
                    visual.LastVisual.Points.Clear();

                    foreach (var p in visual.Actual.Points)
                        visual.LastVisual.Points.Add(p);

                    visual.Actual.Points.Clear();

                    var points = polyline3D.Vertexes
                        .Select(v => new Point3D(v.X, v.Y, v.Z))
                        .ToList();

                    for (int i = 0; i < points.Count - 1; i++)
                    {
                        visual.Actual.Points.Add(points[i]);
                        visual.Actual.Points.Add(points[i + 1]);
                    }
                }
            }

            _viewport?.UpdateLayout();
        }
    }

    public partial class Viewer3DControl : UserControl
    {
        public Viewer3DControl()
        {
            InitializeComponent();
        }

        #region Selected Node Property
        public ViewportController ViewportController
        {
            get => (ViewportController)GetValue(ViewportControllerProperty);
            set => SetValue(ViewportControllerProperty, value);
        }

        public static readonly DependencyProperty ViewportControllerProperty =
            DependencyProperty.Register(nameof(ViewportController), typeof(ViewportController),
                typeof(Viewer3DControl), new FrameworkPropertyMetadata(null, OnDrawerChanged));

        private static void OnDrawerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Viewer3DControl control)
                return;

            if (e.OldValue is ViewportController oldController)
                oldController.Detach();

            if (e.NewValue is ViewportController newController)
                newController.Attach(control.Viewport);
        }
        #endregion
    }
}
