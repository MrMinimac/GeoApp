using GeoAppWpf.Models;
using LegendDesignWpf.Core;
using LegendDesignWpf.Core.Theme;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GeoAppWpf.Views.Controls
{
    public class ObjectItemControl : ContentControl
    {
        private readonly ITheme _theme;

        public ObjectItemControl()
        {
            Style = (Style)Application.Current.Resources["ObjectItemControlStyle"];

            _theme = LegendDesign.Theme
                ?? throw new InvalidOperationException
                ("LegendDesignWpf not initialized. Call LegendDesign.Initialize(theme) before using LDWindow.");
        }

        public Node Node
        {
            get => (Node)GetValue(NodeProperty);
            set => SetValue(NodeProperty, value);
        }

        public static readonly DependencyProperty NodeProperty =
            DependencyProperty.Register(
                nameof(Node),
                typeof(Node),
                typeof(ObjectItemControl));



        public ICommand SelectCommand
        {
            get { return (ICommand)GetValue(SelectCommandProperty); }
            set { SetValue(SelectCommandProperty, value); }
        }

        public static readonly DependencyProperty SelectCommandProperty =
            DependencyProperty.Register(nameof(SelectCommand), typeof(ICommand), typeof(ObjectItemControl), new PropertyMetadata(null));


        public Thickness ContentMargin
        {
            get => (Thickness)GetValue(ContentMarginProperty);
            set => SetValue(ContentMarginProperty, value);
        }

        public static readonly DependencyProperty ContentMarginProperty =
            DependencyProperty.Register(
                nameof(ContentMargin),
                typeof(Thickness),
                typeof(ObjectItemControl),
                new PropertyMetadata(new Thickness(0)));

        #region ActionButtonContentProperty
        public static readonly DependencyProperty ActionButtonContentProperty =
            DependencyProperty.Register(
                nameof(ActionButtonContent),
                typeof(object),
                typeof(ObjectItemControl),
                new PropertyMetadata(null));

        public object ActionButtonContent
        {
            get => GetValue(ActionButtonContentProperty);
            set => SetValue(ActionButtonContentProperty, value);
        }
        #endregion
    }
}
