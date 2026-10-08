using GeoAppCore.Models;
using GeoAppWpf.Services;
using GeoUIWpf.Views.Windows;
using System.Windows;

namespace GeoAppWpf.InputDalogBuilders
{
    public class GenerateGradeInputBuilder
    {
        private static GenerateGradeProperties _lastProperties = new();

        public static GenerateGradeProperties? Build(IntPtr? ownerHandle = null)
        {
            var oreReserveItem = new DoublePropertyItem
            {
                Header = "Запас ПИ в кг",
                Value = _lastProperties.OreReserve
            };

            var minThicknessItem = new DoublePropertyItem
            {
                Header = "Мин. мощность пласта",
                Value = _lastProperties.MinOreThickness
            };

            var maxThicknessItem = new DoublePropertyItem
            {
                Header = "Макс. мощность пласта",
                Value = _lastProperties.MaxOreThickness
            };

            var minGradeItem = new DoublePropertyItem
            {
                Header = "Мин. содержание в пробе",
                Value = _lastProperties.MinGrade
            };

            var maxGradeItem = new DoublePropertyItem
            {
                Header = "Макс. содержание в пробе",
                Value = _lastProperties.MaxGrade
            };

            var minRkpThicknessItem = new DoublePropertyItem
            {
                Header = "Мин. мощность РКП",
                Value = _lastProperties.MinRkpThicknessForUpperOre
            };

            var rkpUpperPartProbabilityItem = new DoublePropertyItem
            {
                Header = "Шанс попадания пробы в РКП",
                Value = _lastProperties.RkpUpperPartProbability
            };

            var maxRkpSamples = new IntegerPropertyItem
            {
                Header = "Макс. проб в РКП",
                Value = _lastProperties.MaxRkpSamples
            };

            var maxRkpOrePercent = new DoublePropertyItem
            {
                Header = "Макс. доля проб в РКП",
                Value = _lastProperties.MaxRkpOrePercent
            };

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
            #endregion

            var items = new PropertyItem[]
            {
                oreReserveItem,
                minThicknessItem,
                maxThicknessItem,
                minGradeItem,
                maxGradeItem,
                minRkpThicknessItem,
                rkpUpperPartProbabilityItem,
                maxRkpSamples,
                maxRkpOrePercent,
                boundaries,
            };

            bool result = InputWindow.Show(
                "Генерация",
                items,
                ownerHandle: ownerHandle);

            if (!result)
                return null;

            _lastProperties = new GenerateGradeProperties
            {
                Boundaries = boundaries.Value ?? new List<BoundaryData>(),
                OreReserve = oreReserveItem.Value,
                MinOreThickness = minThicknessItem.Value,
                MaxOreThickness = maxThicknessItem.Value,
                MinGrade = minGradeItem.Value,
                MaxGrade = maxGradeItem.Value,
                MinRkpThicknessForUpperOre = minRkpThicknessItem.Value,
                RkpUpperPartProbability = rkpUpperPartProbabilityItem.Value,
                MaxRkpSamples = maxRkpSamples.Value,
                MaxRkpOrePercent = maxRkpOrePercent.Value,

            };

            return _lastProperties;
        }
    }
}
