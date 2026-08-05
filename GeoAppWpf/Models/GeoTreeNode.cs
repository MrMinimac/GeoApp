using GeoAppWpf.Interfaces;
using LegendDesignWpf.Core.MVVM;
using System.Collections.ObjectModel;

namespace GeoAppWpf.Models
{
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
