using GeoAppWpf.Services;
using HelixToolkit.Wpf;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace GeoAppWpf.Controls
{
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
            {
                newController.Attach(control.Viewport);
            }
        }
        #endregion

        private void Viewport_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Point position = e.GetPosition(Viewport);
            var visual = FindVisual(position);

            if (visual == null)
                return;

            ViewportController.ToggleSelection(visual);
        }

        private void Viewport_MouseMove(object sender, MouseEventArgs e)
        {
            Point position = e.GetPosition(Viewport);
            var visual = FindVisual(position);

            if (visual == null)
            {
                ViewportController.Unhover();
                Viewport.Cursor = Cursors.Arrow;
                return;
            }

            ViewportController.Hover(visual);
            Viewport.Cursor = Cursors.Hand;
        }

        private Visual3D? FindVisual(Point position)
        {
            var rect = new Rect(position.X - 5, position.Y - 5, 10, 10);
            var hits = Viewport.Viewport.FindHits(rect, SelectionHitMode.Touch).ToList();

            return hits.Count != 0
                ? hits[0].Visual
                : null;
        }
    }
}
