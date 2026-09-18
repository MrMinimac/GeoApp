using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Xml.Linq;

namespace GeoAppCore.Services
{
    public class CoreController
    {
        private static readonly HttpClient _httpClient = new();
        public bool IsReady;

        public static async Task<bool> CheckAsync()
        {
            return true;
            var response = await _httpClient.PostAsJsonAsync(
                "https://still-math-51e7.xlson-syper-90.workers.dev/",
                new
                {
                    key = "B&lx$S-KIp(R!&@A"
                });

            if (!response.IsSuccessStatusCode)
                return false;

            var result = await response.Content.ReadFromJsonAsync<LicenseResponse>();

            return result?.Valid == true;
        }

        private class LicenseResponse
        {
            public bool Valid { get; set; }
        }
    }
}
