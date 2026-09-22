using GeoAppWpf.Services.Excel.Build;
using GeoAppWpf.Services.Excel.Render;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.IO;

namespace GeoAppWpf.Services.Excel
{
    public class ExcelWorksheetBuilder
    {
        private record PendingHatch(int Row, int Col, object Config);

        public static void Build(ExcelWorksheet worksheet, TableDefinition tableDefinition)
        {
            ApplyTableSettings(worksheet, tableDefinition);

            if (tableDefinition.HasHeader)
            {
                WriteHeader(worksheet, tableDefinition);
                ApplyHeaderMergeRules(worksheet, tableDefinition);
                ApplyHeaderStyle(worksheet, tableDefinition);
            }

            var pendingHatches = new List<PendingHatch>();

            WriteRows(
                worksheet,
                tableDefinition,
                pendingHatches);

            ApplyMergeRules(worksheet, tableDefinition);

            ApplyTableStyle(worksheet, tableDefinition);

            if (tableDefinition.AutoFitColumns && worksheet.Dimension != null)
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            ApplyColumnWidths(worksheet, tableDefinition);

            InsertPendingHatches(worksheet, pendingHatches);
        }

        private static void ApplyColumnWidths(ExcelWorksheet worksheet, TableDefinition table)
        {
            for (int columnIndex = 0; columnIndex < table.Columns.Count; columnIndex++)
            {
                var column = table.Columns[columnIndex];

                if (!double.IsNaN(column.Width) && column.Width > 0)
                {
                    worksheet.Column(columnIndex + 1).Width = column.Width;
                }
            }
        }

        #region Table

        private static void ApplyTableSettings(ExcelWorksheet worksheet, TableDefinition table)
            => worksheet.View.ShowGridLines = table.ShowGridLines;

        private static void WriteRows(
            ExcelWorksheet worksheet,
            TableDefinition table,
            List<PendingHatch> pendingHatches)
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

                    if (value is HatchConfig hatchConfig)
                    {
                        cell.Value = null;

                        pendingHatches.Add(
                            new PendingHatch(
                                excelRow,
                                columnIndex + 1,
                                hatchConfig));
                    }
                    else if (value is GeoColumnHatchConfig columnHatch)
                    {
                        cell.Value = null;
                        pendingHatches.Add(new PendingHatch(excelRow, columnIndex + 1, columnHatch));
                    }
                    else
                    {
                        cell.Value = value;
                    }

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
                int startColumn = table.Columns.FindIndex(x => x.Key == rule.ColumnKey) + 1;
                int endColumn = table.Columns.FindIndex(x => x.Key == rule.EndColumnKey) + 1;

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

        #region Row / Cell style

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
            if (style == null)
                return;

            if (style.Bold)
                range.Style.Font.Bold = true;

            if (style.WrapText == true)
            {
                range.Style.WrapText = true;
                range.Style.TextRotation = 0;
            }
            else if (style.WrapText == false)
            {
                range.Style.WrapText = false;
            }

            if (style.PatternType.HasValue && style.PatternType != ExcelFillStyle.None)
            {
                range.Style.Fill.PatternType = style.PatternType.Value;

                if (style.PatternColor.HasValue)
                    range.Style.Fill.PatternColor.SetColor(style.PatternColor.Value);

                if (style.BackgroundColor.HasValue)
                    range.Style.Fill.BackgroundColor.SetColor(style.BackgroundColor.Value);
            }
            else if (style.BackgroundColor.HasValue)
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

        #region Images

        private static void InsertPendingHatches(
    ExcelWorksheet worksheet,
    List<PendingHatch> hatches)
        {
            var processedRanges = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var hatch in hatches)
            {
                var mergedAddress =
                    worksheet.MergedCells[hatch.Row, hatch.Col];

                if (!string.IsNullOrEmpty(mergedAddress))
                {
                    if (!processedRanges.Add(mergedAddress))
                        continue;

                    var range = worksheet.Cells[mergedAddress];

                    InsertCellHatch(
                        worksheet,
                        range.Start.Row,
                        range.Start.Column,
                        hatch.Config
                        );
                }
                else
                {
                    string key = $"{hatch.Row}:{hatch.Col}";

                    if (!processedRanges.Add(key))
                        continue;

                    InsertCellHatch(
                        worksheet,
                        hatch.Row,
                        hatch.Col,
                        hatch.Config
                        );
                }
            }
        }

        private static void InsertCellHatch(ExcelWorksheet worksheet, int row, int col, object config)
        {
            var (boxWidthPx, boxHeightPx, anchorRow, anchorCol) =
                GetCellPixelBox(
                    worksheet,
                    row,
                    col);

            int width = Math.Max(
                1,
                (int)Math.Ceiling(boxWidthPx));

            int height = Math.Max(
                1,
                (int)Math.Ceiling(boxHeightPx));

            byte[] imageBytes = config switch
            {
                HatchConfig hc => HatchImageRenderer.Render(width, height, hc.Elements, hc.Color),
                GeoColumnHatchConfig gc => HatchImageRenderer.RenderColumn(width, height, gc),
                _ => throw new NotSupportedException()
            };

            string tempFile = Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid():N}.png");

            File.WriteAllBytes(
                tempFile,
                imageBytes);

            string name =
                $"hatch_{row}_{col}_{Guid.NewGuid():N}";

            var picture =
                worksheet.Drawings.AddPicture(
                    name,
                    tempFile);

            picture.SetSize(
                width,
                height);

