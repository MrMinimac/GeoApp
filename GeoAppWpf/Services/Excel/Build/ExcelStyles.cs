using System.Drawing;

namespace GeoAppWpf.Services.Excel.Build
{
    public static class ExcelStyles
    {
        public static TableHeaderStyle DefaultHeader => new()
        {
            FontName = "Times New Roman",
            FontSize = 11,
            Bold = true,

            HorizontalAlignment = TableHorizontalAlignment.Center,
            VerticalAlignment = TableVerticalAlignment.Center,

            BackgroundColor = Color.FromArgb(242, 242, 242),

            WrapText = true,
            Height = 60,

            Border = new TableBorderStyle
            {
                Top = true,
                Bottom = true,
                Left = true,
                Right = true,
                Color = Color.Black
            }
        };

        public static TableBorderStyle DefaultBorder => new()
        {
            Top = true,
            Bottom = true,
            Color = Color.Black
        };

        public static TableBorderStyle AllSidesBorder => new()
        {
            Left = true,
            Right = true,
            Top = true,
            Bottom = true,
            Color = Color.Black
        };

        public static TableRowStyleRule DefaultHighlightedCellStyle(string key) => new()
        {
            ColumnKey = key,

            Condition = row =>
            {
                row.Values.TryGetValue(key, out var value);
                return value != null;
            },

            Bold = true,

            BackgroundColor = Color.FromArgb(189, 215, 238),

            Border = new TableBorderStyle
            {
                Top = true,
                Bottom = true,
                Left = true,
                Right = true,
                Color = Color.Black
            }
        };

        public static TableRowStyleRule DefaultPartSpliterRule => new()
        {
            Condition = row =>
            {
                var value = row.Values.FirstOrDefault().Value;
                var text = value?.ToString();
                return text?.StartsWith("Часть", StringComparison.OrdinalIgnoreCase) == true;
            },

            Bold = true,
            BackgroundColor = Color.FromArgb(255, 192, 0),
            Border = ExcelStyles.DefaultBorder
        };

        public static TableMergeRule DefaultPartSpliterMergeRule => new()
        {
            Horizontal = true,
            CanMerge = row =>
            {
                var value = row.Values.FirstOrDefault().Value;
                var text = value?.ToString();
                return text?.StartsWith("Часть", StringComparison.OrdinalIgnoreCase) == true;
            }
        };
    }
}
