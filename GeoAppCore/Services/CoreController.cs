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

        public static readonly string[] Urls = new[]
        {
            "http://localhost:5050/",
            "https://still-math-51e7.xlson-syper-90.workers.dev/",
        };

        public static async Task<bool> CheckAsync()
        {
            return true;
            var response = await _httpClient.PostAsJsonAsync(
                Urls[1],
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
