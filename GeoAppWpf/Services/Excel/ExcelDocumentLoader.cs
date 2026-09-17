using GeoAppCore.Abstractions.Document;
using OfficeOpenXml;

namespace GeoAppWpf.Services
{
    public class ExcelDocumentLoader : IDocumentLoader
    {
        public IEnumerable<string> Extensions => [".xlsx"];

        public ExcelDocumentLoader()
        {
            ExcelPackage.License.SetNonCommercialPersonal("JustUser");
        }

        public bool CanLoad(string extension)
            => extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase);

        public IDocument Load(string path)
        {
            try
            {
                return ExcelReader.ReadExcelDocument(path);
            }
            catch (Exception ex)
            {
                throw new Exception("Не удалось извлечь данные из таблицы.", ex);
            }
        }
    }
}
