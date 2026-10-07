using GeoAppCore;
using GeoAppCore.Services;

namespace GeoAppWpf.Services.Excel.Build.Data
{
    public class LevelingBorehole
    {
        private readonly SectionBorehole _borehole;

        public SectionBorehole SectionBorehole => _borehole;

        public LevelingBoreholeGroup? Group { get; private set; } = null;

        public double PureAvgGrade => _borehole.OreInterval?.PureAvgGrade ?? 0;

        public double Thickness => _borehole.OreInterval?.Thickness ?? 0;

        public double PureVertReserve => _borehole.OreInterval?.PureVertReserve ?? 0;

        public double? Impact { get; private set; }
        public double? ImpactLeveling10 { get; private set; }


        public LevelingBorehole(SectionBorehole borehole)
        {
            _borehole = borehole;
        }

        public void SetGroup(LevelingBoreholeGroup group)
        {
            Group = group;
        }

        public void SetLeveling(double impact, double? impactLeveling10)
        {
            Impact = impact;
            ImpactLeveling10 = impactLeveling10;
        }

        public static List<LevelingBorehole> CreateRange(List<SectionBorehole> boreholes)
        {
            var list = new List<LevelingBorehole>();

            foreach (var borehole in boreholes)
                list.Add(new(borehole));

            return list;
        }
    }

    public class LevelingBoreholeGroup
    {
        public int GroupNumber { get; }
        public List<LevelingBorehole> Items { get; }

        public double Thickness => Items.Sum(h => h.Thickness);

        public double PureVertReserve => Items.Sum(h => h.PureVertReserve);

        public double PureAvgGrade => PureVertReserve / Thickness;

        public double? PureVertReserveLeveling10 => PureVertReserve * 0.1;

        public double? Impact => Items.Sum(h => h.Impact);

        public LevelingBoreholeGroup(int groupNumber, List<LevelingBorehole> items)
        {
            GroupNumber = groupNumber;
            Items = items;

            InitializeGroups();
        }

        private void InitializeGroups()
        {
            foreach (var item in Items)
            {
                double impact = item.PureVertReserve != 0
                    ? (item.PureVertReserve / PureVertReserve) * 100
                    : 0;

                double? leveling10 = impact >= 10 ? PureVertReserveLeveling10 / item.Thickness : null;

                item.SetLeveling(impact, leveling10);
                item.SetGroup(this);
            }
        }

        public static List<LevelingBoreholeGroup> BuildMacroGroups(IEnumerable<BoreholeLine> boreholeLines, int minHoles = 25, int maxHoles = 30, int concatMaxHoles = 35)
        {
            var remainingObjects =
                boreholeLines
                .OrderBy(w => w.Id.Any(char.IsDigit) ? 0 : 1)
                .ThenBy(w => w.Id, new NaturalNameComparer())
                .Select(x => x.BuildSections().Where(x => x.OreInterval?.ConditionResult?.IsValid ?? false).ToList())
                .ToList();

            var restPart = new List<SectionBorehole>();

            var resultGroups = new List<LevelingBoreholeGroup>();
            int groupCounter = 1;

            while (remainingObjects.Any())
            {
                var currentGroupItems = new List<LevelingBorehole>();
                int currentGroupHolesCount = 0;

                for (int i = 0; i < remainingObjects.Count; i++)
                {
                    var section = remainingObjects[i];

                    if (currentGroupHolesCount == 0 && section.Count > maxHoles)
                    {
                        var taken = section.Take(maxHoles).ToList();

                        restPart.AddRange(section.Skip(maxHoles));

                        currentGroupItems.AddRange(LevelingBorehole.CreateRange(taken));
                        currentGroupHolesCount += taken.Count;

                        remainingObjects.RemoveAt(i);
                        i--;

                        break;
                    }

                    if (restPart.Count > 0)
                    {
                        var available = maxHoles - currentGroupHolesCount;

                        var taken = restPart.Take(available).ToList();

                        currentGroupItems.AddRange(LevelingBorehole.CreateRange(taken));
                        restPart.RemoveRange(0, taken.Count);

                        currentGroupHolesCount += taken.Count;

                        if (currentGroupHolesCount >= maxHoles)
                            break;

                        continue;
                    }

                    if (currentGroupHolesCount + section.Count <= maxHoles)
                    {
                        currentGroupItems.AddRange(LevelingBorehole.CreateRange(section));
                        currentGroupHolesCount += section.Count;

                        remainingObjects.RemoveAt(i);
                        i--;
                    }

                    if (currentGroupHolesCount >= minHoles)
                        break;
                }

                resultGroups.Add(new(groupCounter, currentGroupItems));
                groupCounter++;
            }

            if (resultGroups.Count >= 2)
            {
                int lastIdx = resultGroups.Count - 1;
                int preLastIdx = resultGroups.Count - 2;

                var lastGroup = resultGroups[lastIdx];
                var preLastGroup = resultGroups[preLastIdx];

                int holesSum = lastGroup.Items.Count + preLastGroup.Items.Count;

                // Если в сумме не больше 35
                if (holesSum <= concatMaxHoles)
                {
                    // 1. Добавляем каркасы из последней группы в предпоследнюю
                    preLastGroup.Items.AddRange(lastGroup.Items);

                    // 2. Удаляем последнюю группу
                    resultGroups.RemoveAt(lastIdx);

                    // 3. Откатываем счетчик групп на 1 назад, так как одну группу мы уничтожили
                    groupCounter--;
                }
            }

            return resultGroups;
        }
    }
}
