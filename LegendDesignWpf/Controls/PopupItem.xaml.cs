using LegendDesignWpf.Core.Enums;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LegendDesignWpf.Controls
{
    public partial class PopupItem : UserControl
    {
        public PopupItem()
        {
            InitializeComponent();
        }

        #region TextProperty
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(PopupItem), new PropertyMetadata("Кнопка"));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
        #endregion

        #region IconKindProperty
        public static readonly DependencyProperty IconKindProperty =
            DependencyProperty.Register(nameof(IconKind), typeof(PackIconKind), typeof(PopupItem));

        public PackIconKind IconKind
        {
            get => (PackIconKind)GetValue(IconKindProperty);
            set => SetValue(IconKindProperty, value);
        }
        #endregion

        #region ClickCommand
        public ICommand ClickCommand
        {
            get => (ICommand)GetValue(ClickCommandProperty);
            set => SetValue(ClickCommandProperty, value);
        }

        public static readonly DependencyProperty ClickCommandProperty =
            DependencyProperty.Register(nameof(ClickCommand), typeof(ICommand), typeof(PopupItem), new PropertyMetadata(null));
        #endregion

        #region ClickCommandParameter
        public object ClickCommandParameter
        {
            get => GetValue(ClickCommandParameterProperty);
            set => SetValue(ClickCommandParameterProperty, value);
        }

        public static readonly DependencyProperty ClickCommandParameterProperty =
            DependencyProperty.Register(nameof(ClickCommandParameter), typeof(object), typeof(PopupItem), new PropertyMetadata(null));
        #endregion
    }
}
