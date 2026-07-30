using GeoAppWpf.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GeoAppWpf.Pages
{
    public partial class SettingsPage : UserControl
    {
        public SettingsPage()
        {
            InitializeComponent();
            Loaded += async (_, __) =>
            {
                if (DataContext is SettingsViewModel vm)
                {
                    await vm.Init();
                }
            };
        }
    }
}
