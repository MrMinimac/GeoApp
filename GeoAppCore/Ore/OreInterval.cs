namespace GeoAppCore.Ore
{
    public class OreInterval
    {
        public IReadOnlyList<Sample> Samples { get; }

        /// <summary> Глубина кровли пласта (мощность торфов), м </summary>
        public double From => Samples.First().From;

        /// <summary> Глубина подошвы пласта, м </summary>
        public double To => Samples.Last().To;

        /// <summary> Среднее содержание в пласте песков, г/м³ </summary>
        public double AvgGrade => VertReserv / Samples.Sum(s => s.Length);

        /// <summary> Мощность пласта песков (рудного интервала), м </summary>
        public double Thinkness => To - From;

        /// <summary>
        /// Вертикальный (линейный) запас на пласт, г/м².
        /// </summary>
        public double VertReserv
        {
            get
            {
                if (Thinkness <= 0) return 0;
                return Samples.Sum(s => Math.Max(s.PureAvgGrade, 0) * s.Length);
            }
        }

        /// <summary> Мощность горной массы (торфы + пески), м </summary>
        public double RockMassThickness => From + Thinkness;

        /// <summary> Среднее содержание на горную массу, г/м³ </summary>
        public double AvgRockMassGrade => VertReserv / RockMassThickness;

        public OreInterval(IEnumerable<Sample> samples)
        {
            Samples = samples.OrderBy(s => s.From).ToList();
        }
    }
}
