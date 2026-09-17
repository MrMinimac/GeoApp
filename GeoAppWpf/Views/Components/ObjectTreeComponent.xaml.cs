using GeoAppWpf.Helpers;
using GeoAppWpf.Interfaces;
using GeoAppWpf.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GeoAppWpf.Views.Components
{
    public partial class ObjectTreeComponent : UserControl
    {
        public ObjectTreeComponent()
        {
            InitializeComponent();
        }

        #region Document Property
        public ObservableCollection<GeoTreeNode> Documents
        {
            get => (ObservableCollection<GeoTreeNode>)GetValue(DocumentsProperty);
            set => SetValue(DocumentsProperty, value);
        }

        public static readonly DependencyProperty DocumentsProperty =
            DependencyProperty.Register(nameof(Documents), typeof(ObservableCollection<GeoTreeNode>),
                typeof(ObjectTreeComponent), new PropertyMetadata(null));
        #endregion

        #region Selected Node Property

        public GeoTreeNode SelectedNode
        {
            get => (GeoTreeNode)GetValue(SelectedNodeProperty);
            set => SetValue(SelectedNodeProperty, value);
        }

        public static readonly DependencyProperty SelectedNodeProperty =
            DependencyProperty.Register(nameof(SelectedNode), typeof(GeoTreeNode),
                typeof(ObjectTreeComponent), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        #endregion

        #region Command Provider Property

        public ITreeCommandProvider CommandProvider
        {
            get => (ITreeCommandProvider)GetValue(CommandProviderProperty);
            set => SetValue(CommandProviderProperty, value);
        }

        public static readonly DependencyProperty CommandProviderProperty =
            DependencyProperty.Register(nameof(CommandProvider), typeof(ITreeCommandProvider),
                typeof(ObjectTreeComponent), new FrameworkPropertyMetadata(null));

        #endregion

        #region TreeViewEvents

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is GeoTreeNode node)
            {
                SelectedNode = node;
            }
        }

        private void GeoTree_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = VisualHelper.FindParent<TreeViewItem>((DependencyObject)e.OriginalSource);

            if (item == null)
                return;

            if (item.DataContext is GeoTreeNode node && node.MenuItems.Count > 0)
            {
                item.IsSelected = true;
                var menu = new ContextMenu();

                foreach (var treeMenuItem in node.MenuItems)
                    menu.Items.Add(CreateMenuItem(treeMenuItem, node));

                menu.PlacementTarget = item;
                menu.IsOpen = true;
            }

            e.Handled = true;
        }

        private MenuItem CreateMenuItem(TreeMenuItem item, GeoTreeNode node)
        {
            var menuItem = new MenuItem
            {
                Header = item.Header,
                Command = item.Command,
                CommandParameter = item.CommandParameter ?? node
            };

            foreach (var child in item.Items)
                menuItem.Items.Add(CreateMenuItem(child, node));

            return menuItem;
        }
        #endregion
    }
}
