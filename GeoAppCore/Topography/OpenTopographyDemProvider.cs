using System.Globalization;

namespace GeoAppCore.Topography
{
    public sealed class OpenTopographyDemProvider
    {
        private static string _apiKey = "dd9f0e94597d77ee55769cdf963b0d54";

        private const string BaseUrl = "https://portal.opentopography.org/API/globaldem";

        private readonly HttpClient _httpClient;

        public OpenTopographyDemProvider()
        {
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Загружает SRTM GL1 (30 м) для указанной области WGS84
        /// и сохраняет результат в GeoTIFF.
        /// </summary>
        public async Task<string> DownloadSrtm30Async(double south, double north, double west, double east, string outputFile, CancellationToken cancellationToken = default)
        {
            if (south >= north)
                throw new ArgumentException(
                    "South должен быть меньше North.");

            if (west >= east)
                throw new ArgumentException(
                    "West должен быть меньше East.");

            string url =
                $"{BaseUrl}" +
                $"?demtype=SRTMGL1" +
                $"&south={Format(south)}" +
                $"&north={Format(north)}" +
                $"&west={Format(west)}" +
                $"&east={Format(east)}" +
                $"&outputFormat=GTiff" +
                $"&API_Key={Uri.EscapeDataString(_apiKey)}";

            Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);

            using var response = await _httpClient.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string error =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                throw new HttpRequestException(
                    $"OpenTopography вернул " +
                    $"{(int)response.StatusCode} " +
                    $"{response.ReasonPhrase}.\n{error}");
            }

            await using var input =
                await response.Content.ReadAsStreamAsync(
                    cancellationToken);

            await using var output =
                File.Create(outputFile);

            await input.CopyToAsync(
                output,
                cancellationToken);

            return outputFile;
        }

        private static string Format(double value)
        {
            return value.ToString(
                "0.########",
                CultureInfo.InvariantCulture);
        }
    }
}
