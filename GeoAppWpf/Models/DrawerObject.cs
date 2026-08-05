using HelixToolkit.Wpf;
using netDxf.Entities;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace GeoAppWpf.Models
{
    public class DrawerObject
    {
        public Color Color { get; set; } = Colors.White;
        public EntityObject Entity { get; private set; }
        public Visual3D Actual { get; set; }
        public Visual3D LastVisual { get; set; }

        public event Action<DrawerObject>? Changed;

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

            SetVisualColor(Actual, color);
        }

        public DrawerObject(EntityObject entity)
        {
            Entity = entity;

            if (Entity is Polyline3D pl)
            {
                var dxfColor = pl.Color;
                Color = Color.FromArgb(255, dxfColor.R, dxfColor.G, dxfColor.B);

                Actual = new LinesVisual3D
                {
                    Color = Color
                };

                LastVisual = new LinesVisual3D
                {
                    Color = GetDarkColor(Color)
                };
            }
            else if (Entity is Face3D face)
            {
                var dxfColor = face.Color;
                Color = Color.FromArgb(255, dxfColor.R, dxfColor.G, dxfColor.B);

                Actual = new LinesVisual3D
                {
                    Color = Color
                };

                LastVisual = new LinesVisual3D
                {
                    Color = GetDarkColor(Color)
                };
            }
            else if (Entity is Point point)
            {
                var dxfColor = point.Color;
                Color = Color.FromArgb(255, dxfColor.R, dxfColor.G, dxfColor.B);

                Actual = new LinesVisual3D
                {
                    Color = Color
                };

                LastVisual = new LinesVisual3D
                {
                    Color = GetDarkColor(Color)
                };
            }
            else
            {
                throw new Exception("Supported only Polyline3D and Face3D");
            }
        }

        public DrawerObject(Visual3D visual)
        {
            if (visual == null)
                throw new ArgumentNullException(nameof(visual));

            // Устанавливаем базовый Visual3D для всех типов объектов
            Actual = visual;

            // Теперь проверка 'is ScreenSpaceVisual3D' сработает корректно, 
            // так как проверятся базовый тип Visual3D
            if (visual is ScreenSpaceVisual3D scrVisual)
            {
                LastVisual = scrVisual;
                SetVisualColor(LastVisual, GetDarkColor(scrVisual.Color));
            }
            else
            {
                LastVisual = null;
            }
        }

        public void RequestUpdate()
        {
            Changed?.Invoke(this);
        }

        private Color GetDarkColor(Color color)
        {
            var R = (byte)(color.R - 50);
            var G = (byte)(color.G - 50);
            var B = (byte)(color.B - 50);

            if (R < 0) R = 0;
            if (G < 0) G = 0;
            if (B < 0) B = 0;

            return Color.FromArgb(100, R, G, B);
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
