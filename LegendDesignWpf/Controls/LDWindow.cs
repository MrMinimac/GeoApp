using LegendDesignWpf.Core;
using LegendDesignWpf.Core.Enums;
using LegendDesignWpf.Core.Theme;
using LegendDesignWpf.WinApi;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LegendDesignWpf.Controls
{
    public class LDWindow : Window
    {
        private readonly ITheme _theme;

        static LDWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LDWindow), new FrameworkPropertyMetadata(typeof(LDWindow)));
        }

        public LDWindow()
        {
            _theme = LegendDesign.Theme 
                ?? throw new InvalidOperationException
                ("LegendDesignWpf not initialized. Call LegendDesign.Initialize(theme) before using LDWindow.");

            Style = (Style)Application.Current.Resources["LDWindowStyle"];
            Loaded += OnLoaded;
            Closed += OnClosed;
            _theme.OnThemeChanged += ApplyTheme;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var minimizeButton = GetTemplateChild("MinimizeButton") as Button;
            if (minimizeButton != null )
            {
                minimizeButton.Click += (_, __) => WindowState = WindowState.Minimized;
            }

            var maximizeButton = GetTemplateChild("MaximizeButton") as Button;
            if (maximizeButton != null)
            {
                maximizeButton.Click += (_, __) =>
                {
                    WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                };
            }

            var closeButton = GetTemplateChild("CloseButton") as Button;
            if (closeButton != null)
            {
                closeButton.Click += (_, __) => Close();
            }


            if (minimizeButton != null && maximizeButton != null)
            {
                if (ResizeMode == ResizeMode.NoResize)
                {
                    minimizeButton.Visibility = Visibility.Collapsed;
                    maximizeButton.Visibility = Visibility.Collapsed;
                }
            }

        }

        private void ApplyTheme(AppThemes theme)
        {
            switch (theme)
            {
                case AppThemes.Dark:
                    EnableDarkToolBar.Enable(this, true);
                    break;
                case AppThemes.Light:
                    EnableDarkToolBar.Enable(this, false);
                    break;
                case AppThemes.Acrylic:
                    ApplyAcrilicTheme();
                    break;
            }
        }

        private void ApplyAcrilicTheme()
        {
            if (EnableAcrylic.IsTransparencyEnabled())
            {
                RoundedCorners.Apply(this, CornerPreference.Round);
                EnableAcrylic.Enable(this, Color.FromArgb(15, 0, 0, 0));
                _ = new WindowResizeHelper(this);
            }
            else
            {
                MessageBox.Show("Акриловая тема не поддерживается вашей системой, " +
                    "включите прозрачность в настройках персонализации.");
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ApplyTheme(_theme.CurrentTheme);
        }
        private void OnClosed(object? sender, EventArgs e)
        {
            _theme.OnThemeChanged -= ApplyTheme;
        }
    }
}
