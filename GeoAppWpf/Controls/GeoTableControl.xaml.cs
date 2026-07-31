using GeoAppCore;
using GeoAppWpf.Converters;
using GeoAppWpf.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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

        #region TreeViewEvents

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is GeoTreeNode node)
            {
                switch (node)
                {
                    case BoreholeLineNode lineNode:
                        PropertiesDataGrid.ItemsSource = lineNode.Line.Boreholes;
                        break;

                    case BoreholeNode boreholeNode:
                        PropertiesDataGrid.ItemsSource = boreholeNode.Borehole.Samples;
                        break;

                    case GeoDocumentNode doc:
                        PropertiesDataGrid.ItemsSource = doc.Document.BoreholeLines;
                        break;

                    case DxfDocumentNode dxfdoc:
                        PropertiesDataGrid.ItemsSource = dxfdoc.Entities;
                        break;

                    case EntitiesNode entities:
                        PropertiesDataGrid.ItemsSource = entities.Vertexes;
                        break;

                    //case SampleNode sampleNode:
                    //    PropertiesDataGrid.ItemsSource = sampleNode.Sample;
                    //    break;
                }
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
            if (e.PropertyName == "Color")
            {
                var templateColumn = new DataGridTemplateColumn
                {
                    Header = "Цвет",
                    CellTemplate = (DataTemplate)FindResource("ColorTemplate")
                };

                e.Column = templateColumn;
            }

            switch (e.PropertyName)
            {
                case nameof(Borehole.Id):
                case nameof(BoreholeLine.Number):
                    e.Column.Header = "№";
                    break;

                case nameof(BoreholeLine.Azimuth):
                    e.Column.Header = "Азимут";
                    break;

                case nameof(BoreholeLine.First):
                case nameof(BoreholeLine.Last):
                case nameof(BoreholeLine.Boreholes):
                case nameof(BoreholeLine.MinZ):
                case nameof(BoreholeLine.MaxZ):
                    e.Cancel = true;
                    break;

                case nameof(Borehole.Deapth):
                    e.Column.Header = "Глубина";
                    break;

                case nameof(Borehole.SamplesCount):
                    e.Column.Header = "Кол-во проб";
                    break;

                case nameof(Borehole.LithologyIntervals):
                case nameof(Borehole.LineNumber):
                case nameof(Borehole.Key):
                    e.Cancel = true;
                    break;

                case nameof(Sample.Z):
                    e.Column.Header = "Абс. отм.";
                    break;

                case nameof(Sample.Length):
                    e.Column.Header = "Длинна";
                    break;

                case nameof(Sample.From):
                    e.Column.Header = "От";
                    break;

                case nameof(Sample.To):
                    e.Column.Header = "До";
                    break;

                case nameof(Sample.Capacity):
                    e.Column.Header = "Объем";
                    break;

                case nameof(Sample.Value):
                    GenerateValueColumn(e, new ValueConverter(), "Сод.");
                    break;

                case nameof(Sample.AvgValue):
                    GenerateValueColumn(e, new ValueConverter(), "Ср. сод.");
                    break;

                case nameof(Sample.VertReserve):
                    GenerateValueColumn(e, new ValueConverter(), "Верт. запас");
                    break;

                case nameof(Sample.CleanedAvgValue):
                    GenerateValueColumn(e, new ValueConverter(), "Ср. сод. (чист.)");
                    break;

                case nameof(Sample.CleanedVertReserve):
                    GenerateValueColumn(e, new ValueConverter(), "Верт. запас (чист.)");
                    break;

                case nameof(Sample.Lithologies):
                    GenerateValueColumn(e, new LitologiesConverter(), "Литология");
                    break;

                default:
                    e.Column.Header = e.PropertyName;
                    break;
            }
        }
        private static void GenerateValueColumn(DataGridAutoGeneratingColumnEventArgs e, IValueConverter converter, string headerText)
        {
            e.Column.Header = headerText;
            var column = (DataGridTextColumn)e.Column;

            column.Binding = new Binding(e.PropertyName)
            {
                Converter = converter
            };
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
