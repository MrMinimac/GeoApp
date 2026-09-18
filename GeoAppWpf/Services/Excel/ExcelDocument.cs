using GeoAppCore.Abstractions.Document;
using GeoAppCore.Models;
using GeoAppWpf.Services.Excel;
using GeoAppWpf.Services.Excel.Build;
using OfficeOpenXml;
using System.IO;

namespace GeoAppWpf.Services
{
    public class ExcelDocument : IDocument
    {
        public List<GeoObject> Objects { get; init; } = new();

        public string Name { get; set; } = "Untitled";

        public string? FilePath { get; set; }

        public IEnumerable<GeoObject> GetObjects() => Objects;

        public List<TableDefinition> Tables { get; } = new();

        public void Save(string filePath)
        {
            ExcelPackage.License.SetNonCommercialPersonal("MrMinimac.GeoEditor");

            using var package = new ExcelPackage();

            foreach (var table in Tables)
            {
                string sheetName = table.Name;
                int counter = 2;

                while (package.Workbook.Worksheets.Any(ws => ws.Name == sheetName))
                {
                    sheetName = $"{table.Name} {counter}";
                    counter++;
                }

                // Добавляем лист с уже гарантированно уникальным именем
                var worksheet = package.Workbook.Worksheets.Add(sheetName);
                ExcelWorksheetBuilder.Build(worksheet, table);
            }

            package.SaveAs(new FileInfo(filePath));
        }
    }
}
