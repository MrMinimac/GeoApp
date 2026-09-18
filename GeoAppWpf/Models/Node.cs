using GeoAppWpf.Helpers;
using GeoAppWpf.Interfaces;
using GeoAppWpf.ViewModels;
using LegendDesignWpf.Core.MVVM;
using System.Collections.ObjectModel;

namespace GeoAppWpf.Models
{
    public class Node : BaseViewModel
    {
        private string _header;
        private bool _isSelected;
        private bool _isExpanded;
        private bool _isVisible = true;
        private string _toolTipText = "";

        protected readonly CommandsProvider CommandsProvider;

        [PropertyControlIgnore]
        public string Header
        {
            get => _header;
            set
            {
                if (_header == value)
                    return;

                _header = value;
                OnPropertyChanged();
            }
        }

        [PropertyControlIgnore]
        public virtual IReadOnlyList<NodeMenuItem> MenuItems => [];

        [PropertyControlIgnore]
        public bool HasMenuItems => MenuItems.Count > 0;

        [PropertyControlIgnore]
        public ObservableCollection<Node> Children { get; } = new();

        [PropertyControlIgnore]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                    return;

                _isSelected = value;
                OnPropertyChanged();
            }
        }

        [PropertyControlIgnore]
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        [PropertyControlIgnore]
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded == value)
                    return;

                _isExpanded = value;
                OnPropertyChanged();
            }
        }

        [PropertyControlIgnore]
        public string ToolTipText
        {
            get => _toolTipText;
            set => SetProperty(ref _toolTipText, value);
        }

        public Node(string header, CommandsProvider commandsProvider)
        {
            _header = header;
            CommandsProvider = commandsProvider;
        }
    }

    public static class NodeExtensions
    {
        public static IEnumerable<Node> Flatten(this Node node)
        {
            // Возвращаем сам узел, а затем рекурсивно всех его детей
            return new[] { node }.Concat(node.Children?.SelectMany(c => c.Flatten()) ?? Enumerable.Empty<Node>());
        }
    }
}
