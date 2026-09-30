using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoAppCore.Services
{
    public class Block
    {
        public string Id { get; set; } = string.Empty;
        public List<SectionBorehole> Boreholes { get; set; } = new();
    }

    public class BlockBuilder
    {
        public static List<Block> Build(IEnumerable<BoreholeLine> boreholeLines)
        {
            var boreholeLinesList = boreholeLines
                .OrderBy(w => w.Id.Any(char.IsDigit) ? 0 : 1)
                .ThenBy(w => w.Id, new NaturalNameComparer())
                .ToList();

            var sortedHoles = boreholeLinesList
                .SelectMany(x => x.BuildSections()
                    .Where(s => s.OreInterval?.ConditionResult.IsValid ?? false))
                .ToList();

            var blocks = new List<Block>();

            if (boreholeLinesList.Count == 0)
                return blocks;

            // Первая линия — отдельный блок
            AddBlock(GetHoles(boreholeLinesList[0].Id));

            // Блоки из двух соседних линий
            for (int i = 0; i < boreholeLinesList.Count - 1; i++)
            {
                var currentLine = boreholeLinesList[i];
                var nextLine = boreholeLinesList[i + 1];

                var blockHoles = GetHoles(currentLine.Id);
                blockHoles.AddRange(GetHoles(nextLine.Id));

                AddBlock(blockHoles);
            }

            // Последняя линия — отдельный блок
            if (boreholeLinesList.Count > 1)
            {
                AddBlock(GetHoles(boreholeLinesList[^1].Id));
            }

            return blocks;

            List<SectionBorehole> GetHoles(string lineId)
            {
                return sortedHoles
                    .Where(x => x.Source.BoreholeLineId == lineId)
                    .ToList();
            }

            void AddBlock(List<SectionBorehole> holes)
            {
                if (holes.Count == 0)
                    return;

                blocks.Add(new Block
                {
                    Id = $"{blocks.Count + 1}-C1",
                    Boreholes = holes
                });
            }
        }
    }
}
