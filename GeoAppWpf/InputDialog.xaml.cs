using LegendDesignWpf.Controls;
using System.Windows;

namespace GeoAppWpf
{
    public partial class InputDialog : LDWindow
    {
        public string ResponseText { get; private set; }

        private InputDialog(string prompt, string title, string defaultValue)
        {
            InitializeComponent();

            if (!string.IsNullOrEmpty(title))
                Title = title;

            if (!string.IsNullOrEmpty(prompt))
                PromptText.Text = prompt;

            InputTextBox.Text = defaultValue ?? string.Empty;

            Loaded += (s, e) =>
            {
                InputTextBox.Focus();
                InputTextBox.SelectAll();
            };
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ResponseText = InputTextBox.Text;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        public static string Show(string prompt = "Введите значение:",
                                   string title = "Ввод данных",
                                   string defaultValue = "")
        {
            var dialog = new InputDialog(prompt, title, defaultValue);

            // Опционально: привязать владельца, если есть активное окно
            if (Application.Current?.MainWindow != null && Application.Current.MainWindow != dialog)
            {
                dialog.Owner = Application.Current.MainWindow;
            }

            bool? result = dialog.ShowDialog();

            return result == true ? dialog.ResponseText : null;
        }
    }
}
