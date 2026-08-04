using GeoAppCore.Services;
using GeoAppWpf.Services;
using GeoAppWpf.TestServices;
using GeoAppWpf.ViewModels;
using LegendDesignWpf.Core;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Windows;

namespace GeoAppWpf
{
    public partial class App : Application
    {
        public const string APP_NAME = "GeoApp";
        public string AppDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), APP_NAME);
        private ServiceProvider _serviceProvider;

        protected async override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();

            ConfigureServices(services);
            await Check();

            _serviceProvider = services.BuildServiceProvider();

            var settings = _serviceProvider.GetRequiredService<SettingsService>();
            await settings.LoadAsync();

            LegendDesign.Theme.ApplyTheme(settings.AppTheme);
            LegendDesign.Theme.SetAccentSource(settings.AccentSource);

            var mainWin = _serviceProvider.GetRequiredService<MainWindow>();
            mainWin.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<SettingsService>(sp => new SettingsService(AppDirectory));

            services.AddSingleton<ACadService>();
            services.AddSingleton<IExcelService, TestExcelService>();
            services.AddSingleton<DXFService>();

            services.AddSingleton<QuickPanelViewModel>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<TableViewModel>();
            services.AddSingleton<HomeViewModel>();

            services.AddTransient<MainWindow>();
        }

        #region OtherMethods

        private async Task Check()
        {
            if (!await CoreController.CheckAsync())
            {
                MessageBox.Show(
                    LocaleService.Get(Message.CrtErr),
                    LocaleService.Get(Message.ErrTittle),
                    MessageBoxButton.OK, MessageBoxImage.Error);

                SD();
            }
        }

        public static void SD()
        {
            Application.Current.Shutdown();
        }

        #endregion
    }
}
