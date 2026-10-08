using GeoAppCore;
using GeoUIWpf.Views.Windows;
using System.Windows;

namespace GeoAppWpf.InputDalogBuilders
{
    public class ExportAutoCadInputBuilder
    {
        private static GeoDoc _lastProperties = new();

        public static GeoDoc? Build(IEnumerable<BoreholeLine> boreholeLines, IntPtr? ownerHandle = null)
        {
            var exportType = new SelectionPropertyItem<AcadExportType>(
                "Экспортировать как",
                Enum.GetValues<AcadExportType>(),
                _lastProperties.ExportType,
                displayFormatter: op => GetTypeDisplayName(op));

            var drawHatch = new BoolPropertyItem
            {
                Header = "Рисовать штриховку",
                Value = _lastProperties.DrawHatch,
                IsVisible = _lastProperties.ExportType == AcadExportType.Sections
            };

            exportType.ValueChanged += value =>
            {
                drawHatch.IsVisible = value == AcadExportType.Sections;
            };

            var items = new PropertyItem[]
            {
                exportType,
                drawHatch,
            };

            bool result = InputWindow.Show(
                "Экспорт в AutoCad",
                items,
                ownerHandle: ownerHandle);

            if (!result)
                return null;

            _lastProperties = new()
            {
                DrawHatch = drawHatch.Value,
                ExportType = exportType.Value,
                BoreholeLines = boreholeLines.ToList()
            };

            return _lastProperties;
        }

        private static string GetTypeDisplayName(AcadExportType value)
        {
            return value switch
            {
                AcadExportType.Plan => "План",
                AcadExportType.Sections => "Разрезы",
                _ => ""
            };
        }
    }
}
