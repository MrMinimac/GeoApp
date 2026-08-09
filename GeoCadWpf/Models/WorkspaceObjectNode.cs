using GeoAppCore.Objects;
using LegendDesignWpf.Core.MVVM;

namespace GeoCadWpf.Models
{
    public class WorkspaceObjectNode : BaseViewModel
    {
        protected readonly WorkspaceObject Object;

        public string Name
        {
            get => Object.Name;
            set
            {
                Object.Name = value;
                OnPropertyChanged();
            }
        }

        public WorkspaceObjectNode(WorkspaceObject obj)
        {
            Object = obj;
        }
    }
}
