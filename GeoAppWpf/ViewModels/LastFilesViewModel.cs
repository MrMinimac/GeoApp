using GeoAppCore.Abstractions.Document;
using GeoAppWpf.Models;
using GeoAppWpf.Services;
using LegendDesignWpf.Core.MVVM;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Bson;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime;
using System.Windows;
using System.Windows.Input;

namespace GeoAppWpf.ViewModels
{
    public class LastFilesViewModel : BaseViewModel
    {
        private readonly WorkspaceManager _workspaceManager;
        private readonly SettingsService _settings;

        public ObservableCollection<LastFileModel> LastFiles { get; } = new();

        private readonly RelayCommand<LastFileModel> _loadFileCommand;
        public ICommand LoadFileCommand => _loadFileCommand;

        private readonly RelayCommand<LastFileModel> _removeFileCommand;
        public ICommand RemoveFileCommand => _removeFileCommand;

        public LastFilesViewModel(IServiceProvider serviceProvider)
        {
            _workspaceManager = serviceProvider.GetRequiredService<WorkspaceManager>();
            _workspaceManager.OnDocumentAdded += OnDocumentAdded;
            _settings = serviceProvider.GetRequiredService<SettingsService>();
            _loadFileCommand = new(LoadFile);
            _removeFileCommand = new(RemoveFile);

            _ = Initialize();
        }

        private void OnDocumentAdded(IDocument document)
        {
            if (string.IsNullOrWhiteSpace(document.FilePath))
            {
                Debug.WriteLine($"File Path is null or white space: {document.FilePath}");
                return;
            }

            if (!File.Exists(document.FilePath))
            {
                Debug.WriteLine($"File doesn't exists {document.FilePath}");
                return;
            }

            string? dir = Path.GetDirectoryName(document.FilePath);

            if (string.IsNullOrEmpty(dir))
            {
                Debug.WriteLine($"Directory is null or white space: {document.FilePath}");
                return;
            }

            string name = Path.GetFileName(document.FilePath);

            var existsModel = _settings.LastFiles
                .Where(x => x.Directory == dir && x.Name == name)
                .FirstOrDefault();

            if (existsModel != null)
            {
                existsModel.LastModified = DateTime.Now;
                return;
            }

            var file = new LastFileModel
            {
                Name = name,
                Directory = dir,
                LastModified = DateTime.Now,
            };

            _settings.AddLastFile(file);
            LastFiles.Add(file);

            while (_settings.LastFiles.Count > 16)
            {
                var lastFileDto = _settings.LastFiles.OrderByDescending(x => x.LastModified).LastOrDefault();
                var lastFile = LastFiles.FirstOrDefault(x => x.LastModified == lastFileDto?.LastModified);

                if (lastFileDto != null)
                    _settings.RemoveLastFile(lastFileDto);

                if (lastFile != null)
                    LastFiles.Remove(lastFile);
            }
        }

        private async Task Initialize()
        {
            await _settings.LoadAsync();
            UpdateFilesList();
        }

        private void UpdateFilesList()
        {
            LastFiles.Clear();

            var lastFiles = _settings.LastFiles.OrderByDescending(x => x.LastModified);

            foreach (var lastFile in lastFiles)
            {
                if (!File.Exists(lastFile.FilePath))
                    return;

                LastFiles.Add(lastFile);
            }
        }

        private void LoadFile(LastFileModel lastFile)
        {
            try
            {
                var path = lastFile.FilePath;

                if (!File.Exists(path))
                {
                    MessageBox.Show("Файл не найден.");
                    return;
                }

                _workspaceManager?.ImportFiles([path]);
                lastFile.LastModified = DateTime.Now;

                var lastFileDto = _settings.LastFiles.FirstOrDefault(x => x == lastFile);

                if (lastFileDto != null)
                {
                    lastFileDto.LastModified = lastFile.LastModified;
                    _ = _settings.SaveAsync();
                }

                UpdateFilesList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void RemoveFile(LastFileModel lastFile)
        {
            var lastFileDto = _settings.LastFiles.FirstOrDefault(x => x == lastFile);

            if (lastFileDto != null)
                _settings.RemoveLastFile(lastFileDto);

            LastFiles.Remove(lastFile);
        }
    }
}
