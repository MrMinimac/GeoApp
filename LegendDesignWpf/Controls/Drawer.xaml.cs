using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace LegendDesignWpf.Controls
{
    public partial class Drawer : UserControl
    {
        public Drawer()
        {
            InitializeComponent();

            Loaded += (_, __) =>
            {
                UpdateState(IsOpen);
            };
        }

        #region IsOpenProperty
        public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(nameof(IsOpen),
            typeof(bool), typeof(Drawer), new PropertyMetadata(false, OnIsOpenChanged));

        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }
        #endregion

        #region DrawerContentProperty
        public static readonly DependencyProperty DrawerContentProperty = DependencyProperty.Register(nameof(DrawerContent),
            typeof(object), typeof(Drawer), new PropertyMetadata(null));

        public object DrawerContent
        {
            get => GetValue(DrawerContentProperty);
            set => SetValue(DrawerContentProperty, value);
        }
        #endregion

        #region ActiveBrush
        public static readonly DependencyProperty BorderBackgroundProperty =
            DependencyProperty.Register(nameof(BorderBackground), typeof(Brush),
                typeof(Drawer), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(255, 255, 255))));

        public Brush BorderBackground
        {
            get => (Brush)GetValue(BorderBackgroundProperty);
            set => SetValue(BorderBackgroundProperty, value);
        }
        #endregion

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var drawer = (Drawer)d;
            drawer.UpdateState((bool)e.NewValue);
        }

        private void UpdateState(bool isOpen)
        {
            var storyboard = (Storyboard)Resources[isOpen ? "ShowQueueAnimation" : "HideQueueAnimation"];

            storyboard.Begin(this, true);
        }

        private void Drawer_Loaded(object sender, RoutedEventArgs e)
        {
            PanelTransform.Y = ActualHeight;
        }
    }
}
