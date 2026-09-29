namespace GeoAppCore.Ore
{
    public class OreInterval
    {
        public IReadOnlyList<Sample> Samples { get; }

        public IOreConditionResult ConditionResult { get; }

        /// <summary> Глубина кровли пласта (мощность торфов), м </summary>
        public double From => Samples.First().From;

        /// <summary> Глубина подошвы пласта, м </summary>
        public double To => Samples.Last().To;

        /// <summary> Среднее содержание в пласте песков, г/м³ </summary>
        public double AvgGrade => VertReserv / Samples.Sum(s => s.Length);

        /// <summary> Среднее содержание в пласте песков, г/м³ (х. ч.) </summary>
        public double PureAvgGrade => PureVertReserv / Samples.Sum(s => s.Length);

        /// <summary> Мощность пласта песков (рудного интервала), м </summary>
        public double Thinkness => To - From;

        /// <summary>
        /// Вертикальный (линейный) запас на пласт, г/м².
        /// </summary>
        public double PureVertReserv
        {
            get
            {
                if (Thinkness <= 0) return 0;
                return Samples.Sum(s => Math.Max(s.PureAvgGrade, 0) * s.Length);
            }
        }

        /// <summary>
        /// Вертикальный (линейный) запас на пласт, г/м².
        /// </summary>
        public double VertReserv
        {
            get
            {
                if (Thinkness <= 0) return 0;
                return Samples.Sum(s => Math.Max(s.AvgGrade, 0) * s.Length);
            }
        }

        /// <summary> Мощность горной массы (торфы + пески), м </summary>
        public double RockMassThickness => From + Thinkness;

        /// <summary> Среднее содержание на горную массу, г/м³ </summary>
        public double AvgRockMassGrade => PureVertReserv / RockMassThickness;

        public OreInterval(IEnumerable<Sample> samples, IOreConditionResult conditionResult)
        {
            Samples = samples.OrderBy(s => s.From).ToList();
            ConditionResult = conditionResult;
        }
    }
}
