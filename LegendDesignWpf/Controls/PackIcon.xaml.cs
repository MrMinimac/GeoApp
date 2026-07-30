using LegendDesignWpf.Core.Enums;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LegendDesignWpf.Controls
{
    public partial class PackIcon : UserControl
    {
        public PackIcon()
        {
            InitializeComponent();
            UpdateIcon();
        }

        #region Kind
        public static readonly DependencyProperty KindProperty =
            DependencyProperty.Register(
                nameof(Kind),
                typeof(PackIconKind),
                typeof(PackIcon),
                new PropertyMetadata(PackIconKind.Home, OnKindChanged));

        public PackIconKind Kind
        {
            get => (PackIconKind)GetValue(KindProperty);
            set => SetValue(KindProperty, value);
        }
        #endregion

        private static void OnKindChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PackIcon icon)
                icon.UpdateIcon();
        }

        private void UpdateIcon()
        {
            IconPath.Data = (Geometry)Resources[$"{Kind}Geometry"];
        }
    }
}
