namespace GeoAppCore.Ore
{
    public interface IOreCondition
    {
        /// <summary>
        /// Максимально допустимая мощность пустых прослоев внутри пласта (м)
        /// </summary>
        double? MaxWasteThickness { get; }

        /// <summary>
        /// Проверка отдельной пробы на борт (рудная/пустая)
        /// </summary>
        bool IsSampleOre(Sample sample);

        /// <summary>
        /// Проверка накопленного интервала на соответствие кондициям
        /// </summary>
        bool IsIntervalValid(double sumLen, double sumGradeLen, double peatThickness);
    }
}
