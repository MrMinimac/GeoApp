using Newtonsoft.Json;

namespace GeoAppCore
{
    public class GeoDoc
    {
        public int VerticalScale { get; set; } = 10;
        public List<BoreholeLine> BoreholeLines { get; set; } = new();
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        //public void Save(string path)
        //{
        //    string directory = Path.GetDirectoryName(path);

        //    if (!Directory.Exists(directory))
        //        Directory.CreateDirectory(directory);

        //    File.WriteAllText(path, ToJson());
        //}
    }
}
