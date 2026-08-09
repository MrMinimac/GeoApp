using GeoCadWpf.Services;
using HelixToolkit.Wpf;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace GeoCadWpf.Views.Controls
{
    public partial class ViewportControl : UserControl
    {
        public ViewportControl()
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
                typeof(ViewportControl), new FrameworkPropertyMetadata(null, OnDrawerChanged));

        private static void OnDrawerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ViewportControl control)
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
            if (e.ChangedButton != MouseButton.Left)
                return;

            Point position = e.GetPosition(Viewport);
            var visual = FindVisual(position);

            if (visual == null)
                return;

            ViewportController.ToggleSelection(visual);
        }

        private void Viewport_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var menu = new ContextMenu();

            menu.Items.Add(new MenuItem
            {
                Header = "Удалить"
            });

            menu.Items.Add(new MenuItem
            {
                Header = "Свойства"
            });

            menu.IsOpen = true;

            e.Handled = true;
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
