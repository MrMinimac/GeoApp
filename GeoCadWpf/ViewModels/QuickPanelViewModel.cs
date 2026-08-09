using GeoAppCore.Services;
using GeoAppCore.Workspace;
using LegendDesignWpf.Core.MVVM;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows.Input;

namespace GeoCadWpf.ViewModels
{
    public class QuickPanelViewModel : BaseViewModel
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ImporterService _importerService;
        private readonly WorkspaceManager _workspaceManager;

        public QuickPanelViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _importerService = _serviceProvider.GetRequiredService<ImporterService>();
            _workspaceManager = _serviceProvider.GetRequiredService<WorkspaceManager>();
        }

        #region Commands

        public ICommand ImportCommand => new RelayCommand(async () =>
        {
            var dialog = new OpenFileDialog
            {
                Filter = _importerService.GetOpenFileFilter()
            };

            if (dialog.ShowDialog() == true)
            {
                Debug.WriteLine("Importing...");

                var doc = _importerService.Import(dialog.FileName);

                if (doc == null)
                    return;

                Debug.WriteLine("Imported...");

                _workspaceManager.LoadDocument(doc);
            }
        });

        public ICommand ExportCommand => new RelayCommand(async () =>
        {
            
        });

        #endregion
    }
}
