using GeoAppWpf.Interfaces;
using GeoAppWpf.Models;
using GeoAppWpf.Services;
using LegendDesignWpf.Core.MVVM;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace GeoAppWpf.ViewModels
{
    public class HomeViewModel : BaseViewModel
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly WorkspaceManager _workspaceManager;
        private bool _documentsAny;
        private GeoTreeNode? _selectedNode;
        private IEnumerable? _displayItems;
        private IMessageBox _messageBox;

        #region Public Properties

        public ObservableCollection<GeoTreeNode> Documents { get; set; } = new();

        public bool DocumentsAny
        {
            get => _documentsAny;
            set
            {
                _documentsAny = value;
                OnPropertyChanged();
            }
        }

        public GeoTreeNode? SelectedNode
        {
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

        private readonly IReadOnlyList<TreeMenuItem> _dockPanelItems;
        public IReadOnlyList<TreeMenuItem> DockPanelItems => _dockPanelItems;

        #endregion

        public HomeViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            _messageBox = _serviceProvider.GetRequiredService<IMessageBox>();
            _workspaceManager = _serviceProvider.GetRequiredService<WorkspaceManager>();

            _workspaceManager.OnDocumentsChanged += _workspaceManager_OnDocumentsChanged;

            Documents.CollectionChanged += (o, e) =>
            {
                DocumentsAny = Documents.Any();

                if (e.Action.HasFlag(NotifyCollectionChangedAction.Add))
                {
                    var item = e.NewItems?[0];

                    DisplayItems = item switch
                    {
                        GeoDocumentNode n => n.Document.GetObjects(),
                        _ => null
                    };
                }
            };

        }

        private void _workspaceManager_OnDocumentsChanged()
        {
            foreach (var document in _workspaceManager.Documents)
            {
                Documents.Add(new GeoDocumentNode(document));
            }
        }

        public void OnSelectedItemChanged(GeoTreeNode node)
        {
            DisplayItems = SelectedNode switch
            {
                BoreholeLineNode n => n.Line.Boreholes,
                BoreholeNode n => n.Borehole.Samples,
                GeoDocumentNode n => n.Children,
                _ => null
            };
        }
    }
}
