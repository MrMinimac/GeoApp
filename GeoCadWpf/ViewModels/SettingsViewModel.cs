using GeoCad.Infrastructure.Services;
using GeoCadWpf.Helpers;
using LegendDesignWpf.Core;
using LegendDesignWpf.Core.Enums;
using LegendDesignWpf.Core.MVVM;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace GeoCadWpf.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        #region Private Fields
        private AccentSource _accentSource;
        private AppThemes _appTheme;

        private readonly IServiceProvider _serviceProvider;
        private bool _initializing;
        private readonly SettingsService _settings;
        #endregion

        #region Collections
        public ObservableCollection<AppThemes> ThemeValues { get; } = new(Enum.GetValues<AppThemes>());
        public ObservableCollection<AccentSource> AccentSourceValues { get; } = new(Enum.GetValues<AccentSource>());
        #endregion

        #region Properties

        public AccentSource AccentSource
        {
            get => _accentSource;
            set
            {
                if (_accentSource == value)
                    return;

                _accentSource = value;
                OnPropertyChanged();
            }
        }

        public AppThemes AppTheme
        {
            get => _appTheme;
            set
            {
                if (_appTheme == value)
                    return;

                _appTheme = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Commands

        public ICommand ChangeThemeCommand => new RelayCommand<AppThemes>(async (theme) =>
        {
            if (LegendDesign.Theme.CanHotSwap(theme))
            {
                _settings.AppTheme = theme;
                LegendDesign.Theme.ApplyTheme(theme);
            }
            else
            {
                _settings.AppTheme = theme;
                LegendDesign.Theme.ApplyTheme(theme);
                RestartHelper.Restart();
            }
        });

        public ICommand ChangeAccentSourceCommand => new RelayCommand<AccentSource>(async (source) =>
        {
            _settings.AccentSource = source;
            LegendDesign.Theme.SetAccentSource(source);
        });

        #endregion

        public SettingsViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _settings = _serviceProvider.GetRequiredService<SettingsService>();
        }

        public async Task Init()
        {
            _initializing = true;

            AppTheme = _settings.AppTheme;
            AccentSource = _settings.AccentSource;

            _initializing = false;
        }
    }
}
