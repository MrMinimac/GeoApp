using GeoAppCore.Abstractions.Document;
using GeoAppWpf.Interfaces;
using GeoAppWpf.Models;
using GeoAppWpf.Services;
using LegendDesignWpf.Core.MVVM;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Input;

namespace GeoAppWpf.ViewModels
{
    public class HomeViewModel : BaseViewModel
    {
        private Node? _selectionAnchor;
        private readonly IServiceProvider _serviceProvider;
        private readonly WorkspaceManager _workspaceManager;
        private readonly CommandsProvider _commandsProvider;
        private Node? _selectedNode;

        #region Public Properties

        public ObservableCollection<Node> Nodes { get; set; } = new();
        public IEnumerable<Node> AllNodes => Nodes.SelectMany(n => n.Flatten());

        public bool DocumentsAny => Nodes.Any();

        public Node? SelectedNode => AllNodes.Where(x => x.IsSelected).FirstOrDefault();

        public IEnumerable? DisplayItems
        {
            get
            {
                return SelectedNode switch
                {
                    BoreholeLineNode n => n.Line.Boreholes,
                    BoreholeNode n => n.Borehole.Samples,
                    BoreholesDocumentNode n => n.Boreholes,
                    _ => null
                };
            }
        }

        #endregion

        #region Commands
        private readonly RelayCommand<NodeSelectionRequest> _selectNodeCommand;
        public ICommand SelectNodeCommand => _selectNodeCommand;
        #endregion


        public HomeViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            _workspaceManager = _serviceProvider.GetRequiredService<WorkspaceManager>();
            _commandsProvider = _serviceProvider.GetRequiredService<CommandsProvider>();

            _workspaceManager.OnDocumentAdded += OnWorkspaceManagerDocumentsChanged;
            Nodes.CollectionChanged += OnNodesCollectionChanged;

            _selectNodeCommand = new RelayCommand<NodeSelectionRequest>(SelectNode);
        }

        private void OnNodesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(DocumentsAny));
        }

        private void OnWorkspaceManagerDocumentsChanged(IDocument document)
        {
            Nodes.Add(new BoreholesDocumentNode(document, _commandsProvider));
        }

        private void SelectNode(NodeSelectionRequest request)
        {
            if (request.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                SelectRange(request.Node);
                return;
            }

            if (request.Modifiers.HasFlag(ModifierKeys.Control))
            {
                request.Node.IsSelected = !request.Node.IsSelected;
                _selectionAnchor = request.Node;
                return;
            }

            foreach (var node in AllNodes)
            {
                node.IsSelected = node == request.Node;
            }

            _selectionAnchor = request.Node;

            OnPropertyChanged(nameof(SelectedNode));
            OnPropertyChanged(nameof(DisplayItems));
        }

        private void SelectRange(Node node)
        {
            if (_selectionAnchor == null)
            {
                node.IsSelected = true;
                _selectionAnchor = node;
                return;
            }

            var flatNodes = AllNodes.ToList();

            int anchorIndex = flatNodes.IndexOf(_selectionAnchor);
            int targetIndex = flatNodes.IndexOf(node);

            if (anchorIndex < 0 || targetIndex < 0)
                return;

            int start = Math.Min(anchorIndex, targetIndex);
            int end = Math.Max(anchorIndex, targetIndex);

            var nodesToSelect = flatNodes
                .Skip(start)
                .Take(end - start + 1)
                .Where(x => x.IsVisible)
                .ToHashSet();

            foreach (var n in flatNodes)
            {
                n.IsSelected = nodesToSelect.Contains(n);
            }
        }
    }
}