            picture.SetPosition(
                anchorRow - 1,
                0,
                anchorCol - 1,
                0);
        }

        private static (double Width, double Height, int AnchorRow, int AnchorCol) GetCellPixelBox(ExcelWorksheet ws, int row, int col)
        {
            int rowStart = row, rowEnd = row, colStart = col, colEnd = col;

            var mergedAddress = ws.MergedCells[row, col];
            if (!string.IsNullOrEmpty(mergedAddress))
            {
                var range = ws.Cells[mergedAddress];
                rowStart = range.Start.Row;
                rowEnd = range.End.Row;
                colStart = range.Start.Column;
                colEnd = range.End.Column;
            }

            double width = 0;
            for (int c = colStart; c <= colEnd; c++)
                width += GetColumnWidthPixels(ws, c);

            double height = 0;
            for (int r = rowStart; r <= rowEnd; r++)
                height += GetRowHeightPixels(ws, r);

            return (width, height, rowStart, colStart);
        }

        private static double GetColumnWidthPixels(ExcelWorksheet ws, int col)
        {
            double width = ws.Column(col).Width;

            if (width <= 0)
                width = ws.DefaultColWidth;

            return Math.Round(width * 8.0);
        }

        private static double GetRowHeightPixels(ExcelWorksheet ws, int row)
        {
            double heightPoints = ws.Row(row).Height;
            if (heightPoints <= 0) heightPoints = ws.DefaultRowHeight;

            return heightPoints * 96.0 / 72.0;
        }

        #endregion

        #region Merge

        private static void ApplyMergeRules(ExcelWorksheet worksheet, TableDefinition table)
        {
            foreach (var rule in table.MergeRules)
            {
                if (rule.Vertical)
                    MergeRepeatedCells(worksheet, table, rule);

                if (rule.Horizontal)
                    MergeHorizontalCells(worksheet, table, rule);
            }
        }

        private static void MergeHorizontalCells(ExcelWorksheet worksheet, TableDefinition table, TableMergeRule rule)
        {
            int startColumn = rule.ColumnKey != null
                ? table.Columns.FindIndex(x => x.Key == rule.ColumnKey) + 1
                : 1;

            int endColumn = rule.EndColumnKey != null
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

                var range = worksheet.Cells[excelRow, startColumn, excelRow, endColumn];
                range.Merge = true;

                range.Style.HorizontalAlignment = rule.HorizontalAlignment.HasValue
                    ? ConvertHorizontalAlignment(rule.HorizontalAlignment.Value)
                    : ExcelHorizontalAlignment.Center;

                range.Style.VerticalAlignment = rule.VerticalAlignment.HasValue
                    ? ConvertVerticalAlignment(rule.VerticalAlignment.Value)
                    : ExcelVerticalAlignment.Center;
            }
        }

        private static void MergeRepeatedCells(ExcelWorksheet worksheet, TableDefinition table, TableMergeRule rule)
        {
            if (table.Rows.Count == 0)
                return;

            int columnIndex = table.Columns.FindIndex(x => x.Key == rule.ColumnKey) + 1;

            if (columnIndex <= 0)
                return;

            int startRow = -1;
            object? previousValue = null;

            for (int i = 0; i < table.Rows.Count; i++)
            {
                var row = table.Rows[i];
                int excelRow = i + 2;

                row.Values.TryGetValue(rule.ColumnKey, out var currentValue);

                bool canMerge = rule.CanMerge?.Invoke(row) ?? true;

                bool isEmpty = currentValue == null || string.IsNullOrWhiteSpace(currentValue.ToString());

                if (!canMerge || (rule.SkipEmpty && isEmpty))
                {
                    FinishMerge(worksheet, startRow, excelRow - 1, columnIndex, rule);
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

                if (!Equals(previousValue, currentValue))
                {
                    FinishMerge(worksheet, startRow, excelRow - 1, columnIndex, rule);
                    startRow = excelRow;
                    previousValue = currentValue;
                }
            }

            FinishMerge(worksheet, startRow, table.Rows.Count + 1, columnIndex, rule);
        }

        private static void FinishMerge(ExcelWorksheet worksheet, int startRow, int endRow, int column, TableMergeRule rule)
        {
            if (startRow < 0 || startRow >= endRow)
                return;

            var range = worksheet.Cells[startRow, column, endRow, column];
            range.Merge = true;

            range.Style.VerticalAlignment = rule.VerticalAlignment.HasValue
                ? ConvertVerticalAlignment(rule.VerticalAlignment.Value)
                : ExcelVerticalAlignment.Center;

            range.Style.HorizontalAlignment = rule.HorizontalAlignment.HasValue
                ? ConvertHorizontalAlignment(rule.HorizontalAlignment.Value)
                : ExcelHorizontalAlignment.Center;
        }

        #endregion

        #region Conversion

        private static string? ConvertToExcelFormat(string? format)
        {
            if (string.IsNullOrWhiteSpace(format))
                return null;

            if (format.StartsWith("F", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(format[1..], out var digits))
            {
                return "0." + new string('0', digits);
            }

            return format;
        }

        private static ExcelHorizontalAlignment ConvertHorizontalAlignment(TableHorizontalAlignment alignment)
        {
            return alignment switch
            {
                TableHorizontalAlignment.Left => ExcelHorizontalAlignment.Left,
                TableHorizontalAlignment.Right => ExcelHorizontalAlignment.Right,
                _ => ExcelHorizontalAlignment.Center
            };
        }

        private static ExcelVerticalAlignment ConvertVerticalAlignment(TableVerticalAlignment alignment)
        {
            return alignment switch
            {
                TableVerticalAlignment.Top => ExcelVerticalAlignment.Top,
                TableVerticalAlignment.Bottom => ExcelVerticalAlignment.Bottom,
                _ => ExcelVerticalAlignment.Center
            };
        }

        #endregion
    }
}