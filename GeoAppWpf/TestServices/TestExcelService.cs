using GeoAppCore;
using GeoAppCore.Abstractions.Document;
using GeoAppWpf.Services;

namespace GeoAppWpf.TestServices
{
    public class TestExcelLoader : IDocumentLoader
    {
        public IEnumerable<string> Extensions => [".xlsx"];

        public bool CanLoad(string extension)
            => extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase);

        public IDocument Load(string path)
        {
            var boreholeLines = new List<BoreholeLine>();

            //         ПАРАМЕТРЫ            //

            const double diametr = 146;
            const double fineness = 0.9;
            const int borholeLinesCount = 2;
            const int boreholesCount = 30;
            const int boreholesStep = 2;
            const int samplesCount = 20;

            //         ПАРАМЕТРЫ            //


            double bLineDistX = 71.6;
            double bLineDistY = -82.6;

            for (int i = 1; i <= borholeLinesCount; i++)
            {
                double x = bLineDistX;
                double y = bLineDistY;
                double z = Random.Shared.Next(8900, 9000) / 100.0;

                var boreholes = new List<Borehole>();

                for (int l = 1; l < boreholesCount * boreholesStep; l += 2)
                {
                    x += Random.Shared.Next(150, 250) / 10.0;
                    y += Random.Shared.Next(50, 150) / 10.0;

                    if (l < boreholesCount / 2)
                    {
                        z -= Random.Shared.Next(0, 200) / 100.0;
                    }
                    else
                    {
                        z += Random.Shared.Next(0, 200) / 100.0;
                    }

                    var bh = new Borehole
                    {
                        Id = l,
                        Key = $"{i}_{l}",
                        LineNumber = i,
                        X = Math.Round(x, 4),
                        Y = Math.Round(y, 4),
                        Z = Math.Round(z, 4),
                    };

                    double depth = 0;
                    double accumulated = 0;

                    for (int s = 0; s < samplesCount; s++)
                    {
                        double value = s < 3 ? 0 : Random.Shared.Next(-1000, 1000) / 1000.0;

                        if (value < 0 && value > -0.1)
                            value = -1;
                        else if (value < 0)
                            value = 0;

                        double length;

                        if (s < 3)
                        {
                            length = 0.5;
                        }
                        else if (s >= 3 &&
                                 bh.Samples[s - 1].Value == 0 &&
                                 bh.Samples[s - 2].Value == 0 &&
                                 bh.Samples[s - 3].Value == 0)
                        {
                            // Две предыдущие пустые -> крупный интервал
                            length = 0.5;
                        }
                        else
                        {
                            // Есть минерализация -> детализация
                            length = 0.2;
                        }

                        double from = depth;
                        double to = depth + length;

                        bh.Samples.Add(new Sample
                        {
                            From = Math.Round(from, 1),
                            To = Math.Round(to, 1),
                            Length = length,
                            Value = value,
                            Fineness = fineness,
                            Diametr = diametr,
                            X = Math.Round(x, 4),
                            Y = Math.Round(y, 4),
                            Z = Math.Round(z - to, 4),
                            Lithologies = [Lithology.Slate]
                        });

                        depth = to;
                        accumulated += length;
                    }

                    boreholes.Add(bh);
                }

                boreholeLines.Add(new BoreholeLine { Number = i, Boreholes = boreholes });

                bLineDistX += 71.6;
                bLineDistY += -82.6;
            }

            var document = new ExcelDocument();
            document.Objects.AddRange(boreholeLines);

            return document;
        }
    }
}
