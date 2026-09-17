using LegendDesignWpf.Core.MVVM;

namespace GeoAppWpf.ViewModels
{
    public class QuickMenuViewModel : BaseViewModel
    {
        public CommandsProvider CommandsProvider { get; }

        public QuickMenuViewModel(CommandsProvider commandsProvider)
        {
            CommandsProvider = commandsProvider;
        }
    }

    /*
    public class QuickPanelViewModel : BaseViewModel
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ACadService _acadService;
        private readonly IExcelService _excelService;

        public QuickPanelViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _acadService = _serviceProvider.GetRequiredService<ACadService>();
            _excelService = _serviceProvider.GetRequiredService<IExcelService>();
        }

        #region Commands

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
    */
}
