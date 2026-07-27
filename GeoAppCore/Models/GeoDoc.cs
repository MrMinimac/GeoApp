using Newtonsoft.Json;

namespace GeoAppCore
{
    public class GeoDoc
    {
        public List<BoreholeLine> BoreholeLines { get; set; } = new();

        public int VerticalScale { get; set; } = 10;

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
