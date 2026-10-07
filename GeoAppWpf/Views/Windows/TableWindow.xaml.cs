using LegendDesignWpf.Controls;
using System.Collections;
using System.Windows;

namespace GeoAppWpf.Views.Windows
{
    public partial class TableWindow : LDWindow
    {
        public static readonly DependencyProperty DisplayItemsProperty =
            DependencyProperty.Register(
                nameof(DisplayItems),
                typeof(ICollection),
                typeof(TableWindow),
                new PropertyMetadata(null));

        public ICollection? DisplayItems
        {
            get => (ICollection?)GetValue(DisplayItemsProperty);
            set => SetValue(DisplayItemsProperty, value);
        }

        public TableWindow()
        {
            InitializeComponent();
        }
    }
}
