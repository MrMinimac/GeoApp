using LegendDesignWpf.Controls;
using LegendDesignWpf.Core.Models;
using LegendDesignWpf.Core.MVVM;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace LegendDesignWpf.View.MessageBox
{
    public partial class LDMessageBoxWindow : LDWindow
    {
        public MessageBoxResult Result { get; set; } = MessageBoxResult.Cancel;
        public ICommand ButtonClickCommand => new RelayCommand<ButtonModel>(async (b) => await OnButtonClickAsync(b));

        public LDMessageBoxWindow()
        {
            InitializeComponent();
            Buttons = new ObservableCollection<ButtonModel>();
        }

        #region ButtonsProperty
        public static readonly DependencyProperty ButtonsProperty =
            DependencyProperty.Register(
                nameof(Buttons),
                typeof(ObservableCollection<ButtonModel>),
                typeof(LDMessageBoxWindow),
                new PropertyMetadata(null)
            );

        public ObservableCollection<ButtonModel> Buttons
        {
            get => (ObservableCollection<ButtonModel>)GetValue(ButtonsProperty);
            set => SetValue(ButtonsProperty, value);
        }
        #endregion

        #region LoadingMessage
        public static readonly DependencyProperty LoadingMessageProperty =
            DependencyProperty.Register(
                nameof(LoadingMessage),
                typeof(string),
                typeof(LDMessageBoxWindow),
                new PropertyMetadata("Loading...")
            );

        public string LoadingMessage
        {
            get => (string)GetValue(LoadingMessageProperty);
            set => SetValue(LoadingMessageProperty, value);
        }
        #endregion

        #region Message
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(
                nameof(Message),
                typeof(string),
                typeof(LDMessageBoxWindow),
                new PropertyMetadata("")
            );

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }
        #endregion

        private async Task OnButtonClickAsync(ButtonModel button)
        {
            if (button.Action == null)
            {
                Close();
                return;
            }

            LoadingGrid.Visibility = Visibility.Visible;
            MessageGrid.Visibility = Visibility.Collapsed;
            await button.Action();
            Close();
        }
    }
}
