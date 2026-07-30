using GeoAppCore;
using GeoAppWpf.Services;
using LegendDesignWpf.Core.MVVM;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace GeoAppWpf.ViewModels
{
    public class TableViewModel : BaseViewModel
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ACadService _acService;
        private GeoDoc? _document;

        public GeoDoc? Document
        {
            get => _document;
            set
            {
                _document = value;
                OnPropertyChanged();
            }
        }

        public TableViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _acService = _serviceProvider.GetRequiredService<ACadService>();
            _acService.DocumentChanged += (doc) =>
            {
                Document = doc;
                MessageBox.Show("Документ загружен!");
            };
        }
    }
}
