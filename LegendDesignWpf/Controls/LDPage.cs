using LegendDesignWpf.Core;
using LegendDesignWpf.Core.Enums;
using LegendDesignWpf.Core.Theme;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LegendDesignWpf.Controls
{
    public class LDPage : ContentControl
    {
        private readonly ITheme _theme;

        static LDPage()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LDPage), new FrameworkPropertyMetadata(typeof(LDPage)));
        }

        public LDPage()
        {
            _theme = LegendDesign.Theme
                ?? throw new InvalidOperationException
                ("LegendDesignWpf not initialized. Call LegendDesign.Initialize(theme) before using LDWindow.");
        }

        #region TitleProperty
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(LDPage),
                new PropertyMetadata(string.Empty));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
        #endregion

        #region HeaderContentProperty
        public static readonly DependencyProperty HeaderContentProperty =
            DependencyProperty.Register(
                nameof(HeaderContent),
                typeof(object),
                typeof(LDPage),
                new PropertyMetadata(null));

        public object HeaderContent
        {
            get => GetValue(HeaderContentProperty);
            set => SetValue(HeaderContentProperty, value);
        }
        #endregion
    }
}
