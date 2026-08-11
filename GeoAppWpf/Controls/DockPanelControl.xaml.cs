using GeoAppWpf.Models;
using GeoAppWpf.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;

namespace GeoAppWpf.Controls
{
    public partial class DockPanelControl : UserControl
    {
        public DockPanelControl()
        {
            InitializeComponent();
        }

        #region Command Provider Property

        public IReadOnlyList<TreeMenuItem> DockPanelItems
        {
            get => (IReadOnlyList<TreeMenuItem>)GetValue(DockPanelItemsProperty);
            set => SetValue(DockPanelItemsProperty, value);
        }

        public static readonly DependencyProperty DockPanelItemsProperty =
            DependencyProperty.Register(nameof(DockPanelItems), typeof(IReadOnlyList<TreeMenuItem>),
                typeof(DockPanelControl), new FrameworkPropertyMetadata(null, OnDockPanelItemsChanged));

        private static void OnDockPanelItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not DockPanelControl control)
                return;

            control.AddItemsInDockPanel();
        }

        private void AddItemsInDockPanel()
        {
            if (DockPanelItems == null)
                return;

            DockPanel.Items.Clear();

            foreach (var items in DockPanelItems)
                DockPanel.Items.Add(CreateMenuItem(items));
        }

        private MenuItem CreateMenuItem(TreeMenuItem item)
        {
            var menuItem = new MenuItem
            {
                Header = item.Header,
                Command = item.Command,
                CommandParameter = item.CommandParameter
            };

            foreach (var child in item.Items)
                menuItem.Items.Add(CreateMenuItem(child));

            return menuItem;
        }

        #endregion
    }
}
