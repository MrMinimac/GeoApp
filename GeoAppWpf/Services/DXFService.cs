using GeoAppCore;
using Microsoft.Win32;
using netDxf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoAppWpf.Services
{
    internal class DXFService
    {
        public event Action<DxfDocument>? DocumentChanged;

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
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "DXF files (*.dxf)|*.dxf";

            if (dialog.ShowDialog() == true)
            {
                string filePath = dialog.FileName;
                Document = DxfDocument.Load(filePath);
            }
        }
    }
}
