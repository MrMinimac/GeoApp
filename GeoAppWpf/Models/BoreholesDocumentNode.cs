using GeoAppCore;
using GeoAppCore.Abstractions.Document;

namespace GeoAppWpf.Models
{
    public class BoreholesDocumentNode : GeoTreeNode
    {
        public IDocument Document { get; }
        public IEnumerable<BoreholeLine> Boreholes => Document.GetObjects().OfType<BoreholeLine>();

        public BoreholesDocumentNode(IDocument document)
        {
            Document = document;
            Header = document.Name;

            foreach (var obj in document.GetObjects())
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
