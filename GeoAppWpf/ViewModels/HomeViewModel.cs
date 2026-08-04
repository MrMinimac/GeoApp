using GeoAppWpf.Controls;
using GeoAppWpf.Models;
using GeoAppWpf.Services;
using LegendDesignWpf.Core.MVVM;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;

namespace GeoAppWpf.ViewModels
{
    public interface ITreeCommandProvider
    {
        ICommand Open3DCommand { get; }
        ICommand SaveAsCommand { get; }
    }

    public enum SupportExtensions
    {
        DAT,
        DXF
    }

    public record SaveRequest(GeoTreeNode Node, SupportExtensions Extension);

    public class HomeViewModel : BaseViewModel, ITreeCommandProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ACadService _acService;
        private readonly DXFService _dxfService;
        private bool _documentsAny;
        private GeoTreeNode? _selectedNode;
        private IEnumerable? _displayItems;

        #region Public Properties

        public ViewportController ViewportController { get; }

        public ObservableCollection<GeoTreeNode> Documents { get; set; } = new();

        public bool DocumentsAny {
            get => _documentsAny;
            set
            {
                _documentsAny = value;
                OnPropertyChanged();
            }
        }

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

        #endregion

        #region Commands

        public ICommand Open3DCommand => new RelayCommand<GeoTreeNode>(async (node) =>
        {
            if (!ViewportController.IsAttached)
                return;

            if (node is EntitiesNode entityNode)
            {
                ViewportController.Add(new DrawerObject(entityNode.Entity));
            }
            else if (node is DxfDocumentNode dxfNode)
            {
                foreach (var entity in dxfNode.Document.Entities.All)
                    ViewportController.Add(new DrawerObject(entity));
            }
        });

        public ICommand SaveAsCommand => new RelayCommand<SaveRequest>(async (req) =>
        {
            switch (req.Node)
            {
                case DxfDocumentNode:
                    ((DxfDocumentNode)req.Node).Save(req.Extension);
                    break;
            }
        });

        #endregion

        public HomeViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            _acService = _serviceProvider.GetRequiredService<ACadService>();
            _dxfService = _serviceProvider.GetRequiredService<DXFService>();

            ViewportController = new ViewportController();

            _acService.DocumentChanged += (doc) =>
            {
                var docView = new GeoDocumentNode(doc, this);
                docView.Name = "Excel File";
                Documents.Add(docView);
            };

            _dxfService.DocumentChanged += (doc) =>
            {
                Documents.Add(new DxfDocumentNode(doc, this));
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
