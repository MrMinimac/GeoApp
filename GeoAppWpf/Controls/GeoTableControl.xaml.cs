using GeoAppCore;
using GeoAppWpf.Converters;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace GeoAppWpf.Controls
{
    public partial class GeoTableControl : UserControl
    {
        public GeoTableControl()
        {
            InitializeComponent();
        }

        #region Document Property
        public GeoDoc Document
        {
            get => (GeoDoc)GetValue(DocumentProperty);
            set => SetValue(DocumentProperty, value);
        }

        public static readonly DependencyProperty DocumentProperty =
            DependencyProperty.Register(nameof(Document), typeof(GeoDoc), 
                typeof(GeoTableControl), new PropertyMetadata(null, OnDocumentChanged));
        #endregion

        private static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as GeoTableControl;

            if (control == null)
                return;

            if (control.Document != null && control.Document.BoreholeLines.Count > 0)
            {
                control.BoreholeLinesDataGrid.ItemsSource = control.Document?.BoreholeLines;
                control.BoreholeLinesDataGrid.SelectedItem = control.BoreholeLinesDataGrid.Items[0];
                control.BoreholesDataGrid.SelectedItem = control.BoreholesDataGrid.Items[0];
            }
        }

        #region DataGridEvents

        private void BoreholeLinesDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (BoreholeLinesDataGrid.SelectedItem is BoreholeLine line)
            {
                BoreholesDataGrid.ItemsSource = line.Boreholes;
            }
        }

        private void BoreholeLinesDataGrid_AutoGeneratingColumn(object sender, System.Windows.Controls.DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == nameof(BoreholeLine.First) ||
                e.PropertyName == nameof(BoreholeLine.Last) ||
                e.PropertyName == nameof(BoreholeLine.Boreholes) )
            {
                e.Cancel = true;
            }
        }

        private void BoreholesDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == nameof(Borehole.LithologyIntervals))
            {
                e.Cancel = true;
            }

            if (e.PropertyName is nameof(Borehole.AvgValue))
            {
                var column = (DataGridTextColumn)e.Column;

                column.Binding = new Binding(e.PropertyName)
                {
                    Converter = new ValueConverter()
                };
            }
        }

        private void BoreholesDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (BoreholesDataGrid.SelectedItem is Borehole borehole)
            {
                SamplesDataGrid.ItemsSource = borehole.Samples;
            }
        }

        private void SamplesDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName is nameof(Sample.Value)
                or nameof(Sample.AvgValue)
                or nameof(Sample.VertReserve)
                or nameof(Sample.CleanedAvgValue)
                or nameof(Sample.CleanedVertReserve))
            {
                var column = (DataGridTextColumn)e.Column;

                column.Binding = new Binding(e.PropertyName)
                {
                    Converter = new ValueConverter()
                };
            }

            if (e.PropertyName is nameof(Sample.Lithologies))
            {
                var column = (DataGridTextColumn)e.Column;

                column.Binding = new Binding(e.PropertyName)
                {
                    Converter = new LitologiesConverter()
                };
            }
        }

        #endregion
    }
}
