using GeoAppCore;
using GeoAppCore.Services;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace GeoAppWpf.Services
{
    public class ACadService
    {
        public GeoDoc? Document { get; set; }

        public bool LoadDocument()
        {
            var el = new ExcelService();
            Document = el.Load();

            if (Document == null)
                return false;

            return true;
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
