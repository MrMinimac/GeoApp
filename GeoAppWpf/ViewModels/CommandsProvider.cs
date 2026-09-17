using GeoAppWpf.Services;
using LegendDesignWpf.Core.MVVM;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace GeoAppWpf.ViewModels
{
    public class CommandsProvider
    {
        private readonly WorkspaceManager _workspaceManager;
        private readonly RelayCommand _importCommand;
        public ICommand ImportCommand => _importCommand;

        public CommandsProvider(IServiceProvider serviceProvider)
        {
            _workspaceManager = serviceProvider.GetRequiredService<WorkspaceManager>();
            _importCommand = new(Import);
        }

        private void Import()
        {
            var dialog = new OpenFileDialog
            {
                Filter = _workspaceManager?.GetOpenFileFilter(),
                Multiselect = true
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                _workspaceManager?.ImportFiles(dialog.FileNames);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}