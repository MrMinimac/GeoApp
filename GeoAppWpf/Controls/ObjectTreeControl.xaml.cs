using GeoAppWpf.Helpers;
using GeoAppWpf.Models;
using GeoAppWpf.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GeoAppWpf.Controls
{
    public partial class ObjectTreeControl : UserControl
    {
        public ObjectTreeControl()
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
                typeof(ObjectTreeControl), new PropertyMetadata(null));
        #endregion

        #region Selected Node Property

        public GeoTreeNode SelectedNode
        {
            get => (GeoTreeNode)GetValue(SelectedNodeProperty);
            set => SetValue(SelectedNodeProperty, value);
        }

        public static readonly DependencyProperty SelectedNodeProperty =
            DependencyProperty.Register(nameof(SelectedNode), typeof(GeoTreeNode),
                typeof(ObjectTreeControl), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        
        #endregion

        #region Command Provider Property

        public ITreeCommandProvider CommandProvider
        {
            get => (ITreeCommandProvider)GetValue(CommandProviderProperty);
            set => SetValue(CommandProviderProperty, value);
        }

        public static readonly DependencyProperty CommandProviderProperty =
            DependencyProperty.Register(nameof(CommandProvider), typeof(ITreeCommandProvider),
                typeof(ObjectTreeControl), new FrameworkPropertyMetadata(null));

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
                    menu.Items.Add(new MenuItem
                    {
                        Header = treeMenuItem.Header,
                        Command = treeMenuItem.Command,
                        CommandParameter = treeMenuItem.CommandParameter ?? node
                    });

                menu.PlacementTarget = item;
                menu.IsOpen = true;
            }

            e.Handled = true;
        }
        #endregion
    }
}
