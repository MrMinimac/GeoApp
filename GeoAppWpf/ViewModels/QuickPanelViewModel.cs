using GeoAppWpf.Services;
using LegendDesignWpf.Core;
using LegendDesignWpf.Core.Enums;
using LegendDesignWpf.Core.MVVM;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime;
using System.Windows.Input;

namespace GeoAppWpf.ViewModels
{
    public class QuickPanelViewModel : BaseViewModel
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ACadService _acService;

        public QuickPanelViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _acService = _serviceProvider.GetRequiredService<ACadService>();
        }

        #region Commands

        public ICommand ExportSectionsInACAD => new RelayCommand(async () =>
        {
            await _acService.ExportSections();
        });

        public ICommand ExportPlanInACAD => new RelayCommand(async () =>
        {
            await _acService.ExportPlan();
        });

        #endregion
    }
}
