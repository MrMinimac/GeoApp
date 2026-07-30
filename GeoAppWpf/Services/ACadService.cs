using GeoAppCore;
using GeoAppCore.Services;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Windows;

namespace GeoAppWpf.Services
{
    public class ACadService
    {
        private GeoDoc? _document;

        public event Action<GeoDoc>? DocumentChanged;

        public GeoDoc? Document
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

        public async Task ExportSections()
        {
            await Export("export-sections");
        }

        public async Task ExportPlan()
        {
            await Export("export-plan");
        }

        private async Task Export(string endpoint)
        {
            if (Document == null)
            {
                MessageBox.Show(LocaleService.Get(Message.AcDocEmp));
                return;
            }

            try
            {
                using HttpClient client = new HttpClient();

                string json = Document.ToJson();

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response =
                    await client.PostAsync($"{CoreController.Urls[0]}{endpoint}", content);

                if (response.IsSuccessStatusCode)
                    MessageBox.Show(LocaleService.Get(Message.AcImpOk));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocaleService.Get(Message.AcImpErr) +
                    $"\n\n{ex.Message}",
                    LocaleService.Get(Message.ErrTittle),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Debug.WriteLine(ex.ToString());
            }
        }
    }
}
