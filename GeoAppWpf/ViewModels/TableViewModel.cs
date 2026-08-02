using GeoAppWpf.Models;
using GeoAppWpf.Services;
using LegendDesignWpf.Core.MVVM;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace GeoAppWpf.ViewModels
{
    public class TableViewModel : BaseViewModel
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ACadService _acService;
        private readonly DXFService _dxfService;

        public ObservableCollection<GeoTreeNode> Documents { get; set; } = new();

        private bool _documentsAny;
        public bool DocumentsAny {
            get => _documentsAny;
            set
            {
                _documentsAny = value;
                OnPropertyChanged();
            }
        }

        private GeoTreeNode? _selectedNode;
        public GeoTreeNode? SelectedNode {
            get => _selectedNode;
            set
            {
                if (value == null || value == _selectedNode)
                    return;

                _selectedNode = value;
                OnPropertyChanged();
                OnSelectedItemChanged(value);
            }
        }

        private IEnumerable? _displayItems;
        public IEnumerable? DisplayItems
        {
            get => _displayItems;
            set
            {
                if (value == _displayItems)
                    return;

                _displayItems = value;
                OnPropertyChanged();
            }
        }

        public TableViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            _acService = _serviceProvider.GetRequiredService<ACadService>();
            _dxfService = _serviceProvider.GetRequiredService<DXFService>();

            _acService.DocumentChanged += (doc) =>
            {
                var docView = new GeoDocumentNode(doc);
                docView.Name = "Excel File";
                Documents.Add(docView);
            };

            _dxfService.DocumentChanged += (doc) =>
            {
                Documents.Add(new DxfDocumentNode(doc));
            };

            Documents.CollectionChanged += (o, e) =>
            {
                DocumentsAny = Documents.Any();

                if (e.Action.HasFlag(NotifyCollectionChangedAction.Add))
                {
                    var item = e.NewItems?[0];

                    DisplayItems = item switch
                    {
                        DxfDocumentNode n => n.Entities,
                        GeoDocumentNode n => n.Document.BoreholeLines,
                        _ => null
                    };
                }
            };
        }

        public void OnSelectedItemChanged(GeoTreeNode node)
        {
            DisplayItems = SelectedNode switch
            {
                BoreholeLineNode n => n.Line.Boreholes,
                BoreholeNode n => n.Borehole.Samples,
                GeoDocumentNode n => n.Document.BoreholeLines,
                DxfDocumentNode n => n.Entities,
                EntitiesNode n => n.Vertexes,
                _ => null
            };
        }
    }
}
