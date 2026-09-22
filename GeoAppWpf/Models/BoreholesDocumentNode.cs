using GeoAppCore;
using GeoAppCore.Abstractions.Document;
using GeoAppWpf.ViewModels;

namespace GeoAppWpf.Models
{
    public class BoreholesDocumentNode : Node
    {
        public IDocument Document { get; }
        public IEnumerable<BoreholeLine> Boreholes => Document.GetObjects().OfType<BoreholeLine>();

        public override IReadOnlyList<NodeMenuItem> MenuItems => new NodeMenuItem[]
        {
            new()
            {
                Header = "Генерация данных",
                Command = CommandsProvider.GenerateBoreholesCommand,
                CommandParameter = Boreholes
            },
            new()
            {
                Header = "Экспорт в Excel",
                Command = CommandsProvider.ExcelExportBoreholesCommand,
                CommandParameter = Document
            },
            new()
            {
                Header = "Экспорт план в AutoCad",
                Command = CommandsProvider.AutoCadExportPlanCommand,
                CommandParameter = Boreholes
            },
            new()
            {
                Header = "Экспорт разрез в AutoCad",
                Command = CommandsProvider.AutoCadExportSectionsCommand,
                CommandParameter = Boreholes
            },
        };

        public BoreholesDocumentNode(IDocument document, CommandsProvider cmdProvider)
            : base(document.Name, cmdProvider)
        {
            {
                Document = document;

                foreach (var obj in document.GetObjects())
                {
                    var node = obj switch
                    {
                        BoreholeLine line => new BoreholeLineNode(line, cmdProvider),
                        _ => throw new Exception("Объект не поддерживается")
                    };

                    Children.Add(node);
                }
            }
        }
    }
}
