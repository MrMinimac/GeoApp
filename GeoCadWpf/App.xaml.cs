using GeoAppCore.Abstractions.Document;
using GeoAppCore.Services;
using GeoAppCore.Workspace;
using GeoCad.Infrastructure.DXF;
using GeoCad.Infrastructure.Services;
using GeoCadWpf.ViewModels;
using GeoCadWpf.Views.Windows;
using LegendDesignWpf.Core;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Windows;

namespace GeoCadWpf
{
    public partial class App : Application
    {
        public const string APP_NAME = "GeoCad";
        public string AppDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), APP_NAME);

        private ServiceProvider? _serviceProvider;

        protected async override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            ConfigureServices(services);

            _serviceProvider = services.BuildServiceProvider();
            await SetTheme();

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<SettingsService>(sp => new SettingsService(AppDirectory));

            services.AddSingleton<IDocumentLoader, DxfDocumentLoader>();
            services.AddSingleton<ImporterService>();
            services.AddSingleton<WorkspaceManager>();

            services.AddSingleton<QuickPanelViewModel>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<HomeViewModel>();

            services.AddTransient<MainWindow>();
        }

        private async Task SetTheme()
        {
            if (_serviceProvider == null)
                throw new NullReferenceException("ServiceProvider");

            var settings = _serviceProvider.GetRequiredService<SettingsService>();
            await settings.LoadAsync();

            LegendDesign.Theme.ApplyTheme(settings.AppTheme);
            LegendDesign.Theme.SetAccentSource(settings.AccentSource);
        }
    }
}
