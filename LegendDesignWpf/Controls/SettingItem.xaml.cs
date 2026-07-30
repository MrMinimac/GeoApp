using LegendDesignWpf.Core.Enums;
using System.Windows;
using System.Windows.Controls;

namespace LegendDesignWpf.Controls
{
    public partial class SettingItem : UserControl
    {
        public SettingItem()
        {
            InitializeComponent();
        }

        #region TitleProperty
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(SettingItem));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
        #endregion

        #region DescriptionProperty
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(SettingItem), new PropertyMetadata(string.Empty));

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }
        #endregion

        #region ControlContentProperty
        public static readonly DependencyProperty ControlContentProperty =
            DependencyProperty.Register(
                nameof(ControlContent),
                typeof(object),
                typeof(SettingItem),
                new PropertyMetadata(null));

        public object ControlContent
        {
            get => GetValue(ControlContentProperty);
            set => SetValue(ControlContentProperty, value);
        }
        #endregion

        #region ContentPositionProperty
        public static readonly DependencyProperty ContentPositionProperty =
            DependencyProperty.Register(nameof(ContentPosition), typeof(ContentPosition), typeof(SettingItem), new PropertyMetadata(ContentPosition.Right));

        public ContentPosition ContentPosition
        {
            get => (ContentPosition)GetValue(ContentPositionProperty);
            set => SetValue(ContentPositionProperty, value);
        }
        #endregion


    }
}
