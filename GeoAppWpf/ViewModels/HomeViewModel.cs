using GeoAppWpf.Controls;
using GeoAppWpf.Models;
using GeoAppWpf.Services;
using LegendDesignWpf.Core.MVVM;
using Microsoft.Extensions.DependencyInjection;
using netDxf.Entities;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

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

        public IReadOnlyList<TreeMenuItem> DockPanelItems =>
        [
            new()
            {
                Header = "Камера",
                Items =
                [
                    new()
                    {
                        Header = "Центр",
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

        public ICommand BuildCarcasCommand => new RelayCommand(() =>
        {
            var selectedVisuals = ViewportController.Visuals
                .Where(x => x.IsSelected);

            var selectedEntities = selectedVisuals
                .Where(x => x.Entity is Polyline3D)
                .Select(x => (Polyline3D)x.Entity)
                .ToList();

            // 1. Экстраполяция
            var extrapolateEntities = Extrapolator.Extrapolate(selectedEntities, 25, 0.1);
            var allContours = selectedEntities.Concat(extrapolateEntities).ToList();

            if (allContours.Count < 2) return;

            // 2. Генерируем каркас (сущности DXF, включая Face3D)
            var faces = CarcasBuiler.Build(allContours)?.ToList();
            if (faces == null || !faces.Any()) return;

            // 3. Сохраняем в DXF документ
            var dxfDoc = Documents
                .OfType<DxfDocumentNode>()
                .Select(x => x.Document)
                .FirstOrDefault();

            dxfDoc?.Entities.Add(faces);

            // 4. Рисуем линии каркаса
            foreach (var entity in faces)
                ViewportController.Add(new DrawerObject(entity));

            // 5. Рисуем каркас как объект
            if (faces.Any())
            {
                GeometryModel3D solidModel = CarcasMeshBuilder.BuildFromFaces(
                    faces,
                    Colors.Orange, // Цвет
                    opacity: 0.75  // Прозрачность
                );

                var modelVisual = new ModelVisual3D { Content = solidModel };
                ViewportController.Add(new DrawerObject(modelVisual));
            }

            ViewportController.UnselectAll();
        });

        /*
        public ICommand BuildCarcasCommand => new RelayCommand(() =>
        {
            var selectedVisuals = ViewportController.Visuals
                .Where(x => x.IsSelected);

            var selectedEntities = selectedVisuals
                .Where(x => x.Entity is Polyline3D)
                .Select(x => (Polyline3D)x.Entity)
                .ToList();

            var extrapolateEntities = Extrapolator.Extrapolate(selectedEntities, 25);

            foreach (var extEn in extrapolateEntities)
                selectedEntities.Add(extEn);

            var carcasEntities = CarcasBuiler.Build(selectedEntities);

            if (carcasEntities == null || carcasEntities.Count() == 0)
                return;

            var dxfDoc = Documents
                .Where(x => x is DxfDocumentNode)
                .Select(x => ((DxfDocumentNode)x).Document)
                .First();

            dxfDoc?.Entities.Add(carcasEntities);

            foreach (var entity in carcasEntities)
                ViewportController.Add(new DrawerObject(entity));

            foreach (var visual in selectedVisuals)
                visual.IsSelected = false;
        });
        */

        public ICommand UnselectAllCommand => new RelayCommand(ViewportController.UnselectAll);
        public ICommand UnselectLastCommand => new RelayCommand(ViewportController.UnselectLast);
        public ICommand SelectAllCommand => new RelayCommand(ViewportController.SelectAll);

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
