using GeoAppCore.Services;
using GeoAppWpf.Models;
using Microsoft.Win32;
using netDxf.Entities;
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

            if (item.DataContext is DxfDocumentNode)
            {
                item.IsSelected = true;

                var menu = new ContextMenu();

                var open3D = new MenuItem
                {
                    Header = "Открыть 3D просмотр"
                };
                open3D.Click += Open3D_All_Click;


                var save = new MenuItem
                {
                    Header = "Сохранить как"
                };


                var saveDxf = new MenuItem
                {
                    Header = "DXF (AutoCAD)"
                };
                saveDxf.Click += SaveDxf_Click;


                var saveDat = new MenuItem
                {
                    Header = "DAT (Micromine)"
                };
                saveDat.Click += SaveDat_Click;


                save.Items.Add(saveDxf);
                save.Items.Add(saveDat);


                menu.Items.Add(open3D);
                menu.Items.Add(save);

                menu.PlacementTarget = item;
                menu.IsOpen = true;
            }

            e.Handled = true;
        }

        private void SaveDat_Click(object sender, RoutedEventArgs e)
        {
            if (GeoTree.SelectedItem is not DxfDocumentNode node)
                return;

            var dialog = new SaveFileDialog
            {
                Filter = "DAT files (*.dat)|*.dat",
                DefaultExt = ".dat",
                FileName = node.Name + ".dat"
            };

            if (dialog.ShowDialog() == true)
                node.Save(dialog.FileName);
        }

        private void SaveDxf_Click(object sender, RoutedEventArgs e)
        {
            if (GeoTree.SelectedItem is not DxfDocumentNode node)
                return;

            var dialog = new SaveFileDialog
            {
                Filter = "DXF files (*.dxf)|*.dxf",
                DefaultExt = ".dxf",
                FileName = node.Name + ".dxf"
            };

            if (dialog.ShowDialog() == true)
                node.Document.Save(dialog.FileName);
        }

        private void Open3D_All_Click(object sender, RoutedEventArgs e)
        {
            if (GeoTree.SelectedItem is DxfDocumentNode doc)
            {
                var list = new List<EntityObject>();

                foreach (var node in doc.Entities)
                {
                    if (node.Entity is Polyline3D pl)
                        list.Add(node.Entity);
                }

                var viewer = new Viewer3D(new DXFDrawer(list));
                viewer.Show();
            }
        }

        private void Open3D_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu menu && menu.PlacementTarget is TreeViewItem item)
            {
                if (GeoTree.SelectedItem is EntitiesNode node)
                {
                    var viewer = new Viewer3D(new DXFDrawer([node.Entity]));
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
