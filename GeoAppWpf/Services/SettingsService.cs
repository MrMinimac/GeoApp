using GeoAppWpf.Models;
using LegendDesignWpf.Core.Enums;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

namespace GeoAppWpf.Services
{
    public class SettingsService
    {
        public event Action? SettingsChanged;
        private readonly string _settingsFilePath;
        private SettingsModel _model = new();

        private CancellationTokenSource? _saveCts;

        public SettingsService(string directory)
        {
            _settingsFilePath = Path.Combine(directory, "settings.xml");
        }

        // ===== Data =====

        public AppThemes AppTheme
        {
            get => _model.AppTheme;
            set
            {
                bool isFastSave = value == AppThemes.Acrylic || _model.AppTheme == AppThemes.Acrylic;

                _model.AppTheme = value;
                if (isFastSave)
                    _ = SaveAsync();
                else
                    ScheduleSave();
            }
        }

        public AccentSource AccentSource
        {
            get => _model.AccentSource;
            set
            {
                if (_model.AccentSource == value)
                    return;

                _model.AccentSource = value;
                ScheduleSave();
            }
        }

        public string LastDocPath
        {
            get => _model.LastDocPath;
            set
            {
                if (_model.LastDocPath == value)
                    return;

                _model.LastDocPath = value;
                ScheduleSave();
            }
        }

        // ===== Load / Save =====
        public async Task LoadAsync()
        {
            if (!File.Exists(_settingsFilePath))
                return;

            await using var fs = File.OpenRead(_settingsFilePath);
            var serializer = new XmlSerializer(typeof(SettingsModel));
            _model = (SettingsModel)serializer.Deserialize(fs)!;
        }

        private void ScheduleSave()
        {
            _saveCts?.Cancel();
            _saveCts = new CancellationTokenSource();

            _ = SaveDelayedAsync(_saveCts.Token);
        }

        private async Task SaveDelayedAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(1000, token);
                await SaveAsync();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private async Task SaveAsync()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath)!);

            await using var fs = File.Create(_settingsFilePath);
            var serializer = new XmlSerializer(typeof(SettingsModel));
            serializer.Serialize(fs, _model);
            SettingsChanged?.Invoke();
        }
    }
}
