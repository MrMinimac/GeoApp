using System.IO;

namespace GeoAppWpf.Models
{
    public class LastFileModel
    {
        public string Name { get; set; }
        public string Directory { get; set; }

        public DateTime LastModified { get; set; }

        public string FilePath => Path.Combine(Directory, Name);
    }
}
