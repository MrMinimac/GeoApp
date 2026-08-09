using GeoCadWpf.MVVM;
using LegendDesignWpf.Core.MVVM;
using Microsoft.Extensions.DependencyInjection;

namespace GeoCadWpf.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly SettingsViewModel _settingsViewModel;
        private readonly HomeViewModel _homeViewModel;
        private NavigationPage _selectedPage;
        private object _currentPage;

        public QuickPanelViewModel QuickPanelViewModel { get; }

        public object CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage == value)
                    return;

                _currentPage = value;
                OnPropertyChanged();
            }
        }

        public NavigationPage SelectedPage
        {
            get => _selectedPage;
            set
            {
                if (_selectedPage != value)
                {
                    _selectedPage = value;
                    Navigate(value);
                }
            }
        }

        public MainViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _homeViewModel = _serviceProvider.GetRequiredService<HomeViewModel>();
            _settingsViewModel = _serviceProvider.GetRequiredService<SettingsViewModel>();

            QuickPanelViewModel = serviceProvider.GetRequiredService<QuickPanelViewModel>();

            _currentPage = _homeViewModel;
            CurrentPage = _currentPage;
        }

        private void Navigate(NavigationPage page)
        {
            CurrentPage = page switch
            {
                NavigationPage.Home => _homeViewModel,
                NavigationPage.Settings => _settingsViewModel,
                _ => _homeViewModel
            };
        }
    }
}
