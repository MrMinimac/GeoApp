using CommunityToolkit.Mvvm.Input;
using GeoAppCore;
using GeoAppCore.Models;
using GeoAppWpf.Services;
using GeoAppWpf.Views.Windows;
using System.Windows;

namespace GeoAppWpf.InputDalogBuilders
{
    public class GenerateBoreholesInputBuilder
    {
        private static BoreholesGeneratorProperties _lastProperties = new();

        public static BoreholesGeneratorProperties? Build(Window? owner = null)
        {
            var generateSamplesItem = new BoolPropertyItem
            {
                Header = "Сгенирировать пробы",
                Value = _lastProperties.GenerateSamples,
            };

            var sampleLengthItem = new DoublePropertyItem
            {
                Header = "Длина пробы",
                Value = _lastProperties.SampleLength,
                IsVisible = generateSamplesItem.Value
            };

            generateSamplesItem.ValueChanged += value =>
            {
                sampleLengthItem.IsVisible = value;
            };

            var items = new PropertyItem[]
            {
                generateSamplesItem,
                sampleLengthItem,
            };

            bool result = InputWindow.Show(
                "Генерация",
                items,
                owner: owner);

            if (!result)
                return null;

            _lastProperties = new()
            {
                GenerateSamples = generateSamplesItem.Value,
                SampleLength = sampleLengthItem.Value,
            };

            return _lastProperties;
        }
    }

    public class GeoDocInputBuilder
    {
        private static GeoDoc _lastProperties = new();

        public static GeoDoc? Build(IEnumerable<BoreholeLine> boreholeLines, Window? owner = null)
        {
            var drawHatch = new BoolPropertyItem
            {
                Header = "Рисовать штриховку",
                Value = _lastProperties.DrawHatch,
            };


            var items = new PropertyItem[]
            {
                drawHatch,
            };

            bool result = InputWindow.Show(
                "Генерация",
                items,
                owner: owner);

            if (!result)
                return null;

            _lastProperties = new()
            {
                DrawHatch = drawHatch.Value,
                BoreholeLines = boreholeLines.ToList()
            };

            return _lastProperties;
        }
    }

    public class GenerateGradeProperties
    {
        public List<BoundaryData> Boundaries { get; set; } = new();

        /// <summary>
        /// Общий требуемый запас по всем блокам, кг.
        /// </summary>
        public double OreReserve { get; set; } = 100;

        /// <summary>
        /// Минимальная желаемая мощность руды на скважину.
        /// </summary>
        public double MinOreThickness { get; set; } = 0.8;

        /// <summary>
        /// Максимальная желаемая мощность руды на скважину.
        /// </summary>
        public double MaxOreThickness { get; set; } = 1.2;

        /// <summary>
        /// Минимальное генерируемое значение Grade.
        /// </summary>
        public double MinGrade { get; set; } = 1.0;

        /// <summary>
        /// Максимальное генерируемое значение Grade.
        /// </summary>
        public double MaxGrade { get; set; } = 5.0;

        /// <summary>
        /// Если мощность РКП больше этого значения,
        /// появляется возможность захватить верхнюю часть РКП.
        /// </summary>
        public double MinRkpThicknessForUpperOre { get; set; } = 0.4;

        /// <summary>
        /// Вероятность попадания в верхнюю часть РКП, %.
        /// </summary>
        public double RkpUpperPartProbability { get; set; } = 50;

        /// <summary>
        /// Максимальное количество проб в верхней части РКП.
        /// Фактическое количество выбирается случайно от 1 до этого значения.
        /// </summary>
        public int MaxRkpSamples { get; set; } = 1;

        /// <summary>
        /// Максимальная доля РКП, которую может занять руда, %.
        /// Например, 50 означает не более половины мощности РКП.
        /// </summary>
        public double MaxRkpOrePercent { get; set; } = 50;
    }

    public class GenerateGradeInputBuilder
    {
        private static GenerateGradeProperties _lastProperties = new();

        public static GenerateGradeProperties? Build(Window? owner = null)
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
                owner: owner);

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
