using GeoAppCore;
using GeoAppCore.Abstractions.Document;
using GeoAppWpf.ViewModels;
using LegendDesignWpf.Core.MVVM;
using System.Windows.Input;

namespace GeoAppWpf.Models
{
    public class BoreholesDocumentNode : Node
    {
        public IDocument Document { get; }
        public IEnumerable<BoreholeLine> Boreholes => Document.GetObjects().OfType<BoreholeLine>();

        private readonly RelayCommand _removeNodeCommand;
        public ICommand RemoveNodeCommand => _removeNodeCommand;

        public override IReadOnlyList<NodeMenuItem> MenuItems => new NodeMenuItem[]
        {
            new()
            {
                Header = "Генерация данных",
                Items =
                {
                    new()
                    {
                        Header = "Генерация проб",
                        Command = CommandsProvider.GenerateSamplesCommand,
                        CommandParameter = Boreholes
                    },
                    new()
                    {
                        Header = "Генерация содержаний",
                        Command = CommandsProvider.GenerateSampleGrades,
                        CommandParameter = Boreholes
                    },
                },

            },
            new()
            {
                Header = "Экспорт",
                Items =
                {
                    new()
                    {
                        Header = "Экспорт в Excel",
                        Command = CommandsProvider.ExportExcelCommand,
                        CommandParameter = Document
                    },
                    new()
                    {
                        Header = "Экспорт в AutoCad",
                        Command = CommandsProvider.AutoCadExportCommand,
                        CommandParameter = Boreholes
                    },
                },
            },
            new()
            {
                Header = "Удалить",
                Command = RemoveNodeCommand,
                CommandParameter = this
            },
        };

        public event Action<BoreholesDocumentNode>? OnRemoveRequested;

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

                _removeNodeCommand = new(RequestRemove);
            }
        }

        private void RequestRemove()
        {
            OnRemoveRequested?.Invoke(this);
        }
    }
}
