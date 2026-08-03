using GeoAppWpf.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace GeoAppWpf.Controls
{
    public partial class QuickPanelControl : UserControl
    {
        public QuickPanelControl()
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
