using GeoAppCore;
using GeoAppWpf.Services;
using LegendDesignWpf.Core.MVVM;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime;

namespace GeoAppWpf.ViewModels
{
    public class TableViewModel : BaseViewModel
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ACadService _acService;

        public GeoDoc? Document
        {
            get => _acService?.Document;
            set
            {
                _acService.Document = value;
                OnPropertyChanged();
            }
        }

        public TableViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _acService = _serviceProvider.GetRequiredService<ACadService>();

            var doc = new GeoDoc();

            double x = 0;
            double y = 0;

            for (int i = 1; i <= 2; i++)
            {
                var boreholes = new List<Borehole>();

                for (int l = 1; l < 10; l++)
                {
                    x += Random.Shared.Next(140, 150) / 10.0;
                    y += Random.Shared.Next(240, 250) / 10.0;

                    double z = Random.Shared.Next(70000, 90000) / 100.0;

                    var bh = new Borehole
                    {
                        Id = l,
                        Key = $"{i}_{l}",
                        LineNumber = i,
                        X = x,
                        Y = y,
                        Z = z,
                    };

                    double lFrom = 0;
                    double lTo = 0.5;

                    for (int s = 0; s < 20; s++)
                    {
                        bh.Samples.Add(new Sample
                        {
                            From = lFrom,
                            To = lTo,
                            Value = Random.Shared.Next(0, 1000) / 1000.0,
                            Fineness = 0.9,
                            Diametr = 90,
                            X = x,
                            Y = y,
                            Z = z - (lTo - lFrom),
                            Lithologies = [Lithology.Slate],
                        });

                        lTo += 0.5;
                        lFrom += 0.5;
                    }

                    boreholes.Add(bh);
                }

                doc.BoreholeLines.Add(new BoreholeLine { Number = i, Boreholes = boreholes });
            }

            Document = doc;
        }
    }
}
