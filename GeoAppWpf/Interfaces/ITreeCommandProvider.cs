using System.Windows.Input;

namespace GeoAppWpf.Interfaces
{
    public interface ITreeCommandProvider
    {
        ICommand Open3DCommand { get; }
        ICommand SaveAsCommand { get; }
    }
}
