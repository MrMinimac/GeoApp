using GeoCadWpf.ViewModels;
using LegendDesignWpf.Controls;
using System.Windows;

namespace GeoCadWpf.Views.Windows
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