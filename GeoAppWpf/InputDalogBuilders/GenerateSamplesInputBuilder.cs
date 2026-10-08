using GeoAppCore;
using GeoAppCore.Models;
using GeoAppWpf.Services;
using GeoAppWpf.Views.Windows;
using GeoUIWpf.Views.Windows;
using System.Windows;

namespace GeoAppWpf.InputDalogBuilders
{
    public class GenerateSamplesInputBuilder
    {
        private static SamplesGeneratorProperties _lastProperties = new();

        public static SamplesGeneratorProperties? Build(IntPtr? ownerHandle = null)
        {
            var sampleLengthItem = new DoublePropertyItem
            {
                Header = "Длина пробы",
                Value = _lastProperties.SampleLength
            };

            var items = new PropertyItem[]
            {
                sampleLengthItem,
            };

            bool result = InputWindow.Show("Генерация проб", items, ownerHandle: ownerHandle);

            if (!result)
                return null;

            _lastProperties = new()
            {
                SampleLength = sampleLengthItem.Value,
            };

            return _lastProperties;
        }
    }

    public enum ExcelExportType
    {
        DataBase,
        OreReserve,
        Documentation,
    }

    public class ExportExcelProperties
    {
        public ExcelExportType ExportType { get; set; }
        public List<BoundaryData> Boundaries { get; set; } = new();

    }

    public class ExportExcelInputBuilder
    {
        private static ExportExcelProperties _lastProperties = new();

        public static ExportExcelProperties? Build(IntPtr? ownerHandle = null)
        {

            var exportType = new SelectionPropertyItem<ExcelExportType>(
                "Экспортировать как",
                Enum.GetValues<ExcelExportType>(),
                _lastProperties.ExportType,
                displayFormatter: op => GetTypeDisplayName(op));

            #region Boundaries
            CustomActionPropertyItem<List<BoundaryData>>? boundaries = null;
            boundaries = new CustomActionPropertyItem<List<BoundaryData>>(
                async () =>
                {
                    var result = await AutoCadService.GetBoundaryAsync();

                    if (result == null)
                        return;

                    if (!result.Success)
                    {
                        MessageBox.Show(
                            result.Error ?? "Не удалось получить контур.",
                            "AutoCAD",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);

                        return;
                    }

                    boundaries!.Value = result.Boundaries;
                    boundaries!.DisplayValue = $"Выбрано контуров: {result.Boundaries.Count}";
                });

            boundaries.Header = "Выберите контур оруденения";
            boundaries.Placeholder = "Выбрать в AutoCAD";
            boundaries.IsVisible = _lastProperties.ExportType == ExcelExportType.OreReserve;
            #endregion

            exportType.ValueChanged += value =>
            {
                boundaries.IsVisible = value == ExcelExportType.OreReserve;
            };

            var items = new PropertyItem[]
            {
                exportType,
                boundaries,
            };

            bool result = InputWindow.Show(
                "Экспорт в Excel",
                items,
                ownerHandle: ownerHandle);

            if (!result)
                return null;

            _lastProperties = new ExportExcelProperties
            {
                ExportType = exportType.Value,
                Boundaries = boundaries.Value ?? new List<BoundaryData>(),
            };

            return _lastProperties;
        }

        private static string GetTypeDisplayName(ExcelExportType type)
        {
            return type switch
            {
                ExcelExportType.DataBase => "База данных",
                ExcelExportType.Documentation => "Первичная документация",
                ExcelExportType.OreReserve => "Подсчет запасов",
                _ => ""
            };
        }
    }
}
