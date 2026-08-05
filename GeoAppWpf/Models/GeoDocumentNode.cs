using GeoAppCore;
using GeoAppWpf.Interfaces;
using GeoAppWpf.ViewModels;

namespace GeoAppWpf.Models
{
    public class GeoDocumentNode : GeoTreeNode
    {
        public GeoDoc Document { get; }

        public GeoDocumentNode(GeoDoc doc, ITreeCommandProvider commandProvider) : base(commandProvider)
        {
            Document = doc;

            foreach (var line in doc.BoreholeLines)
                Children.Add(new BoreholeLineNode(line, CommandProvider));
        }
    }
}
