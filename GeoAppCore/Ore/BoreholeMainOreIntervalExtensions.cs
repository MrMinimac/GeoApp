namespace GeoAppCore.Ore
{
    public static class BoreholeMainOreIntervalExtensions
    {
        public static OreInterval? GetMainOreInterval(this Borehole borehole, IOreCondition condition)
        {
            var intervals = borehole.BuildOreIntervals(condition);

            if (intervals.Count == 0)
                return null;

            return intervals
                .OrderByDescending(x => x.Thinkness)
                .ThenByDescending(x => x.AvgGrade)
                .First();
        }
    }
}
