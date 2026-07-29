using GeoAppCore;
using GeoAppWpf.Converters;
using GeoAppWpf.Services;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace GeoAppWpf
{
    public partial class MainWindow : Window
    {
        private GeoDoc? _curDoc;

        public MainWindow()
        {
            InitializeComponent();
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
                e.PropertyName == nameof(BoreholeLine.Boreholes))
            {
                e.Cancel = true;
            }
        }

        private void BoreholesDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
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

        #region ButtonEvents

        private void ExcelLoadButton_Click(object sender, RoutedEventArgs e)
        {
            var el = new ExcelService();
            _curDoc = el.Load();

            if (_curDoc == null)
                return;

            BoreholeLinesDataGrid.ItemsSource = _curDoc.BoreholeLines;
            BoreholeLinesDataGrid.SelectedItem = BoreholeLinesDataGrid.Items[0];
            BoreholesDataGrid.SelectedItem = BoreholesDataGrid.Items[0];
            DataGrids.Visibility = Visibility.Visible;
        }

        private async void ImportInAcadButton_Click(object sender, RoutedEventArgs e)
        {
            ImportInCad(1);
        }

        private async void ImportInAcadButton_Click2(object sender, RoutedEventArgs e)
        {
            ImportInCad(2);
        }

        #endregion

        private async Task SendToAutoCAD(GeoDoc doc, int id)
        {
            using HttpClient client = new HttpClient();

            string json = doc.ToJson();

            var content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");


            var endpoint = id switch
            {
                2 => "import2",
                _ => "import",
            };

            HttpResponseMessage response =
                await client.PostAsync(
                    $"http://localhost:5050/{endpoint}",
                    content);

            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show(
                    "Проект отправлен в AutoCAD");
            }
        }

        private async void ImportInCad(int id)
        {
            if (_curDoc == null)
            {
                MessageBox.Show("Документ пуст.");
                return;
            }

            try
            {
                await SendToAutoCAD(_curDoc, id);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка при импорте.\n" +
                    $"Убедитесь что у вас запущен AutoCad и загружен плагин. " +
                    $"\n\n{ex.Message}", "Ошибка!");

                Debug.WriteLine(ex.ToString());
            }
        }
    }
}