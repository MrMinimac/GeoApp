using GeoAppCore;
using GeoAppCore.Abstractions.Document;
using GeoAppCore.Services;
using GeoAppWpf.InputDalogBuilders;
using GeoAppWpf.Services;
using GeoAppWpf.Services.Excel.Build;
using GeoAppWpf.Services.Excel.Build.Data;
using GeoAppWpf.Services.Excel.Build.Tables.BlocksReportTable;
using GeoAppWpf.Services.Excel.Build.Tables.BoreholeDataBaseTable;
using GeoAppWpf.Services.Excel.Build.Tables.BoreholeInfluence;
using GeoAppWpf.Services.Excel.Build.Tables.BoreholeReportTable;
using GeoAppWpf.Services.Excel.Build.Tables.ConditionsTable;
using GeoAppWpf.Services.Excel.Build.Tables.OreReserveTable;
using LegendDesignWpf.Core.MVVM;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace GeoAppWpf.ViewModels
{
    public class CommandsProvider
    {
        private readonly WorkspaceManager _workspaceManager;
        private readonly AutoCadService _acadExporter;

        #region Commands

        private readonly RelayCommand _importCommand;
        public ICommand ImportCommand => _importCommand;

        private readonly RelayCommand<IEnumerable<BoreholeLine>> _autoCadExportPlanCommand;
        public ICommand AutoCadExportPlanCommand => _autoCadExportPlanCommand;

        private readonly RelayCommand<IEnumerable<BoreholeLine>> _autoCadExportSectionsCommand;
        public ICommand AutoCadExportSectionsCommand => _autoCadExportSectionsCommand;

        private readonly RelayCommand<IEnumerable<BoreholeLine>> _generateBoreholesCommand;
        public ICommand GenerateBoreholesCommand => _generateBoreholesCommand;

        private readonly RelayCommand<IDocument> _excelExportConditionsTableCommand;
        public ICommand ExcelExportConditionsTableCommand => _excelExportConditionsTableCommand;

        private readonly RelayCommand<IDocument> _excelExportBoreholesDBCommand;
        public ICommand ExcelExportBoreholesDBCommand => _excelExportBoreholesDBCommand;

        private readonly RelayCommand<IDocument> _excelExportBoreholesCommand;
        public ICommand ExcelExportBoreholesCommand => _excelExportBoreholesCommand;

        #endregion

        public CommandsProvider(IServiceProvider serviceProvider)
        {
            _workspaceManager = serviceProvider.GetRequiredService<WorkspaceManager>();
            _acadExporter = serviceProvider.GetRequiredService<AutoCadService>();

            _importCommand = new(Import);

            _autoCadExportPlanCommand = new(AutoCadExportPlan);
            _generateBoreholesCommand = new(GenerateBoreholes);

            _excelExportBoreholesDBCommand = new(ExcelExportBoreholesDB);
            _excelExportBoreholesCommand = new(ExcelExportBoreholes);
            _excelExportConditionsTableCommand = new(ExcelExportConditionsTable);

            _autoCadExportSectionsCommand = new(AutoCadExportSections);
        }

        private void ExcelExportBoreholes(IDocument document)
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

        private void ExcelExportConditionsTable(IDocument document)
        {
            var lines = document.GetObjects().OfType<BoreholeLine>();

            if (lines == null || !lines.Any())
                return;

            var levelingGroups = LevelingBoreholeGroup.BuildMacroGroups(lines);
            var blocks = BlockBuilder.Build(lines);

            var table = ConditionsTableBuilder.Build(lines);
            var table2 = BoreholeInfluenceTableBuilder.Build(levelingGroups);
            var table3 = BlocksReportTableBuilder.Build(blocks, levelingGroups);
            var table4 = OreReserveTableBuilder.Build(blocks, lines);

            SaveTable(document.Name, [table, table2, table3, table4]);
        }

        private void ExcelExportBoreholesDB(IDocument document)
        {
            var lines = document.GetObjects().OfType<BoreholeLine>();

            if (lines == null || !lines.Any())
                return;

            var table = BoreholeDataBaseTableBuilder.Build(lines);
            SaveTable(document.Name, [table]);
        }

        private void GenerateBoreholes(IEnumerable<BoreholeLine> boreholeLines)
        {
            var properties = GenerateBoreholesInputBuilder.Build();

            if (properties == null)
                return;

            BoreholesGenerator.Generate(boreholeLines, properties);
        }

        private async Task AutoCadExportPlan(IEnumerable<BoreholeLine> boreholeLines)
        {
            var geoDoc = new GeoDoc
            {
                BoreholeLines = boreholeLines.ToList()
            };

            await _acadExporter.ExportPlan(geoDoc);
        }

        private async Task AutoCadExportSections(IEnumerable<BoreholeLine> boreholeLines)
        {
            var geoDoc = new GeoDoc
            {
                BoreholeLines = boreholeLines.ToList()
            };

            await _acadExporter.ExportSections(geoDoc);
        }

        private void Import()
        {
            var dialog = new OpenFileDialog
            {
                Filter = _workspaceManager?.GetOpenFileFilter(),
                Multiselect = true
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                _workspaceManager?.ImportFiles(dialog.FileNames);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void SaveTable(string name, IEnumerable<TableDefinition> tables)
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