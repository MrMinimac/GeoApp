using GeoAppWpf.Services;
using LegendDesignWpf.Core.MVVM;
using Microsoft.Extensions.DependencyInjection;

namespace GeoAppWpf.ViewModels
{
    public class StatusBarViewModel : BaseViewModel, IDisposable
    {
        private readonly AutoCadService _acadService;
        private bool _disposed;

        private bool _autoCadStatus = false;
        public bool AutoCadStatus 
        {
            get => _autoCadStatus;
            set
            {
                if (_autoCadStatus == value)
                    return;

                _autoCadStatus = value;
                OnPropertyChanged(nameof(AutoCadStatus));
            }
        }

        public StatusBarViewModel(IServiceProvider serviceProvider)
        {
            _acadService = serviceProvider.GetRequiredService<AutoCadService>();

            _ = CheckConnectionLoop();
        }

        private async Task CheckConnectionLoop()
        {
            while (!_disposed)
            {
                AutoCadStatus = await _acadService.CheckConnection();
                await Task.Delay(500);
            }
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
