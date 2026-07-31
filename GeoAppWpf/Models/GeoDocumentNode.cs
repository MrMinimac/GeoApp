using GeoAppCore;

namespace GeoAppWpf.Models
{
    public class GeoDocumentNode : GeoTreeNode
    {
        public GeoDoc Document { get; }

        public GeoDocumentNode(GeoDoc doc)
        {
            Document = doc;

            foreach (var line in doc.BoreholeLines)
                Children.Add(new BoreholeLineNode(line));
        }
    }
}
