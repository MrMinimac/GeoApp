using GeoAppCore.Services;
using GeoAppWpf.Models;
using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GeoAppWpf.Controls
{
    public partial class GeoTableControl : UserControl
    {
        public GeoTableControl()
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
                typeof(GeoTableControl), new PropertyMetadata(null));
        #endregion

        #region Selected Node Property
        public GeoTreeNode SelectedNode
        {
            get => (GeoTreeNode)GetValue(SelectedNodeProperty);
            set => SetValue(SelectedNodeProperty, value);
        }

        public static readonly DependencyProperty SelectedNodeProperty =
            DependencyProperty.Register(nameof(SelectedNode), typeof(GeoTreeNode),
                typeof(GeoTableControl), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        #endregion

        #region Display Items Property
        public IEnumerable DisplayItems
        {
            get => (IEnumerable)GetValue(DisplayItemsProperty);
            set => SetValue(DisplayItemsProperty, value);
        }

        public static readonly DependencyProperty DisplayItemsProperty =
            DependencyProperty.Register(nameof(DisplayItems), typeof(IEnumerable),
                typeof(GeoTableControl), new PropertyMetadata(null));
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
            var item = FindParent<TreeViewItem>((DependencyObject)e.OriginalSource);

            if (item == null)
                return;

            if (item.DataContext is EntitiesNode)
            {
                item.IsSelected = true;

                var menu = new ContextMenu();

                var open3D = new MenuItem
                {
                    Header = "Открыть 3D просмотр"
                };

                open3D.Click += Open3D_Click;

                menu.Items.Add(open3D);

                menu.PlacementTarget = item;
                menu.IsOpen = true;
            }

            e.Handled = true;
        }

        private void Open3D_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu menu && menu.PlacementTarget is TreeViewItem item)
            {
                if (GeoTree.SelectedItem is EntitiesNode node)
                {
                    var viewer = new Viewer3D(new DXFDrawer(node.Entity));
                    viewer.Show();
                }
            }
        }

        #endregion

        #region DataGridEvents

        private void PropertiesDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            
        }

        private void PropertiesDataGrid_AutoGeneratingColumn(object sender, System.Windows.Controls.DataGridAutoGeneratingColumnEventArgs e)
        {
            var info = LocaleService.GetColumnInfo(e.PropertyName);

            if (info == null)
            {
                e.Column.Header = e.PropertyName;
                return;
            }

            if (!info.Visible)
            {
                e.Cancel = true;
                return;
            }


            if (info.CellTemplate != null)
            {
                var templateColumn = new DataGridTemplateColumn
                {
                    Header = info.Header,
                    CellTemplate = (DataTemplate)FindResource("ColorTemplate")
                };

                e.Column = templateColumn;
            }
            else
            {
                e.Column.Header = info.Header;
            }
        }
        #endregion

        private static T? FindParent<T>(DependencyObject obj) where T : DependencyObject
        {
            while (obj != null)
            {
                if (obj is T result)
                    return result;

                obj = VisualTreeHelper.GetParent(obj);
            }

            return null;
        }
    }
}
