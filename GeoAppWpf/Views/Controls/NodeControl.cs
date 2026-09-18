using GeoAppWpf.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GeoAppWpf.Views.Controls
{
    public class NodeControl : ContentControl
    {
        public NodeControl()
        {
            Style = (Style)Application.Current.Resources["NodeControlStyle"];
        }

        public string Header
        {
            get { return (string)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(nameof(Header), typeof(string), typeof(NodeControl), new PropertyMetadata("NewItem"));


        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(NodeControl), new PropertyMetadata(false));

        public bool IsExpandToggleVisible
        {
            get { return (bool)GetValue(IsExpandToggleVisibleProperty); }
            set { SetValue(IsExpandToggleVisibleProperty, value); }
        }

        public static readonly DependencyProperty IsExpandToggleVisibleProperty =
            DependencyProperty.Register(nameof(IsExpandToggleVisible), typeof(bool), typeof(NodeControl), new PropertyMetadata(false));

        public bool IsExpanded
        {
            get { return (bool)GetValue(IsExpandedProperty); }
            set { SetValue(IsExpandedProperty, value); }
        }

        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(NodeControl), new PropertyMetadata(false));

        public Thickness ContentMargin
        {
            get => (Thickness)GetValue(ContentMarginProperty);
            set => SetValue(ContentMarginProperty, value);
        }

        public static readonly DependencyProperty ContentMarginProperty =
            DependencyProperty.Register(
                nameof(ContentMargin),
                typeof(Thickness),
                typeof(NodeControl),
                new PropertyMetadata(new Thickness(0)));

        public ICommand SelectCommand
        {
            get { return (ICommand)GetValue(SelectCommandProperty); }
            set { SetValue(SelectCommandProperty, value); }
        }

        public static readonly DependencyProperty SelectCommandProperty =
            DependencyProperty.Register(nameof(SelectCommand), typeof(ICommand), typeof(NodeControl), new PropertyMetadata(null));

        public Node? Node
        {
            get => (Node)GetValue(NodeProperty);
            set => SetValue(NodeProperty, value);
        }

        public static readonly DependencyProperty NodeProperty =
            DependencyProperty.Register(
                nameof(Node),
                typeof(Node),
                typeof(NodeControl),
                new PropertyMetadata(null));


        #region ToolTipText
        public static readonly DependencyProperty ToolTipTextProperty =
            DependencyProperty.Register(
                nameof(ToolTipText),
                typeof(string),
                typeof(NodeControl),
                new PropertyMetadata(string.Empty));

        public string ToolTipText
        {
            get => (string)GetValue(ToolTipTextProperty);
            set => SetValue(ToolTipTextProperty, value);
        }
        #endregion

        #region ActionButtonContentProperty
        public static readonly DependencyProperty ActionButtonContentProperty =
            DependencyProperty.Register(
                nameof(ActionButtonContent),
                typeof(object),
                typeof(NodeControl),
                new PropertyMetadata(null));

        public object ActionButtonContent
        {
            get => GetValue(ActionButtonContentProperty);
            set => SetValue(ActionButtonContentProperty, value);
        }
        #endregion

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.ChangedButton != MouseButton.Left || Node == null)
                return;

            var request = new NodeSelectionRequest(Node, Keyboard.Modifiers);

            if (SelectCommand?.CanExecute(request) == true)
                SelectCommand.Execute(request);
        }
    }
}
