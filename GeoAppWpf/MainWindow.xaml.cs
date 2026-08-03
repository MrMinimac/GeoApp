using GeoAppWpf.ViewModels;
using LegendDesignWpf.Controls;

namespace GeoAppWpf
{
    public partial class MainWindow : LDWindow
    {
        public MainWindow(MainViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}