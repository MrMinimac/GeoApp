using GeoAppCore;
using Microsoft.Win32;
using OfficeOpenXml;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace GeoAppWpf.Services
{
    public class ExcelService
    {
        public ExcelService()
        {
            ExcelPackage.License.SetNonCommercialPersonal("JustUser");
        }

        public GeoDoc Load()
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "Excel files (*.xlsx)|*.xlsx";


            if (dialog.ShowDialog() == true)
            {
                string filePath = dialog.FileName;

                return LoadExcel(filePath);
            }

            return null;
        }

        private GeoDoc LoadExcel(string filePath)
        {
            var boreholes = LoadBoreholes(filePath);

            var doc = new GeoDoc();

            var lines =
                boreholes
                .GroupBy(x => x.LineNumber)
                .Select(x => new BoreholeLine
                {
                    Number = x.Key,
                    Boreholes = x
                        .OrderBy(b => b.Id)
                        .ToList()
                })
                .ToList();

            doc.BoreholeLines = lines;

            return doc;
        }

        private List<Borehole> LoadBoreholes(string filePath)
        {
            Borehole currentBorehole = null;
            var boreholes = new List<Borehole>();
            string oldkey = "";

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[0];
                var diametr = int.Parse(worksheet.Cells[2, 11].Text);
                var fineness = double.Parse(worksheet.Cells[2, 12].Text);

                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {
                    string key = worksheet.Cells[row, 1].Text;

                    if (!string.IsNullOrWhiteSpace(key) && key != oldkey)
                    {
                        oldkey = key;
                        currentBorehole = new Borehole();
                        currentBorehole.Key = key;

                        try
                        {
                            currentBorehole.LineNumber =
                                int.Parse(worksheet.Cells[row, 2].Text);
                        }
                        catch
                        {
                            Debug.WriteLine($"{worksheet.Cells[row, 2].Text}");
                        }

                        currentBorehole.Id =
                            int.Parse(worksheet.Cells[row, 3].Text);

                        currentBorehole.X = ParseDouble(worksheet.Cells[row, 8].Text);
                        currentBorehole.Y = ParseDouble(worksheet.Cells[row, 9].Text);
                        currentBorehole.Z = ParseDouble(worksheet.Cells[row, 10].Text);


                        boreholes.Add(currentBorehole);
                    }


                    // Добавляем пробу текущей скважине

                    if (currentBorehole != null)
                    {
                        Sample sample = new Sample();

                        sample.X = ParseDouble(worksheet.Cells[row, 8].Text, currentBorehole.X);
                        sample.Y = ParseDouble(worksheet.Cells[row, 9].Text, currentBorehole.Y);
                        sample.Z = ParseDouble(worksheet.Cells[row, 10].Text);

                        sample.From =
                            double.Parse(worksheet.Cells[row, 4].Text);

                        sample.To =
                            double.Parse(worksheet.Cells[row, 5].Text);

                        sample.Length =
                            double.Parse(worksheet.Cells[row, 6].Text);


                        sample.Value =
                            ParseDouble(worksheet.Cells[row, 7].Text);

                        sample.Diametr = diametr;
                        sample.Fineness = fineness;

                        currentBorehole.Samples.Add(sample);
                    }
                }
            }

            return boreholes;
        }

        private double ParseDouble(string value, double fallback = 0)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            if (value.ToLower().Trim() == "зн")
                return -1;
            else if (value.ToLower().Trim() == "пс")
                return 0;

            value = value.Replace(",", ".");
            return double.Parse(value, CultureInfo.InvariantCulture);
        }
    }
}
