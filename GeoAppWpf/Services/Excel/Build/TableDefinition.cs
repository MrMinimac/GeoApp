using OfficeOpenXml.Style;
using System.Drawing;

namespace GeoAppWpf.Services.Excel.Build
{
    public sealed class TableDefinition
    {
        public string Name { get; set; } = string.Empty;
        public TableStyle Style { get; set; } = new();

        public List<TableColumn> Columns { get; set; } = [];

        public List<TableRow> Rows { get; set; } = [];

        public bool CanUserSortColumns { get; set; } = true;

        public bool ShowGridLines { get; set; } = true;

        public bool AutoFitColumns { get; set; } = true;

        public bool HasHeader { get; set; } = true;

        public TableHeaderStyle HeaderStyle { get; set; } = new();

        public List<TableMergeRule> MergeRules { get; set; } = [];

        public List<TableMergeRule> HeaderMergeRules { get; set; } = [];

        public List<TableRowStyleRule> RowStyleRules { get; set; } = [];
        public TablePrintSettings? PrintSettings { get; set; }
    }

    public sealed class TablePrintSettings
    {
        public TableOrintation Orientation { get; set; }
        public bool FitToPage { get; set; }
        public int FitToWidth { get; set; }
        public int FitToHeight { get; set; }
    }

    public enum TableOrintation
    {
        Landscape,
        Portrait,
    }

    public sealed class TableRowStyle
    {
        public bool Bold { get; set; }

        public Color? BackgroundColor { get; set; }

        public TableBorderStyle? Border { get; set; }

        public TableHorizontalAlignment? HorizontalAlignment { get; set; }

        public TableVerticalAlignment? VerticalAlignment { get; set; }

        public bool? WrapText { get; set; }

        public double? Height { get; set; }

        public ExcelFillStyle? PatternType { get; set; }
        public Color? PatternColor { get; set; }
    }

    public sealed class TableStyle
    {
        public TableBorderStyle? Border { get; set; }

        public bool WrapText { get; set; }
    }

    public sealed class TableRowStyleRule
    {
        public Func<TableRow, bool> Condition { get; set; }
            = _ => false;

        public string? ColumnKey { get; set; }

        public bool Bold { get; set; }

        public Color? BackgroundColor { get; set; }

        public TableBorderStyle? Border { get; set; }

        public TableHorizontalAlignment? HorizontalAlignment { get; set; }

        public TableVerticalAlignment? VerticalAlignment { get; set; }

        public bool? WrapText { get; set; }
    }

    public sealed class TableBorderStyle
    {
        public bool Top { get; set; }

        public bool Bottom { get; set; }

        public bool Left { get; set; }

        public bool Right { get; set; }

        public Color Color { get; set; } = Color.Black;
    }

    public sealed class TableHeaderStyle
    {
        public string FontName { get; set; } = "Times New Roman";
        public double FontSize { get; set; } = 11;
        public bool Bold { get; set; } = true;

        public TableHorizontalAlignment HorizontalAlignment { get; set; } = TableHorizontalAlignment.Center;
        public TableVerticalAlignment VerticalAlignment { get; set; } = TableVerticalAlignment.Center;

        public Color BackgroundColor { get; set; } = Color.FromArgb(242, 242, 242);

        public TableBorderStyle? Border { get; set; }
        public bool WrapText { get; set; }
        public double Height { get; set; }
    }

    public sealed class TableRow
    {
        public Dictionary<string, object?> Values { get; } = [];
        public TableRowStyle? Style { get; set; }
        public bool Highlight { get; set; } = false;
        public double? Height { get; set; }
    }

    public sealed class TableColumn
    {
        public string Header { get; set; } = string.Empty;

        public string Key { get; set; } = string.Empty;

        public string? Format { get; set; }

        public double Width { get; set; } = double.NaN;

        public string FontName { get; set; } = "Times New Roman";

        public double FontSize { get; set; } = 11;

        public bool Bold { get; set; }

        public bool WrapText { get; set; }

        public TableHorizontalAlignment HorizontalAlignment { get; set; }
            = TableHorizontalAlignment.Center;

        public TableVerticalAlignment VerticalAlignment { get; set; }
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
        public string? ColumnKey { get; set; }

        public string? EndColumnKey { get; set; }

        public bool Vertical { get; set; }

        public bool Horizontal { get; set; }

        public bool Header { get; set; }

        public bool SkipEmpty { get; set; } = true;

        public Func<TableRow, bool>? CanMerge { get; set; }

        public TableHorizontalAlignment? HorizontalAlignment { get; set; } = TableHorizontalAlignment.Center;
        public TableVerticalAlignment? VerticalAlignment { get; set; } = TableVerticalAlignment.Center;
    }
}
