using GeoCadWpf.ViewModels;
using LegendDesignWpf.Core.MVVM;
using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace GeoCadWpf.Views.Controls
{

    public partial class ObjectTreeControl : UserControl
    {
        public ObjectTreeControl()
        {
            InitializeComponent();
            DataContextChanged += ObjectTreeControl_DataContextChanged;
        }

        private void ObjectTreeControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ClearTreeViewSelection(GeoTree);
        }

        private void ClearTreeViewSelection(ItemsControl parent)
        {
            foreach (var item in parent.Items)
            {
                if (parent.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem treeItem)
                {
                    treeItem.IsSelected = false;
                    ClearTreeViewSelection(treeItem);
                }
            }
        }

        #region ItemsSourceProperty
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(ObjectTreeControl));

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }
        #endregion

        #region Selected Node Property

        public object SelectedItem
        {
            get => (object)GetValue(SelectedNodeProperty);
            set => SetValue(SelectedNodeProperty, value);
        }

        public static readonly DependencyProperty SelectedNodeProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(object),
                typeof(ObjectTreeControl), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        #endregion

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SetCurrentValue(SelectedNodeProperty, e.NewValue);
        }
    }
}
