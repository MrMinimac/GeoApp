using GeoAppWpf.ViewModels;
using LegendDesignWpf.Core.MVVM;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace GeoAppWpf.Models
{
    public class TreeMenuItem
    {
        public string Header { get; init; }

        public ICommand Command { get; init; }

        public object? CommandParameter { get; init; }

        public ObservableCollection<TreeMenuItem> Items { get; init; } = [];
    }

    public abstract class GeoTreeNode : BaseViewModel
    {
		private string _name;
		public string Name
		{
			get => _name;
            set 
			{
				if (value == _name)
					return;

                _name = value;
				OnPropertyChanged();
			}
		}

        public string Extension { get; init; }

        public ITreeCommandProvider CommandProvider { get; }

        public virtual IReadOnlyList<TreeMenuItem> MenuItems => [];

        public ObservableCollection<GeoTreeNode> Children { get; } = new();

        protected GeoTreeNode(ITreeCommandProvider commandProvider)
        {
            CommandProvider = commandProvider;
        }
    }
}
