using GeoAppCore.Services;
using GeoAppWpf.Helpers;
using GeoAppWpf.Models;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace GeoAppWpf.Views.Components
{
    public partial class TableComponent : UserControl
    {
        public TableComponent()
        {
            InitializeComponent();
        }

        #region Display Items Property

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(IEnumerable),
                typeof(TableComponent),
                new PropertyMetadata(null));

        #endregion

        private void PropertiesDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            var info = ColumnData.GetColumnInfo(e.PropertyName);

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

            e.Column.Header = info.Header;

            if (info.CellTemplate != null)
            {
                var templateColumn = new DataGridTemplateColumn
                {
                    Header = info.Header,
                    CellTemplate = (DataTemplate)FindResource("ColorTemplate")
                };

                e.Column = templateColumn;
            }
            else if (info.Converter != null && e.Column is DataGridTextColumn textColumn)
            {
                textColumn.Binding = new Binding(e.PropertyName)
                {
                    Converter = info.Converter
                };
            }
        }
    }
}
