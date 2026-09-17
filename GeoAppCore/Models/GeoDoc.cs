using GeoAppCore.Abstractions.Document;
using GeoAppCore.Models;
using Newtonsoft.Json;

namespace GeoAppCore
{
    public class GeoDoc : IDocument
    {
        public string Name { get; set; }
        public string? FilePath { get; set; }
        public int VerticalScale { get; set; } = 10;

        public List<BoreholeLine> BoreholeLines { get; set; } = new();

        public IEnumerable<GeoObject> GetObjects()
        {
            return BoreholeLines;
        }

        public void Save(string path)
        {
            string directory = Path.GetDirectoryName(path);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, ToJson());
        }

        public static GeoDoc Load(string path)
        {
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<GeoDoc>(json);
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(
                this,
                Formatting.Indented);
        }
    }
}
