using System.Drawing;

namespace GeoAppWpf.Services.Excel.Build
{
    public sealed class TableDefinition
    {
        public string Name { get; init; } = string.Empty;
        public TableStyle Style { get; init; } = new();

        public List<TableColumn> Columns { get; init; } = [];

        public List<TableRow> Rows { get; init; } = [];

        public bool CanUserSortColumns { get; set; } = true;

        public bool ShowGridLines { get; set; } = true;

        public bool AutoFitColumns { get; set; } = true;

        public bool HasHeader { get; set; } = true;

        public TableHeaderStyle HeaderStyle { get; init; } = new();

        public List<TableMergeRule> MergeRules { get; init; } = [];

        public List<TableMergeRule> HeaderMergeRules { get; init; } = [];

        public List<TableRowStyleRule> RowStyleRules { get; init; } = [];
    }

    public sealed class TableStyle
    {
        public TableBorderStyle? Border { get; init; }

        public bool WrapText { get; init; }
    }

    public sealed class TableRowStyleRule
    {
        public Func<TableRow, bool> Condition { get; init; }
            = _ => false;

        public string? ColumnKey { get; init; }

        public bool Bold { get; init; }

        public Color? BackgroundColor { get; init; }

        public TableBorderStyle? Border { get; init; }

        public TableHorizontalAlignment? HorizontalAlignment { get; init; }

        public TableVerticalAlignment? VerticalAlignment { get; init; }

        public bool? WrapText { get; set; }
    }

    public sealed class TableBorderStyle
    {
        public bool Top { get; init; }

        public bool Bottom { get; init; }

        public bool Left { get; init; }

        public bool Right { get; init; }

        public Color Color { get; init; } = Color.Black;
    }

    public sealed class TableHeaderStyle
    {
        public string FontName { get; init; } = "Times New Roman";
        public double FontSize { get; init; } = 11;
        public bool Bold { get; init; } = true;

        public TableHorizontalAlignment HorizontalAlignment { get; init; } = TableHorizontalAlignment.Center;
        public TableVerticalAlignment VerticalAlignment { get; init; } = TableVerticalAlignment.Center;

        public Color BackgroundColor { get; init; } = Color.FromArgb(242, 242, 242);

        public TableBorderStyle? Border { get; init; }
        public bool WrapText { get; init; }
        public double Height { get; init; }
    }

    public sealed class TableRow
    {
        public Dictionary<string, object?> Values { get; } = [];
        public bool Highlight { get; set; } = false;
        public double? Height { get; set; }
    }

    public sealed class TableColumn
    {
        public string Header { get; init; } = string.Empty;

        public string Key { get; init; } = string.Empty;

        public string? Format { get; init; }

        public double Width { get; init; } = double.NaN;

        public string FontName { get; init; } = "Times New Roman";

        public double FontSize { get; init; } = 11;

        public bool Bold { get; init; }

        public bool WrapText { get; init; }

        public TableHorizontalAlignment HorizontalAlignment { get; init; }
            = TableHorizontalAlignment.Center;

        public TableVerticalAlignment VerticalAlignment { get; init; }
            = TableVerticalAlignment.Center;
    }

    public enum TableHorizontalAlignment
    {
        Left,
        Center,
        Right
    }

    public enum TableVerticalAlignment
    {
        Top,
        Center,
        Bottom
    }

    public sealed class TableMergeRule
    {
        public string? ColumnKey { get; init; }

        public string? EndColumnKey { get; init; }

        public bool Vertical { get; init; }

        public bool Horizontal { get; init; }

        public bool Header { get; init; }

        public bool SkipEmpty { get; init; } = true;

        public Func<TableRow, bool>? CanMerge { get; init; }

        public TableHorizontalAlignment? HorizontalAlignment { get; set; } = TableHorizontalAlignment.Center;
        public TableVerticalAlignment? VerticalAlignment { get; set; } = TableVerticalAlignment.Center;
    }
}
