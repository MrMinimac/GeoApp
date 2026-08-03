using netDxf;
using netDxf.Entities;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;

namespace GeoAppWpf.Models
{
    public class DxfDocumentNode : GeoTreeNode
    {
        public DxfDocument Document { get; }
        public ObservableCollection<DxfEntityView> Entities { get; } = new();

        public DxfDocumentNode(DxfDocument doc)
        {
            Document = doc;
            Name = doc.Name;

            foreach (var entity in doc.Entities.All)
            {
                Children.Add(new EntitiesNode(entity));
                Entities.Add(new DxfEntityView(entity));
            }
        }

        public void Save(string filePath)
        {
            var culture = CultureInfo.InvariantCulture;

            using (var writer = new StreamWriter(filePath, false, Encoding.Default))
            {
                writer.WriteLine(new string(' ', 40));
                writer.WriteLine("5   VARIABLES");
                writer.WriteLine("EAST      N 12  3");
                writer.WriteLine("NORTH     N 12  3");
                writer.WriteLine("RL        N 12  3");
                writer.WriteLine("STRING    C 20  0");
                writer.WriteLine("JOIN      C 10  0");

                int stringId = 1;

                foreach (var entity in Document.Entities.All)
                {
                    if (entity is not Polyline3D polyline)
                        continue;

                    foreach (var v in polyline.Vertexes)
                    {
                        string east = v.X.ToString("F3", culture);
                        string north = v.Y.ToString("F3", culture);
                        string rl = v.Z.ToString("F3", culture);

                        writer.WriteLine(
                            $"{east,-12}" +
                            $"{north,-12}" +
                            $"{rl,-12}" +
                            $"{string.Empty,-20}" +
                            $"{stringId,-10}"
                        );
                    }

                    stringId++;
                }
            }
        }
    }
}
