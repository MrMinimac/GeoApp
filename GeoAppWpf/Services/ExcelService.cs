using GeoAppCore;
using Microsoft.Win32;
using OfficeOpenXml;
using System.Globalization;
using System.IO;
using System.Windows;

namespace GeoAppWpf.Services
{
    public class ExcelService : IExcelService
    {
        public ExcelService()
        {
            ExcelPackage.License.SetNonCommercialPersonal("JustUser");
        }

        public GeoDoc? Load()
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "Excel files (*.xlsx)|*.xlsx";

            if (dialog.ShowDialog() == true)
            {
                string filePath = dialog.FileName;
                var doc = LoadExcel(filePath);

                if (doc == null)
                    return null;

                doc.Name = Path.GetFileName(filePath);
                return doc;
            }

            return null;
        }

        private GeoDoc? LoadExcel(string filePath)
        {
            var doc = new GeoDoc();

            try
            {
                using var package = new ExcelPackage(new FileInfo(filePath));

                foreach (var ws in package.Workbook.Worksheets)
                {
                    var boreholes = LoadBoreholes(ws);

                    if (boreholes == null)
                        continue;

                    var lines = boreholes
                        .GroupBy(x => x.LineNumber)
                        .Select(x => new BoreholeLine
                        {
                            Number = x.Key,
                            Boreholes = x
                                .OrderBy(b => b.Id)
                                .ToList()
                        })
                        .ToList();

                    doc.BoreholeLines.AddRange(lines);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            return doc;
        }

        private List<Borehole>? LoadBoreholes(ExcelWorksheet worksheet)
        {
            Borehole currentBorehole = null;
            var boreholes = new List<Borehole>();
            string oldkey = "";

            var diametr = TryGetDouble(worksheet, 2, 15);
            var fineness = TryGetDouble(worksheet, 2, 16);
            var vScale = TryGetDouble(worksheet, 2, 17);

            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                string key = worksheet.Cells[row, 1].Text;

                if (!string.IsNullOrWhiteSpace(key) && key != oldkey)
                {
                    oldkey = key;
                    currentBorehole = new Borehole
                    {
                        Key = key,
                        LineNumber = TryGetInt(worksheet, row, 2) ?? 0,
                        Id = TryGetInt(worksheet, row, 3) ?? 0,
                        X = ParseDouble(worksheet.Cells[row, 8].Text),
                        Y = ParseDouble(worksheet.Cells[row, 9].Text),
                        Z = ParseDouble(worksheet.Cells[row, 10].Text),
                    };
                    boreholes.Add(currentBorehole);
                }

                if (currentBorehole != null)
                {
                    Sample sample = new Sample();
                    sample.X = ParseDouble(worksheet.Cells[row, 8].Text, currentBorehole.X);
                    sample.Y = ParseDouble(worksheet.Cells[row, 9].Text, currentBorehole.Y);
                    sample.Z = ParseDouble(worksheet.Cells[row, 10].Text, currentBorehole.Z);
                    sample.From = TryGetDouble(worksheet, row, 4);
                    sample.To = TryGetDouble(worksheet, row, 5);
                    sample.Length = TryGetDouble(worksheet, row, 6);
                    sample.Value = ParseDouble(worksheet.Cells[row, 7].Text);
                    sample.Diametr = diametr;
                    sample.Fineness = fineness;
                    sample.Lithologies = ParseLithology(worksheet, row, 11);
                    currentBorehole.Samples.Add(sample);
                }
            }

            return boreholes;
        }

        private List<Lithology> ParseLithology(ExcelWorksheet ws, int row, int column)
        {
            var lithologies = new List<Lithology>();

            var text = ws.Cells[row, column].Text;

            if (string.IsNullOrWhiteSpace(text))
                return lithologies;

            var ids = text.Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (var id in ids)
            {
                if (int.TryParse(id.Trim(), out int value) && Enum.IsDefined(typeof(Lithology), value))
                {
                    lithologies.Add((Lithology)value);
                }
            }

            return lithologies;
        }

        private double TryGetDouble(ExcelWorksheet ws, int row, int column)
        {
            var text = ws.Cells[row, column].Text;

            if (string.IsNullOrEmpty(text))
                throw new InvalidOperationException($"Ячейка ({row};{column}) не содержит никакие данные.");

            text = text.Replace(",", ".");

            if (double.TryParse(text, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }

            throw new InvalidOperationException($"Не удалось получить число из ячейки ({row};{column}), " +
                $"так как значение ({text}) не является числом.");
        }

        private int? TryGetInt(ExcelWorksheet ws, int row, int column)
        {
            var text = ws.Cells[row, column].Text;

            if (string.IsNullOrEmpty(text))
                throw new InvalidOperationException($"Ячейка ({row};{column}) не содержит никакие данные.");

            if (int.TryParse(text, out int result))
            {
                return result;
            }

            throw new InvalidOperationException($"Не удалось получить целое число из ячейки ({row};{column}), " +
                $"так как значение ({text}) не является целым числом.");
        }

        private string TryGetString(ExcelWorksheet ws, int row, int column)
        {
            var text = ws.Cells[row, column].Text;

            if (string.IsNullOrEmpty(text))
                throw new InvalidOperationException($"Ячейка ({row};{column}) не содержит никакие данные.");

            return text;
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
