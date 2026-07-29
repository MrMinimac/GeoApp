using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace LegendDesignWpf.Behaviors
{
    public class SliderDragBehavior : DependencyObject
    {
        // Attached Property для команды на начало манипуляции (down)
        public static readonly DependencyProperty ManipulationStartedCommandProperty =
            DependencyProperty.RegisterAttached("ManipulationStartedCommand", typeof(ICommand), typeof(SliderDragBehavior),
                new PropertyMetadata(null, OnManipulationStartedCommandChanged));
        public static ICommand GetManipulationStartedCommand(DependencyObject obj) =>
            (ICommand)obj.GetValue(ManipulationStartedCommandProperty);
        public static void SetManipulationStartedCommand(DependencyObject obj, ICommand value) =>
            obj.SetValue(ManipulationStartedCommandProperty, value);

        // Attached Property для команды на конец манипуляции (up)
        public static readonly DependencyProperty ManipulationCompletedCommandProperty =
            DependencyProperty.RegisterAttached("ManipulationCompletedCommand", typeof(ICommand), typeof(SliderDragBehavior),
                new PropertyMetadata(null, OnManipulationCompletedCommandChanged));
        public static ICommand GetManipulationCompletedCommand(DependencyObject obj) =>
            (ICommand)obj.GetValue(ManipulationCompletedCommandProperty);
        public static void SetManipulationCompletedCommand(DependencyObject obj, ICommand value) =>
            obj.SetValue(ManipulationCompletedCommandProperty, value);

        // Хендлер для Started (изменено на AddHandler)
        private static void OnManipulationStartedCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Slider slider)
            {
                slider.RemoveHandler(UIElement.PreviewMouseLeftButtonDownEvent, (MouseButtonEventHandler)OnPreviewMouseLeftButtonDown);

                if (e.NewValue is ICommand)
                {
                    slider.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent,
                                     new MouseButtonEventHandler(OnPreviewMouseLeftButtonDown),
                                     true);
                }
            }
        }

        private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Slider slider && slider.IsEnabled)
            {
                var command = GetManipulationStartedCommand(slider);
                if (command?.CanExecute(null) == true)
                {
                    command.Execute(null);
                }

                e.Handled = false;
            }
        }

        // Хендлер для Completed (аналогично обновлён на AddHandler)
        private static void OnManipulationCompletedCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Slider slider)
            {
                slider.RemoveHandler(UIElement.PreviewMouseLeftButtonUpEvent, (MouseButtonEventHandler)OnPreviewMouseLeftButtonUp);

                if (e.NewValue is ICommand)
                {
                    slider.AddHandler(UIElement.PreviewMouseLeftButtonUpEvent,
                                     new MouseButtonEventHandler(OnPreviewMouseLeftButtonUp),
                                     true);
                }
            }
        }

        private static void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Slider slider && slider.IsEnabled)
            {
                var command = GetManipulationCompletedCommand(slider);
                if (command?.CanExecute(slider.Value) == true)
                {
                    command.Execute(slider.Value);
                }
                e.Handled = false;
            }
        }

        // Helper: рекурсивный поиск предка типа T (можно убрать, если не нужен)
        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}