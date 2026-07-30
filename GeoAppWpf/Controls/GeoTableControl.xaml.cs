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
            switch (e.PropertyName)
            {
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

                default:
                    e.Column.Header = e.PropertyName;
                    break;
            }
        }

        private void BoreholesDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Borehole.Id):
                    e.Column.Header = "№";
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

                case nameof(Borehole.AvgValue):
                    e.Column.Header = "Ср. сод.";
                    var column = (DataGridTextColumn)e.Column;
                    column.Binding = new Binding(e.PropertyName)
                    {
                        Converter = new ValueConverter()
                    };
                    break;

                default:
                    e.Column.Header = e.PropertyName;
                    break;
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
            switch (e.PropertyName)
            {
                case nameof(Sample.Diametr):
                case nameof(Sample.Fineness):
                case nameof(Sample.X):
                case nameof(Sample.Y):
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
    }
}
