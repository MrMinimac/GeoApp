using GeoAppCore;
using GeoAppWpf.Services;

namespace GeoAppWpf.TestServices
{
    public class TestExcelService : IExcelService
    {
        public GeoDoc? Load()
        {
            var doc = new GeoDoc();

            double x = 0;
            double y = 0;

            for (int i = 1; i <= 2; i++)
            {
                var boreholes = new List<Borehole>();

                for (int l = 1; l < 60; l += 2)
                {
                    x += Random.Shared.Next(140, 150) / 10.0;
                    y += Random.Shared.Next(240, 250) / 10.0;

                    double z = Random.Shared.Next(70000, 90000) / 100.0;

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

                    for (int s = 0; s < 20; s++)
                    {
                        double value = s < 3 ? 0 : Random.Shared.Next(-1000, 1000) / 1000.0;

                        if (value < 0 && value > -0.1)
                            value = -1;
                        else if (value < 0)
                            value = 0;

                        double length;

                        if (s < 2)
                        {
                            length = 0.5;
                        }
                        else if (s >= 2 &&
                                 bh.Samples[s - 1].Value == 0 &&
                                 bh.Samples[s - 2].Value == 0)
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
                            Fineness = 0.9,
                            Diametr = 90,
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

                doc.BoreholeLines.Add(new BoreholeLine { Number = i, Boreholes = boreholes });
            }

            return doc;
        }
    }

}
