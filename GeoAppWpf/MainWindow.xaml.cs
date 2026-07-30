using GeoAppWpf.ViewModels;
using LegendDesignWpf.Controls;

namespace GeoAppWpf
{
    public partial class MainWindow : LDWindow
    {
        // private readonly ACadService _acs;

        public MainWindow(MainViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            // _acs = new ACadService();
        }

        #region ButtonEvents

        //private void ExcelLoadButton_Click(object sender, RoutedEventArgs e)
        //{
        //    var ok = _acs.LoadDocument();

        //    if (!ok)
        //        return;

        //    BoreholeLinesDataGrid.ItemsSource = _acs.Document?.BoreholeLines;
        //    BoreholeLinesDataGrid.SelectedItem = BoreholeLinesDataGrid.Items[0];
        //    BoreholesDataGrid.SelectedItem = BoreholesDataGrid.Items[0];
        //    DataGrids.Visibility = Visibility.Visible;
        //}

        //private async void ImportInAcadButton_Click(object sender, RoutedEventArgs e) => _acs.Import(1);
        //private async void ImportInAcadButton_Click2(object sender, RoutedEventArgs e) => _acs.Import(2);

        #endregion


    }
}