using GeoAppWpf.Interfaces;
using System.Windows;

namespace GeoAppWpf.Services
{
    public class MessageBoxService : IMessageBox
    {
        public void ShowError(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
