using GeoAppWpf.Interfaces;
using GeoAppWpf.Models;
using GeoAppWpf.Services;
using LegendDesignWpf.Core.MVVM;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using System.Collections.ObjectModel;

namespace GeoAppWpf.ViewModels
{
    //public abstract class DocumentTreeNode : BaseViewModel
    //{
    //    public string Name { get; set; }

    //    public string Extension { get; init; }

    //    public ITreeCommandProvider CommandProvider { get; }

    //    public virtual IReadOnlyList<TreeMenuItem> MenuItems => [];

    //    public ObservableCollection<GeoTreeNode> Children { get; } = new();

    //    protected DocumentTreeNode(ITreeCommandProvider commandProvider)
    //    {
    //        CommandProvider = commandProvider;
    //    }
    //}

    //public class DocumentTreeViewModel : BaseViewModel
    //{
    //    private readonly IServiceProvider _serviceProvider;
    //    private readonly DocumentManager _documentManager;
    //    private GeoTreeNode? _selectedNode;
    //    private IEnumerable? _displayItems;

    //    public ObservableCollection<DocumentTreeNode> Documents { get; } = new();

    //    public GeoTreeNode? SelectedNode
    //    {
    //        get => _selectedNode;
    //        set
    //        {
    //            if (value == null || value == _selectedNode)
    //                return;

    //            _selectedNode = value;
    //            OnPropertyChanged();
    //            OnSelectedItemChanged(value);
    //        }
    //    }

    //    public IEnumerable? DisplayItems
    //    {
    //        get => _displayItems;
    //        set
    //        {
    //            if (value == _displayItems)
    //                return;

    //            _displayItems = value;
    //            OnPropertyChanged();
    //        }
    //    }

    //    public DocumentTreeViewModel(IServiceProvider serviceProvider)
    //    {
    //        _serviceProvider = serviceProvider;
    //        _documentManager = _serviceProvider.GetRequiredService<DocumentManager>();

    //        _documentManager.DocumentLoaded += (doc) =>
    //        {
    //            Documents.Add(new DocumentTreeNode(doc));
    //        };
    //    }

    //    public void OnSelectedItemChanged(GeoTreeNode node)
    //    {
    //        DisplayItems = SelectedNode switch
    //        {
    //            BoreholeLineNode n => n.Line.Boreholes,
    //            BoreholeNode n => n.Borehole.Samples,
    //            GeoDocumentNode n => n.Document.BoreholeLines,
    //            DxfDocumentNode n => n.EntityViews,
    //            EntitiesNode n => n.Vertexes,
    //            _ => null
    //        };
    //    }
    // }
}
