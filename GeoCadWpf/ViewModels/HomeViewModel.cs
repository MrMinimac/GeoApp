using GeoAppCore.Services;
using GeoAppCore.Workspace;
using LegendDesignWpf.Core.MVVM;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace GeoCadWpf.ViewModels
{
    public class HomeViewModel : BaseViewModel
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly WorkspaceManager _workspaceManager;

        public ObservableCollection<WorkspaceViewModel> Workspaces { get; } = new();

        #region Commands

        private RelayCommand _createCommand;
        private RelayCommand<WorkspaceViewModel> _removeCommand;

        public ICommand CreateCommand { get; private set; }
        public ICommand RemoveCommand { get; private set; }

        #endregion

        public WorkspaceViewModel? CurrentWorkspace
        {
            get => Workspaces.FirstOrDefault(x => x.Workspace == _workspaceManager.CurrentWorkspace);
            set
            {
                if (value == null)
                    return;

                _workspaceManager.Select(value.Workspace);
                OnPropertyChanged();
            }
        }

        public HomeViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _workspaceManager = _serviceProvider.GetRequiredService<WorkspaceManager>();
            _workspaceManager.CurrentWorkspaceChanged += OnCurrentWorkspaceChanged;
            InitializeCommands();

            Create();
        }

        private void OnCurrentWorkspaceChanged()
        {
            OnPropertyChanged(nameof(CurrentWorkspace));
        }

        public void Create()
        {
            var workspace = _workspaceManager.Create();
            var viewModel = new WorkspaceViewModel(workspace);

            Workspaces.Add(viewModel);
            CurrentWorkspace = viewModel;
        }


        public void Remove(WorkspaceViewModel viewModel)
        {
            _workspaceManager.Remove(viewModel.Workspace);
            Workspaces.Remove(viewModel);
        }

        private void InitializeCommands()
        {
            _createCommand = new(Create);
            CreateCommand = _createCommand;

            _removeCommand = new(Remove);
            RemoveCommand = _removeCommand;
        }
    }
}
