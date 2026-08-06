using GeoAppWpf.Interfaces;
using GeoAppWpf.Models;
using GeoAppWpf.Operations;
using GeoAppWpf.Services;
using LegendDesignWpf.Core.MVVM;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;

namespace GeoAppWpf.ViewModels
{
    public class HomeViewModel : BaseViewModel, ITreeCommandProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ACadService _acService;
        private readonly DXFService _dxfService;
        private readonly UndoManager _undoManager;
        private bool _documentsAny;
        private GeoTreeNode? _selectedNode;
        private IEnumerable? _displayItems;
        private IMessageBox _messageBox;

        #region Public Properties

        public ViewportController ViewportController { get; }

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

        #region Commands

        private readonly RelayCommand<GeoTreeNode> _open3DCommand;
        private readonly RelayCommand<SaveRequest> _saveAsCommand;
        private readonly RelayCommand _buildCarcasCommand;
        private readonly RelayCommand _undoCommand;
        private readonly RelayCommand _redoCommand;
        private readonly RelayCommand _unselectAllCommand;
        private readonly RelayCommand _unselectLastCommand;
        private readonly RelayCommand _selectAllCommand;
        private readonly RelayCommand _zoomExtentsCommand;

        public ICommand Open3DCommand => _open3DCommand;
        public ICommand SaveAsCommand => _saveAsCommand;
        public ICommand BuildCarcasCommand => _buildCarcasCommand;
        public ICommand UndoCommand => _undoCommand;
        public ICommand RedoCommand => _redoCommand;
        public ICommand UnselectAllCommand => _unselectAllCommand;
        public ICommand UnselectLastCommand => _unselectLastCommand;
        public ICommand SelectAllCommand => _selectAllCommand;
        public ICommand ZoomExtentsCommand => _zoomExtentsCommand;

        private void Open3D(GeoTreeNode node)
        {
            if (!ViewportController.IsAttached)
                return;

            if (node is EntitiesNode entityNode)
                ViewportController.Add(new DrawerObject(entityNode.Entity));

            else if (node is DxfDocumentNode dxfNode)
            {
                var entities = dxfNode.Document.Entities.All;
                var objs = entities.Select(e => new DrawerObject(e));
                ViewportController.AddRange(objs);
            }
        }

        private void SaveAs(SaveRequest request)
        {
            switch (request.Node)
            {
                case DxfDocumentNode:
                    ((DxfDocumentNode)request.Node).Save(request.Extension);
                    break;
            }
        }

        private void BuildCarcas()
        {
            var dxfDoc = Documents
                .OfType<DxfDocumentNode>()
                .Select(x => x.Document)
                .FirstOrDefault();

            if (dxfDoc == null)
            {
                _messageBox.ShowError("DXF документ не открыт.");
                return;
            }

            var operation = new BuildCarcasOperation(dxfDoc, ViewportController);

            try
            {
                _undoManager.Execute(operation);
            }
            catch (Exception e)
            {
                _messageBox.ShowError(e.Message);
            }
        }

        #endregion

        public HomeViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            _acService = _serviceProvider.GetRequiredService<ACadService>();
            _dxfService = _serviceProvider.GetRequiredService<DXFService>();
            _messageBox = _serviceProvider.GetRequiredService<IMessageBox>();
            _undoManager = _serviceProvider.GetRequiredService<UndoManager>();

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
                        DxfDocumentNode n => n.EntityViews,
                        GeoDocumentNode n => n.Document.BoreholeLines,
                        _ => null
                    };
                }
            };

            _open3DCommand = new RelayCommand<GeoTreeNode>(Open3D);
            _saveAsCommand = new RelayCommand<SaveRequest>(SaveAs);
            _buildCarcasCommand = new RelayCommand(BuildCarcas);
            _undoCommand = new RelayCommand(_undoManager.Undo, () => _undoManager.CanUndo);
            _redoCommand = new RelayCommand(_undoManager.Redo, () => _undoManager.CanRedo);
            _unselectAllCommand = new RelayCommand(ViewportController.UnselectAll);
            _unselectLastCommand = new RelayCommand(ViewportController.UnselectLast);
            _selectAllCommand = new RelayCommand(ViewportController.SelectAll);
            _zoomExtentsCommand = new RelayCommand(ViewportController.ZoomExtents);

            _undoManager.StateChanged += UndoManager_StateChanged;

            _dockPanelItems = BuildDockPanelItems();
        }

        public void OnSelectedItemChanged(GeoTreeNode node)
        {
            DisplayItems = SelectedNode switch
            {
                BoreholeLineNode n => n.Line.Boreholes,
                BoreholeNode n => n.Borehole.Samples,
                GeoDocumentNode n => n.Document.BoreholeLines,
                DxfDocumentNode n => n.EntityViews,
                EntitiesNode n => n.Vertexes,
                _ => null
            };
        }

        private void UndoManager_StateChanged()
        {
            _undoCommand.RaiseCanExecuteChanged();
            _redoCommand.RaiseCanExecuteChanged();
        }

        private IReadOnlyList<TreeMenuItem> BuildDockPanelItems()
        {
            return
            [
                new()
                {
                    Header = "Камера",
                    Items =
                    [
                        new()
                        {
                            Header = "Центр",
                            Command = ZoomExtentsCommand
                        },
                    ]
                },
                new()
                {
                    Header = "Правка",
                    Items =
                    [
                        new()
                        {
                            Header = "Отменить",
                            Command = UndoCommand
                        },
                        new()
                        {
                            Header = "Вернуть",
                            Command = RedoCommand
                        },
                        new()
                        {
                            Header = "Снять последнее выделение",
                            Command = UnselectLastCommand
                        },
                        new()
                        {
                            Header = "Снять все выделения",
                            Command = UnselectAllCommand
                        },
                        new()
                        {
                            Header = "Выбрать все",
                            Command = SelectAllCommand
                        },
                    ]
                },
                new()
                {
                    Header = "Редактирование",
                    Items =
                    [
                        new()
                        {
                            Header = "Построить каркас",
                            Command = BuildCarcasCommand,
                        }
                    ]
                }
            ];
        }
    }
}
