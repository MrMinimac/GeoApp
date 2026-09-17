using GeoAppCore.Services;
using GeoAppWpf.Models;
using System.Collections;
using System.Windows;
using System.Windows.Controls;

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
    }
}
