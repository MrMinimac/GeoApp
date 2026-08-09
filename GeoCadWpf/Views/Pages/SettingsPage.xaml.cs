using GeoCadWpf.ViewModels;
using System.Windows.Controls;

namespace GeoCadWpf.Views.Pages
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
