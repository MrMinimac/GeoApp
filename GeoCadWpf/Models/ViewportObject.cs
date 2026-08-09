using GeoAppCore.Objects;
using HelixToolkit.Wpf;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace GeoCadWpf.Models
{
    public class ViewportObject
    {
        public Color Color { get; set; }

        public WorkspaceObject? Entity { get; private set; }
        public Visual3D Visual { get; set; }

        public object? Tag { get; set; }

        public event Action<ViewportObject>? Changed;

        private bool _selected;
        public bool IsSelected
        {
            get => _selected;
            set
            {
                _selected = value;
                UpdateColor();
            }
        }

        public bool IsVisible { get; set; } = true;

        private bool _isHovered;
        public bool IsHovered
        {
            get => _isHovered;
            set
            {
                _isHovered = value;
                UpdateColor();
            }
        }

        private void UpdateColor()
        {
            var color = _isHovered
                ? Colors.LightSkyBlue
                : _selected
                    ? Colors.Blue
                    : Color;

            SetVisualColor(Visual, color);
        }

        public ViewportObject(WorkspaceObject entity)
        {
            Entity = entity;

            if (Entity is PolylineObject pl)
            {
                var dxfColor = pl.Color;
                Color = Color.FromArgb(255, dxfColor.R, dxfColor.G, dxfColor.B);

                Visual = new LinesVisual3D
                {
                    Color = Color
                };
            }
            else if (Entity is FaceObject face)
            {
                var dxfColor = face.Color;
                Color = Color.FromArgb(255, dxfColor.R, dxfColor.G, dxfColor.B);

                Visual = new LinesVisual3D
                {
                    Color = Color
                };
            }
            else if (Entity is PointObject point)
            {
                var dxfColor = point.Color;
                Color = Color.FromArgb(255, dxfColor.R, dxfColor.G, dxfColor.B);

                Visual = new LinesVisual3D
                {
                    Color = Color
                };
            }
            else
            {
                throw new Exception("Supported only Polyline3D and Face3D");
            }
        }

        public ViewportObject(Visual3D visual)
        {
            if (visual == null)
                throw new ArgumentNullException(nameof(visual));

            Visual = visual;
        }

        public void RequestUpdate()
        {
            Changed?.Invoke(this);
        }

        private void SetVisualColor(Visual3D visual, Color color)
        {
            if (visual == null) return;

            // 1. Для линий, точек и ScreenSpace объектов HelixToolkit
            if (visual is ScreenSpaceVisual3D scrVisual)
            {
                scrVisual.Color = color;
            }
            // 2. Для сплошных 3D-сеток (ModelVisual3D)
            else if (visual is ModelVisual3D modelVisual)
            {
                if (modelVisual.Content is GeometryModel3D geomModel)
                {
                    var brush = new SolidColorBrush(color) { Opacity = color.A / 255.0 };
                    var materialGroup = new MaterialGroup();
                    materialGroup.Children.Add(new DiffuseMaterial(brush));
                    materialGroup.Children.Add(new SpecularMaterial(Brushes.White, 30));

                    geomModel.Material = materialGroup;
                    geomModel.BackMaterial = materialGroup;
                }
            }
        }
    }
}
