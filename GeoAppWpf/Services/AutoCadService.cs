using GeoAppCore;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Windows;

namespace GeoAppWpf.Services
{
    public class AutoCadService
    {
        private readonly HttpClient _client = new HttpClient
        {
            Timeout = TimeSpan.FromMilliseconds(500)
        };

        public async Task<bool> CheckConnection()
        {
            try
            {
                using var response = await _client.PostAsync("http://localhost:5050/check",null);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task ExportSections(GeoDoc document)
        {
            await Export("export-sections", document);
        }

        public async Task ExportPlan(GeoDoc document)
        {
            await Export("export-plan", document);
        }

        private async Task Export(string endpoint, GeoDoc document)
        {
            if (document == null)
            {
                MessageBox.Show("Документ пуст!");
                return;
            }

            try
            {
                using HttpClient client = new HttpClient();

                string json = document.ToJson();

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync($"http://localhost:5050/{endpoint}", content);

                if (response.IsSuccessStatusCode)
                    MessageBox.Show("Проект отправлен в AutoCAD");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Произошла ошибка при импорте.\nУбедитесь что у вас запущен AutoCad и загружен плагин." +
                    $"\n\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Debug.WriteLine(ex.ToString());
            }
        }
    }
}
