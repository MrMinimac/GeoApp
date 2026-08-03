using GeoAppCore.Services;
using GeoAppWpf.Services;
using LegendDesignWpf.Core.MVVM;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;

namespace GeoAppWpf.ViewModels
{
    public class QuickPanelViewModel : BaseViewModel
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ACadService _acadService;
        private readonly IExcelService _excelService;
        private readonly DXFService _dxfService;

        public QuickPanelViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _acadService = _serviceProvider.GetRequiredService<ACadService>();
            _excelService = _serviceProvider.GetRequiredService<IExcelService>();
            _dxfService = _serviceProvider.GetRequiredService<DXFService>();
        }

        #region Commands

        public ICommand ImportMacromineDat => new RelayCommand(async () =>
        {
            _dxfService.ImportDat();
        });

        public ICommand ImportDXF => new RelayCommand(async () =>
        {
            _dxfService.Import();
        });

        public ICommand ImportFromExcel => new RelayCommand(async () =>
        {
            var doc = _excelService.Load();
            _acadService.Document = doc;
        });

        public ICommand ExportSectionsInACAD => new RelayCommand(async () =>
        {
            await _acadService.ExportSections();
        });

        public ICommand ExportPlanInACAD => new RelayCommand(async () =>
        {
            await _acadService.ExportPlan();
        });

        #endregion
    }
}
