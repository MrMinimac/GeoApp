using GeoAppCore;

namespace GeoAppWpf.Services
{
    public class BoreholesGeneratorProperties
    {
        public bool GenerateSamples { get; set; } = true;
        public double SampleLength { get; set; } = 0.4;
    }

    public class BoreholesGenerator
    {
        public static void Generate(IEnumerable<BoreholeLine> lines, BoreholesGeneratorProperties properties)
        {
            foreach (var line in lines)
            {
                if (properties.GenerateSamples)
                {
                    foreach (var bh in line.Boreholes)
                    {
                        GenerateSamples(bh, properties);
                    }
                }
            }
        }

        private static void GenerateSamples(Borehole borehole, BoreholesGeneratorProperties properties)
        {
            int samplesCount = (int)double.Round(borehole.Deapth / properties.SampleLength, 0);

            int precision = 3;

            for (int i = 0; i < samplesCount; i++)
            {
                double from = Math.Round(i * properties.SampleLength, precision);
                double to = Math.Round((i + 1) * properties.SampleLength, precision);

                borehole.Samples.Add(new Sample
                {
                    Length = Math.Round(to - from, precision),
                    From = from,
                    To = to,
                });
            }

            double currentDepth = Math.Round(samplesCount * properties.SampleLength, precision);
            double remainder = Math.Round(borehole.Deapth - currentDepth, precision);

            if (remainder > 0)
            {
                borehole.Samples.Add(new Sample
                {
                    Length = remainder,
                    From = currentDepth,
                    To = Math.Round(borehole.Deapth, precision),
                });
            }
        }
    }
}
