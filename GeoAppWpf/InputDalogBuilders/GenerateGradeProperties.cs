using GeoAppCore.Models;

namespace GeoAppWpf.InputDalogBuilders
{
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
}
