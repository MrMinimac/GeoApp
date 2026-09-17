using GeoAppCore;
using GeoAppCore.Abstractions.Document;

namespace GeoAppWpf.Models
{
    public class GeoDocumentNode : GeoTreeNode
    {
        public IDocument Document { get; }

        public GeoDocumentNode(IDocument doc)
        {
            Document = doc;
            Header = doc.Name;

            foreach (var obj in doc.GetObjects())
            {
                var node = obj switch
                {
                    BoreholeLine line => new BoreholeLineNode(line),
                    _ => throw new Exception("Объект не поддерживается")
                };

                Children.Add(node);
            }
        }
    }
}
