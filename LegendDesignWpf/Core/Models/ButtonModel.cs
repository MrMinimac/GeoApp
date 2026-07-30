using System.Windows.Input;

namespace LegendDesignWpf.Core.Models
{
    public class ButtonModel
    {
        public string Title { get; set; }
        public Func<Task>? Action { get; init; }
    }
}
