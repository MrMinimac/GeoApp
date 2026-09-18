using GeoAppWpf.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GeoAppWpf.Views.Components
{
    public partial class ObjectTreeComponent : UserControl
    {
        public ObjectTreeComponent()
        {
            InitializeComponent();
        }

        public IEnumerable<Node> Nodes
        {
            get { return (IEnumerable<Node>)GetValue(NodesProperty); }
            set { SetValue(NodesProperty, value); }
        }

        public static readonly DependencyProperty NodesProperty =
            DependencyProperty.Register(
                nameof(Nodes), 
                typeof(IEnumerable<Node>), 
                typeof(ObjectTreeComponent),
                new PropertyMetadata(null));

        public ICommand SelectNodeCommand
        {
            get { return (ICommand)GetValue(SelectNodeCommandProperty); }
            set { SetValue(SelectNodeCommandProperty, value); }
        }

        public static readonly DependencyProperty SelectNodeCommandProperty =
            DependencyProperty.Register(
                nameof(SelectNodeCommand), 
                typeof(ICommand), 
                typeof(ObjectTreeComponent), 
                new PropertyMetadata(null));

        public string SearchText
        {
            get { return (string)GetValue(SearchTextProperty); }
            set { SetValue(SearchTextProperty, value); }
        }

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(
                nameof(SearchText), 
                typeof(string), 
                typeof(ObjectTreeComponent), 
                new PropertyMetadata(string.Empty));

    }
}
