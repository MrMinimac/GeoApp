using LegendDesignWpf.Core;
using LegendDesignWpf.Core.Enums;
using System;
using System.Windows;

namespace GeoUIWpf
{
    public static class GeoUiMain
    {
        private static bool _initialized;

        public static void Initialize()
        {
            try
            {
                if (_initialized)
                    return;

                if (Application.Current == null)
                {
                    _ = new Application
                    {
                        ShutdownMode = ShutdownMode.OnExplicitShutdown
                    };
                }

                InitializeResources();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private static void InitializeResources()
        {
            var resources = new ResourceDictionary
            {
                Source = new Uri(
                    "/GeoUIWpf;component/Resources.xaml",
                    UriKind.Relative)
            };

            Application.Current!
                .Resources
                .MergedDictionaries
                .Add(resources);

            LegendDesign.Theme.ApplyTheme(AppThemes.Dark);
            LegendDesign.Theme.SetAccentSource(AccentSource.Windows);
        }
    }
}