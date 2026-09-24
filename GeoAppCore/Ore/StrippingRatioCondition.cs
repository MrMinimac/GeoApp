namespace GeoAppCore.Ore
{
    public class StrippingRatioCondition : IOreCondition
    {
        public double SampleCutoff { get; set; } = 0.075;       // Борт для зацепления пробы
        public double BaseMinGrade { get; set; } = 0.104;      // Базовое мин. содержание
        public double WasteGradient { get; set; } = 0.012;     // Градиент на ед. вскрыши
        public double? MaxWasteThickness { get; set; } = null; // Допустимый прослой пустых пород

        public bool IsSampleOre(Sample sample)
        {
            // Используем уже пересчитанное с учетом пробности значение
            return sample.PureAvgGrade >= SampleCutoff;
        }

        public bool IsIntervalValid(double sumLen, double sumGradeLen, double peatThickness)
        {
            if (sumLen <= 0) return false;

            // 1. Среднее содержание по интервалу (х/ч)
            double avgGrade = sumGradeLen / sumLen;

            // 2. Коэффициент вскрыши (Торфы / Пески)
            double strippingRatio = peatThickness / sumLen;

            // 3. Динамическое минимальное содержание по формуле
            double minGradeRequired = Math.Round(BaseMinGrade + (WasteGradient * strippingRatio), 3);

            return avgGrade >= minGradeRequired;
        }
    }
}
