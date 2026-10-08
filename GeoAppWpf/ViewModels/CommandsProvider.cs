using GeoAppCore;
using GeoAppCore.Abstractions.Document;
using GeoAppWpf.InputDalogBuilders;
using GeoAppWpf.Models;
using GeoAppWpf.Services;
using GeoAppWpf.Services.Excel;
using LegendDesignWpf.Core.MVVM;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Diagnostics;
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

        private readonly RelayCommand<IEnumerable<BoreholeLine>> _showOreIntervalCommand;
        public ICommand GenerateSampleGrades => _showOreIntervalCommand;

        private readonly RelayCommand<IEnumerable<BoreholeLine>> _autoCadExportCommand;
        public ICommand AutoCadExportCommand => _autoCadExportCommand;

        private readonly RelayCommand<IEnumerable<BoreholeLine>> _generateBoreholesCommand;
        public ICommand GenerateSamplesCommand => _generateBoreholesCommand;

        private readonly RelayCommand<IDocument> _exportExcelCommand;
        public ICommand ExportExcelCommand => _exportExcelCommand;

        #endregion

        public CommandsProvider(IServiceProvider serviceProvider)
        {
            _workspaceManager = serviceProvider.GetRequiredService<WorkspaceManager>();
            _acadExporter = serviceProvider.GetRequiredService<AutoCadService>();

            _importCommand = new(Import);

            _generateBoreholesCommand = new(GenerateBoreholes);

            _exportExcelCommand = new(ExportExcel);

            _autoCadExportCommand = new(AutoCadExport);

            _showOreIntervalCommand = new(GenerateGrade);
        }


        private async Task GenerateGrade(IEnumerable<BoreholeLine> lines)
        {
            var properties = GenerateGradeInputBuilder.Build();

            if (properties == null)
                return;

            GenerateGradeService.Generate(lines, properties);
        }

        private void ExportExcel(IDocument document)
        {
            var properties = ExportExcelInputBuilder.Build();

            if (properties == null)
                return;

            ExcelExporter.Export(document, properties);
        }

        private void GenerateBoreholes(IEnumerable<BoreholeLine> boreholeLines)
        {
            var properties = GenerateSamplesInputBuilder.Build();

            if (properties == null)
                return;

            SamplesGenerator.Generate(boreholeLines, properties);
        }

        private async Task AutoCadExport(IEnumerable<BoreholeLine> boreholeLines)
        {

            var geoDoc = ExportAutoCadInputBuilder.Build(boreholeLines);

            if (geoDoc == null)
                return;

            await _acadExporter.Export(geoDoc);
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
    }
}