using GeoAppCore.Services;
using Microsoft.Win32;
using netDxf;
using System.IO;

namespace GeoAppWpf.Services
{
    internal class DXFService
    {
        public event Action<DxfDocument>? DocumentChanged;
        public event Action<DxfDocument, MacromineDatResult>? DatDocumentChanged;

        private DxfDocument? _document;
        public DxfDocument? Document
        {
            get => _document;
            set
            {
                if (value == null)
                    return;

                _document = value;
                DocumentChanged?.Invoke(value);
            }
        }

        public void Import()
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog
                {
                    Multiselect = true
                };

                dialog.Filter = "DXF files (*.dxf)|*.dxf";

                if (dialog.ShowDialog() == true)
                {
                    foreach (string filePath in dialog.FileNames)
                    {
                        var doc = DxfDocument.Load(filePath);
                        doc.Name = Path.GetFileNameWithoutExtension(filePath);
                        Document = doc;
                    }
                }
            }
            catch (Exception ex)
            {
                LocaleService.ShowError(ex);
            }
        }

        public void ImportDat()
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog
                {
                    Multiselect = true,
                    Filter = "DAT files (*.dat)|*.dat"
                };

                if (dialog.ShowDialog() == true)
                {
                    foreach (string filePath in dialog.FileNames)
                    {
                        var reader = new MacromineDatReader();

                        var result = reader.Read(filePath);

                        if (result.Dxf == null)
                            continue;

                        result.Dxf.Name =
                            Path.GetFileNameWithoutExtension(filePath);

                        // Передаем и DXF, и данные DAT
                        DatDocumentChanged?.Invoke(
                            result.Dxf,
                            result);
                    }
                }
            }
            catch (Exception ex)
            {
                LocaleService.ShowError(ex);
            }
        }
    }
}
