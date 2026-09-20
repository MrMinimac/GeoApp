using GeoAppWpf.Services.Excel.Build;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace GeoAppWpf.Services.Excel
{
    public class ExcelWorksheetBuilder
    {
        public static void Build(ExcelWorksheet worksheet, TableDefinition tableDefinition)
        {
            ApplyTableSettings(worksheet, tableDefinition);

            if (tableDefinition.HasHeader)
            {
                WriteHeader(worksheet, tableDefinition);
                ApplyHeaderMergeRules(worksheet, tableDefinition);
                ApplyHeaderStyle(worksheet, tableDefinition);
            }

            WriteRows(worksheet, tableDefinition);

            ApplyMergeRules(worksheet, tableDefinition);

            ApplyTableStyle(worksheet, tableDefinition);

            if (tableDefinition.AutoFitColumns && worksheet.Dimension != null)
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            ApplyColumnWidths(worksheet, tableDefinition);
        }
        private static void ApplyColumnWidths(ExcelWorksheet worksheet, TableDefinition table)
        {
            for (int columnIndex = 0; columnIndex < table.Columns.Count; columnIndex++)
            {
                var column = table.Columns[columnIndex];

                // Если ширина задана явно (не NaN и больше 0), применяем её
                if (!double.IsNaN(column.Width) && column.Width > 0)
                {
                    worksheet.Column(columnIndex + 1).Width = column.Width;
                }
            }
        }

        #region Table

        private static void ApplyTableSettings(ExcelWorksheet worksheet, TableDefinition table)
            => worksheet.View.ShowGridLines = table.ShowGridLines;

        private static void WriteRows(ExcelWorksheet worksheet, TableDefinition table)
        {
            for (int rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
            {
                var row = table.Rows[rowIndex];
                int excelRow = rowIndex + 2;

                for (int columnIndex = 0; columnIndex < table.Columns.Count; columnIndex++)
                {
                    var column = table.Columns[columnIndex];
                    row.Values.TryGetValue(column.Key, out var value);
                    var cell = worksheet.Cells[excelRow, columnIndex + 1];
                    cell.Value = value;

                    ApplyColumnStyle(cell, column);
                }

                ApplyRowStyle(worksheet, excelRow, table, row);
            }
        }

        private static void ApplyTableStyle(ExcelWorksheet worksheet, TableDefinition table)
        {
            if (worksheet.Dimension == null)
                return;

            var range = worksheet.Cells[worksheet.Dimension.Address];

            ApplyBorder(range, table.Style.Border);
        }

        #endregion

        #region Header

        private static void WriteHeader(ExcelWorksheet worksheet, TableDefinition table)
        {
            for (int columnIndex = 0; columnIndex < table.Columns.Count; columnIndex++)
            {
                var column = table.Columns[columnIndex];

                var cell = worksheet.Cells[1, columnIndex + 1];

                cell.Value = column.Header;
            }
        }

        private static void ApplyHeaderMergeRules(ExcelWorksheet worksheet, TableDefinition table)
        {
            foreach (var rule in table.HeaderMergeRules)
            {
                int startColumn = table.Columns.FindIndex(
                    x => x.Key == rule.ColumnKey) + 1;

                int endColumn = table.Columns.FindIndex(
                    x => x.Key == rule.EndColumnKey) + 1;

                if (startColumn <= 0 || endColumn <= 0)
                    continue;

                var range = worksheet.Cells[1, startColumn, 1, endColumn];
                range.Merge = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }
        }

        private static void ApplyHeaderStyle(ExcelWorksheet worksheet, TableDefinition table)
        {
            var style = table.HeaderStyle;

            var range = worksheet.Cells[1, 1, 1, table.Columns.Count];

            range.Style.Font.Name = style.FontName;
            range.Style.Font.Size = (float)style.FontSize;
            range.Style.Font.Bold = style.Bold;
            range.Style.HorizontalAlignment = ConvertHorizontalAlignment(style.HorizontalAlignment);
            range.Style.VerticalAlignment = ConvertVerticalAlignment(style.VerticalAlignment);
            range.Style.WrapText = style.WrapText;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(style.BackgroundColor);

            ApplyBorder(range, style.Border);

            if (style.Height > 0)
                worksheet.Row(1).Height = style.Height;
        }

        #endregion

        #region Column style

        private static void ApplyColumnStyle(ExcelRange cell, TableColumn column)
        {
            cell.Style.Font.Name = column.FontName;

            cell.Style.Font.Size = (float)column.FontSize;

            cell.Style.Font.Bold = column.Bold;

            cell.Style.HorizontalAlignment = ConvertHorizontalAlignment(column.HorizontalAlignment);

            cell.Style.VerticalAlignment = ConvertVerticalAlignment(column.VerticalAlignment);

            cell.Style.WrapText = column.WrapText;

            var format = ConvertToExcelFormat(column.Format);

            if (!string.IsNullOrWhiteSpace(format))
                cell.Style.Numberformat.Format = format;
        }

        #endregion

        #region Row style

        private static void ApplyRowStyle(ExcelWorksheet worksheet, int rowIndex, TableDefinition table, TableRow row)
        {
            if (row.Height.HasValue)
                worksheet.Row(rowIndex).Height = row.Height.Value;

            ApplyRowStyle(worksheet, rowIndex, table, row, row.Style);

            foreach (var rule in table.RowStyleRules)
            {
                if (!rule.Condition(row))
                    continue;

                var style = new TableRowStyle
                {
                    Bold = rule.Bold,
                    BackgroundColor = rule.BackgroundColor,
                    Border = rule.Border,
                    HorizontalAlignment = rule.HorizontalAlignment,
                    VerticalAlignment = rule.VerticalAlignment,
                    WrapText = rule.WrapText
                };

                ExcelRange range;

                if (rule.ColumnKey != null)
                {
                    int columnIndex = table.Columns.FindIndex(x => x.Key == rule.ColumnKey);

                    if (columnIndex < 0)
                        continue;

                    range = worksheet.Cells[rowIndex, columnIndex + 1];
                }
                else
                {
                    range = worksheet.Cells[rowIndex, 1, rowIndex, table.Columns.Count];
                }

                ApplyRowStyle(range, style);
            }
        }

        private static void ApplyRowStyle(ExcelWorksheet worksheet, int rowIndex, TableDefinition table, TableRow row, TableRowStyle? style)
        {
            if (style == null)
                return;

            var range = worksheet.Cells[rowIndex, 1, rowIndex, table.Columns.Count];

            ApplyRowStyle(range, style);

            if (style.Height.HasValue)
                worksheet.Row(rowIndex).Height = style.Height.Value;
        }

        private static void ApplyRowStyle(ExcelRange range, TableRowStyle style)
        {
            if (style.Bold)
                range.Style.Font.Bold = true;

            if (style.WrapText.HasValue)
                range.Style.WrapText = style.WrapText.Value;

            if (style.BackgroundColor.HasValue)
            {
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(style.BackgroundColor.Value);
            }

            if (style.HorizontalAlignment.HasValue)
                range.Style.HorizontalAlignment = ConvertHorizontalAlignment(style.HorizontalAlignment.Value);

            if (style.VerticalAlignment.HasValue)
                range.Style.VerticalAlignment = ConvertVerticalAlignment(style.VerticalAlignment.Value);

            ApplyBorder(range, style.Border);
        }

        private static void ApplyBorder(ExcelRange range, TableBorderStyle? border)
        {
            if (border == null)
                return;

            if (border.Top)
            {
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;

                range.Style.Border.Top.Color.SetColor(border.Color);
            }

            if (border.Bottom)
            {
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                range.Style.Border.Bottom.Color.SetColor(border.Color);
            }

            if (border.Left)
            {
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;

                range.Style.Border.Left.Color.SetColor(border.Color);
            }

            if (border.Right)
            {
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;

                range.Style.Border.Right.Color.SetColor(border.Color);
            }
        }

        #endregion

        #region Merge

        private static void ApplyMergeRules(ExcelWorksheet worksheet, TableDefinition table)
        {
            foreach (var rule in table.MergeRules)
            {
                if (rule.Vertical)
                {
                    MergeRepeatedCells(
                        worksheet,
                        table,
                        rule);
                }

                if (rule.Horizontal)
                {
                    MergeHorizontalCells(
                        worksheet,
                        table,
                        rule);
                }
            }
        }

        private static void MergeHorizontalCells(ExcelWorksheet worksheet, TableDefinition table, TableMergeRule rule)
        {
            int startColumn =
                rule.ColumnKey != null
                    ? table.Columns.FindIndex(x => x.Key == rule.ColumnKey) + 1
                    : 1;

            int endColumn =
                rule.EndColumnKey != null
                    ? table.Columns.FindIndex(x => x.Key == rule.EndColumnKey) + 1
                    : table.Columns.Count;

            if (startColumn <= 0 || endColumn <= 0 || startColumn >= endColumn)
                return;

            for (int rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
            {
                var row = table.Rows[rowIndex];

                if (rule.CanMerge != null && !rule.CanMerge(row))
                    continue;

                int excelRow = rowIndex + 2;

                var range = worksheet.Cells[
                    excelRow,
                    startColumn,
                    excelRow,
                    endColumn];

                range.Merge = true;

                range.Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center;

                range.Style.VerticalAlignment =
                    ExcelVerticalAlignment.Center;

                if (rule.HorizontalAlignment.HasValue)
                {
                    range.Style.HorizontalAlignment = ConvertHorizontalAlignment(rule.HorizontalAlignment.Value);
                }
                else
                {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                if (rule.VerticalAlignment.HasValue)
                {
                    range.Style.VerticalAlignment = ConvertVerticalAlignment(rule.VerticalAlignment.Value);
                }
                else
                {
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                }
            }
        }

        private static void MergeRepeatedCells(
            ExcelWorksheet worksheet,
            TableDefinition table,
            TableMergeRule rule)
        {
            if (table.Rows.Count == 0)
                return;

            int columnIndex =
                table.Columns.FindIndex(
                    x => x.Key == rule.ColumnKey) + 1;

            if (columnIndex <= 0)
                return;

            int startRow = -1;

            object? previousValue = null;

            for (int i = 0;
                 i < table.Rows.Count;
                 i++)
            {
                var row = table.Rows[i];

                int excelRow = i + 2;

                row.Values.TryGetValue(
                    rule.ColumnKey,
                    out var currentValue);

                bool canMerge =
                    rule.CanMerge?.Invoke(row) ?? true;

                bool isEmpty =
                    currentValue == null ||
                    string.IsNullOrWhiteSpace(
                        currentValue.ToString());

                if (!canMerge ||
                    (rule.SkipEmpty && isEmpty))
                {
                    FinishMerge(
                        worksheet,
                        startRow,
                        excelRow - 1,
                        columnIndex);

                    startRow = -1;
                    previousValue = null;

                    continue;
                }

                if (startRow == -1)
                {
                    startRow = excelRow;
                    previousValue = currentValue;
                    continue;
                }

                if (!Equals(
                        previousValue,
                        currentValue))
                {
                    FinishMerge(
                        worksheet,
                        startRow,
                        excelRow - 1,
                        columnIndex);

                    startRow = excelRow;
                    previousValue = currentValue;
                }
            }

            FinishMerge(
                worksheet,
                startRow,
                table.Rows.Count + 1,
                columnIndex);
        }

        private static void FinishMerge(
            ExcelWorksheet worksheet,
            int startRow,
            int endRow,
            int column)
        {
            if (startRow < 0 ||
                startRow >= endRow)
            {
                return;
            }

            var range = worksheet.Cells[
                startRow,
                column,
                endRow,
                column];

            range.Merge = true;

            range.Style.VerticalAlignment =
                ExcelVerticalAlignment.Center;
        }

        #endregion

        #region Conversion

        private static string? ConvertToExcelFormat(string? format)
        {
            if (string.IsNullOrWhiteSpace(format))
                return null;

            if (format.StartsWith(
                    "F",
                    StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(
                    format[1..],
                    out var digits))
            {
                return "0." +
                       new string('0', digits);
            }

            return format;
        }

        private static ExcelHorizontalAlignment ConvertHorizontalAlignment(TableHorizontalAlignment alignment)
        {
            return alignment switch
            {
                TableHorizontalAlignment.Left =>
                    ExcelHorizontalAlignment.Left,

                TableHorizontalAlignment.Right =>
                    ExcelHorizontalAlignment.Right,

                _ =>
                    ExcelHorizontalAlignment.Center
            };
        }

        private static ExcelVerticalAlignment ConvertVerticalAlignment(TableVerticalAlignment alignment)
        {
            return alignment switch
            {
                TableVerticalAlignment.Top =>
                    ExcelVerticalAlignment.Top,

                TableVerticalAlignment.Bottom =>
                    ExcelVerticalAlignment.Bottom,

                _ =>
                    ExcelVerticalAlignment.Center
            };
        }

        #endregion

        private static string GetExcelColumnName(int column)
        {
            var result = string.Empty;

            while (column > 0)
            {
                column--;

                result = (char)('A' + column % 26) + result;

                column /= 26;
            }

            return result;
        }
    }
}
