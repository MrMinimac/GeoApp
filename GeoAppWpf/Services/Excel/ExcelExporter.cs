using GeoAppCore;
using GeoAppCore.Abstractions.Document;
using GeoAppCore.Services;
using GeoAppWpf.InputDalogBuilders;
using GeoAppWpf.Services.Excel.Build;
using GeoAppWpf.Services.Excel.Build.Data;
using GeoAppWpf.Services.Excel.Build.Tables.BlocksReportTable;
using GeoAppWpf.Services.Excel.Build.Tables.BoreholeDataBaseTable;
using GeoAppWpf.Services.Excel.Build.Tables.BoreholeInfluence;
using GeoAppWpf.Services.Excel.Build.Tables.BoreholeReportTable;
using GeoAppWpf.Services.Excel.Build.Tables.ConditionsTable;
using GeoAppWpf.Services.Excel.Build.Tables.OreReserveTable;
using Microsoft.Win32;
using System.IO;
using System.Reflection.Metadata;
using System.Windows;
using System.Xml.Linq;

namespace GeoAppWpf.Services.Excel
{
    public static class ExcelExporter
    {
        public static void Export(IDocument document, ExportExcelProperties properties)
        {
            switch (properties.ExportType)
            {
                case ExcelExportType.Documentation:
                    ExportDocumentaion(document);
                    break;
                case ExcelExportType.OreReserve:
                    ExportOreReserve(document, properties);
                    break;
                case ExcelExportType.DataBase:
                    ExportDataBase(document);
                    break;
            }
        }

        private static void ExportDataBase(IDocument document)
        {
            var lines = document.GetObjects().OfType<BoreholeLine>();

            if (lines == null || !lines.Any())
                return;

            var table = BoreholeDataBaseTableBuilder.Build(lines);
            SaveTable(document.Name, [table]);
        }

        public static void ExportOreReserve(IDocument document, ExportExcelProperties properties)
        {
            var lines = document.GetObjects().OfType<BoreholeLine>();

            if (lines == null || !lines.Any())
                return;

            var levelingGroups = LevelingBoreholeGroup.BuildMacroGroups(lines);
            var blocks = BlockBuilder.Build(lines, properties.Boundaries);

            var table = ConditionsTableBuilder.Build(lines);
            var table2 = BoreholeInfluenceTableBuilder.Build(levelingGroups);
            var table3 = BlocksReportTableBuilder.Build(blocks, levelingGroups);
            var table4 = OreReserveTableBuilder.Build(blocks, lines);

            SaveTable(document.Name, [table, table2, table3, table4]);
        }

        public static void ExportDocumentaion(IDocument document)
        {
            var lines = document.GetObjects().OfType<BoreholeLine>();

            if (lines == null || !lines.Any())
                return;

            var dialog = new OpenFolderDialog();
            dialog.Title = "Выберите директорию сохранения";

            if (dialog.ShowDialog() != true)
                return;

            foreach (var line in lines)
            {
                var tables = new List<TableDefinition>();

                foreach (var bh in line.Boreholes)
                {
                    tables.Add(BoreholeReportTableBuilder.Build(bh, $"{bh.BoreholeLineId} СКВ-{bh.Id}"));
                }

                var excelDoc = new ExcelDocument();
                excelDoc.Tables.AddRange(tables);
                excelDoc.Save(Path.Combine(dialog.FolderName, $"{line.Id}.xlsx"));
            }
        }

        private static void SaveTable(string name, IEnumerable<TableDefinition> tables)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx",
                    FileName = $"{name}.xlsx"
                };

                if (dialog.ShowDialog() != true)
                    return;

                var document = new ExcelDocument();
                document.Tables.AddRange(tables);
                document.Save(dialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
