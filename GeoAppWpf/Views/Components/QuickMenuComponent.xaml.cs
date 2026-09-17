using System.Windows;
using System.Windows.Controls;

namespace GeoAppWpf.Views.Components
{
    public partial class QuickMenuComponent : UserControl
    {
        public QuickMenuComponent()
        {
            InitializeComponent();
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Control)sender;
            if (btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }
    }
}
