using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LegendDesignWpf.Controls
{
    public partial class SearchBox : UserControl
    {
        public SearchBox()
        {
            InitializeComponent();
        }

        #region InputTextProperty
        public static readonly DependencyProperty InputTextProperty =
            DependencyProperty.Register(nameof(InputText), typeof(string), typeof(SearchBox), new PropertyMetadata(string.Empty));

        public string InputText
        {
            get => (string)GetValue(InputTextProperty);
            set => SetValue(InputTextProperty, value);
        }
        #endregion

        #region IsSearchingProperty
        public static readonly DependencyProperty IsSearchingProperty =
            DependencyProperty.Register(nameof(IsSearching), typeof(bool), typeof(SearchBox), new PropertyMetadata(false));

        public bool IsSearching
        {
            get => (bool)GetValue(IsSearchingProperty);
            set => SetValue(IsSearchingProperty, value);
        }
        #endregion

        #region HintTextProperty
        public static readonly DependencyProperty HintTextProperty =
            DependencyProperty.Register(nameof(HintText), typeof(string), typeof(SearchBox), new PropertyMetadata(string.Empty));

        public string HintText
        {
            get => (string)GetValue(HintTextProperty);
            set => SetValue(HintTextProperty, value);
        }
        #endregion

        #region CancelCommand
        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(SearchBox), new PropertyMetadata(false));
        #endregion

        #region BorderBrush
        public static readonly new DependencyProperty BorderBrushProperty =
            DependencyProperty.Register(nameof(BorderBrush), typeof(Brush), 
                typeof(SearchBox), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(133,64,180))));

        public new Brush BorderBrush
        {
            get => (Brush)GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }
        #endregion

        #region BorderInactiveBrush
        public static readonly DependencyProperty BorderInactiveBrushProperty =
            DependencyProperty.Register(nameof(BorderInactiveBrush), typeof(Brush), 
                typeof(SearchBox), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(124, 124, 124))));

        public Brush BorderInactiveBrush
        {
            get => (Brush)GetValue(BorderInactiveBrushProperty);
            set => SetValue(BorderInactiveBrushProperty, value);
        }
        #endregion

        #region BorderBackground
        public static readonly DependencyProperty BorderBackgroundProperty =
            DependencyProperty.Register(nameof(BorderBackground), typeof(Brush),
                typeof(SearchBox), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(44, 44, 44))));

        public Brush BorderBackground
        {
            get => (Brush)GetValue(BorderBackgroundProperty);
            set => SetValue(BorderBackgroundProperty, value);
        }
        #endregion

        #region HoverBrush
        public static readonly DependencyProperty HoverBrushProperty =
            DependencyProperty.Register(nameof(HoverBrush), typeof(Brush),
                typeof(SearchBox), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(60, 60, 60))));

        public Brush HoverBrush
        {
            get => (Brush)GetValue(HoverBrushProperty);
            set => SetValue(HoverBrushProperty, value);
        }
        #endregion

        #region CornerRadiusProperty
        public static readonly DependencyProperty BorderRadiusProperty =
            DependencyProperty.Register(nameof(BorderRadius), typeof(CornerRadius), typeof(SearchBox));

        public CornerRadius BorderRadius
        {
            get => (CornerRadius)GetValue(BorderRadiusProperty);
            set => SetValue(BorderRadiusProperty, value);
        }
        #endregion

        #region SearchCommand
        public ICommand SearchCommand
        {
            get => (ICommand)GetValue(SearchCommandProperty);
            set => SetValue(SearchCommandProperty, value);
        }

        public static readonly DependencyProperty SearchCommandProperty =
            DependencyProperty.Register(nameof(SearchCommand), typeof(ICommand), typeof(SearchBox), new PropertyMetadata(null));
        #endregion

        #region CancelCommand
        public ICommand CancelCommand
        {
            get => (ICommand)GetValue(CancelCommandProperty);
            set => SetValue(CancelCommandProperty, value);
        }

        public static readonly DependencyProperty CancelCommandProperty =
            DependencyProperty.Register(nameof(CancelCommand), typeof(ICommand), typeof(SearchBox), new PropertyMetadata(null));
        #endregion

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            InputText = string.Empty;
            textBox.Focus();
        }

        private void TextBoxFocusedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            border.BorderBrush = textBox.IsFocused
                ? BorderInactiveBrush
                : BorderBrush;

            border.BorderThickness = textBox.IsFocused
                ? new Thickness(0, 0, 0, 1)
                : new Thickness(0, 0, 0, 2);

            border.Background = textBox.IsFocused
                ? BorderBackground
                : HoverBrush;
        }
    }
}
