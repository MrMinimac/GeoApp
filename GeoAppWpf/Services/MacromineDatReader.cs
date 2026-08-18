using GeoAppWpf.Models;
using LegendDesignWpf.Core.WinApi;
using netDxf;
using netDxf.Entities;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace GeoAppWpf.Services
{
    /// <summary>
    /// Универсальная запись из табличного DAT.
    /// </summary>
    public class MacromineRecord
    {
        public Dictionary<string, string> Values { get; } = new();

        public string Get(string columnName)
        {
            return Values.TryGetValue(columnName, out var value)
                ? value
                : string.Empty;
        }

        public double GetDouble(string columnName)
        {
            var value = Get(columnName);

            return double.TryParse(
                value,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var result)
                    ? result
                    : 0.0;
        }
    }


    /// <summary>
    /// Результат чтения DAT.
    /// </summary>
    public class MacromineDatResult
    {
        public MacromineDatType Type { get; set; }

        /// <summary>
        /// Записи табличного DAT.
        /// Заполняется только для Table.
        /// </summary>
        public List<MacromineRecord> Records { get; set; } = new();

        public List<MacromineSample> Samples { get; set; } = new();

        public Dictionary<EntityObject, MacromineSample> SampleData { get; set; } = new();

        /// <summary>
        /// DXF с полилиниями.
        /// Заполняется только для Strings.
        /// </summary>
        public DxfDocument? Dxf { get; set; }

    }


    public enum MacromineDatType
    {
        Unknown,
        Strings,
        Table
    }


    public class MacromineDatReader
    {
        // ============================================================
        // ОСНОВНОЙ МЕТОД
        // ============================================================

        public MacromineDatResult Read(string filePath)
        {
            Encoding.RegisterProvider(
        CodePagesEncodingProvider.Instance);

            var lines = File.ReadAllLines(
    filePath,
    Encoding.GetEncoding(1251));

            var (contentStartIndex, columns) =
                ParseHeader(lines);

            var type = DetectType(columns);

            switch (type)
            {
                case MacromineDatType.Strings:
                    {
                        var parsedLines = ParseLines(
                            lines,
                            contentStartIndex,
                            columns);

                        var sampleData =
                            new Dictionary<EntityObject, MacromineSample>();

                        var dxf = BuildDxf(
                            parsedLines,
                            null,
                            sampleData);

                        return new MacromineDatResult
                        {
                            Type = MacromineDatType.Strings,
                            Dxf = dxf,
                            SampleData = sampleData
                        };
                    }

                case MacromineDatType.Table:
                    {
                        var records = ReadTable(
                            lines,
                            contentStartIndex,
                            columns);

                        var input = InputDialog.Show("Минимальное содержание:", "", "0.15");

                        double minAu = 0.0;

                        if (!string.IsNullOrWhiteSpace(input))
                        {
                            input = input.Replace(',', '.');

                            double.TryParse(
                                input,
                                NumberStyles.Any,
                                CultureInfo.InvariantCulture,
                                out minAu);
                        }

                        if (double.TryParse(input, out double result))
                        {
                            minAu = result;

                            Debug.WriteLine($"Мин Au найден: {result}");
                        }

                        var samples = records
                            .Select((x) => CreateSample(x, minAu))
                            .ToList();

                        var sampleData =
                            new Dictionary<EntityObject, MacromineSample>();

                        var dxf = BuildDxf(
                            null,
                            samples,
                            sampleData);

                        foreach (var sample in samples)
                        {
                            Debug.WriteLine($"SampleCode: {sample.Code} | SampleAu: {sample.Au}");
                        }

                        return new MacromineDatResult
                        {
                            Type = MacromineDatType.Table,
                            Records = records,
                            Samples = samples,
                            SampleData = sampleData,
                            Dxf = dxf
                        };
                    }

                default:
                    throw new InvalidDataException(
                        "Неизвестный тип DAT файла.");
            }
        }


        // ============================================================
        // ОПРЕДЕЛЕНИЕ ТИПА DAT
        // ============================================================

        private MacromineDatType DetectType(
            List<(string Name, int Width)> columns)
        {
            bool hasEast = HasColumn(columns, "EAST");
            bool hasNorth = HasColumn(columns, "NORTH");
            bool hasRl = HasColumn(columns, "RL");

            bool hasString = HasColumn(columns, "STRING");
            bool hasJoin = HasColumn(columns, "JOIN");

            // DAT со строками / контурами
            if (hasEast &&
                hasNorth &&
                hasRl &&
                (hasString || hasJoin))
            {
                return MacromineDatType.Strings;
            }

            // Всё остальное пока считаем таблицей
            return MacromineDatType.Table;
        }


        private bool HasColumn(
            List<(string Name, int Width)> columns,
            string name)
        {
            return columns.Any(c =>
                c.Name.Equals(
                    name,
                    StringComparison.OrdinalIgnoreCase));
        }


        // ============================================================
        // ЧТЕНИЕ ТАБЛИЧНОГО DAT
        // ============================================================

        private List<MacromineRecord> ReadTable(
            string[] lines,
            int startIndex,
            List<(string Name, int Width)> columns)
        {
            var records = new List<MacromineRecord>();

            for (int i = startIndex; i < lines.Length; i++)
            {
                var line = lines[i];

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var record = new MacromineRecord();

                int currentOffset = 0;

                foreach (var column in columns)
                {
                    string rawValue = string.Empty;

                    if (currentOffset < line.Length)
                    {
                        int length = Math.Min(
                            column.Width,
                            line.Length - currentOffset);

                        rawValue = line
                            .Substring(currentOffset, length)
                            .Trim();
                    }

                    record.Values[column.Name] = rawValue;

                    currentOffset += column.Width;
                }

                records.Add(record);
            }

            return records;
        }


        // ============================================================
        // ЧТЕНИЕ КОНТУРОВ
        // ============================================================

        private List<List<MacrominePoint>> ParseLines(
            string[] lines,
            int startIndex,
            List<(string Name, int Width)> columns)
        {
            var resultLines =
                new List<List<MacrominePoint>>();

            var currentLine =
                new List<MacrominePoint>();

            string currentJoinId = string.Empty;


            // Индексы колонок
            int eastIdx = columns.FindIndex(c =>
                c.Name.Equals(
                    "EAST",
                    StringComparison.OrdinalIgnoreCase));

            int northIdx = columns.FindIndex(c =>
                c.Name.Equals(
                    "NORTH",
                    StringComparison.OrdinalIgnoreCase));

            int rlIdx = columns.FindIndex(c =>
                c.Name.Equals(
                    "RL",
                    StringComparison.OrdinalIgnoreCase));

            int stringIdx = columns.FindIndex(c =>
                c.Name.Equals(
                    "STRING",
                    StringComparison.OrdinalIgnoreCase));

            int joinIdx = columns.FindIndex(c =>
                c.Name.Equals(
                    "JOIN",
                    StringComparison.OrdinalIgnoreCase));


            for (int i = startIndex; i < lines.Length; i++)
            {
                var line = lines[i];


                // ----------------------------------------------------
                // Пустая строка = разрыв
                // ----------------------------------------------------

                if (string.IsNullOrWhiteSpace(line))
                {
                    if (currentLine.Count > 0)
                    {
                        resultLines.Add(currentLine);

                        currentLine =
                            new List<MacrominePoint>();
                    }

                    continue;
                }


                var point = new MacrominePoint();

                int currentOffset = 0;


                // ----------------------------------------------------
                // Разбираем колонки
                // ----------------------------------------------------

                for (int colIdx = 0;
                     colIdx < columns.Count;
                     colIdx++)
                {
                    var col = columns[colIdx];

                    if (currentOffset >= line.Length)
                        break;

                    int lengthToRead = Math.Min(
                        col.Width,
                        line.Length - currentOffset);

                    string rawValue =
                        line.Substring(
                            currentOffset,
                            lengthToRead)
                        .Trim();


                    if (colIdx == eastIdx)
                    {
                        point.X = ParseDouble(rawValue);
                    }
                    else if (colIdx == northIdx)
                    {
                        point.Y = ParseDouble(rawValue);
                    }
                    else if (colIdx == rlIdx)
                    {
                        point.Z = ParseDouble(rawValue);
                    }
                    else if (colIdx == stringIdx)
                    {
                        point.StringCode = rawValue;
                    }
                    else if (colIdx == joinIdx)
                    {
                        point.JoinId = rawValue;
                    }


                    currentOffset += col.Width;
                }


                // ----------------------------------------------------
                // Обработка ~
                // ----------------------------------------------------

                bool isForceBreak = false;


                if (!string.IsNullOrEmpty(point.JoinId) &&
                    point.JoinId.EndsWith("~"))
                {
                    isForceBreak = true;

                    point.JoinId =
                        point.JoinId.TrimEnd('~');
                }
                else if (!string.IsNullOrEmpty(point.StringCode) &&
                         point.StringCode.EndsWith("~"))
                {
                    isForceBreak = true;

                    point.StringCode =
                        point.StringCode.TrimEnd('~');
                }


                // ----------------------------------------------------
                // Смена JOIN = новая линия
                // ----------------------------------------------------

                if (currentLine.Count > 0 &&
                    (isForceBreak ||
                     point.JoinId != currentJoinId))
                {
                    resultLines.Add(currentLine);

                    currentLine =
                        new List<MacrominePoint>();
                }


                currentLine.Add(point);

                currentJoinId = point.JoinId;
            }


            // --------------------------------------------------------
            // Последняя линия
            // --------------------------------------------------------

            if (currentLine.Count > 0)
            {
                resultLines.Add(currentLine);
            }


            return resultLines;
        }


        // ============================================================
        // СОЗДАНИЕ DXF ИЗ КОНТУРОВ
        // ============================================================

        private DxfDocument BuildDxf(
    List<List<MacrominePoint>>? parsedLines,
    List<MacromineSample>? samples,
    Dictionary<EntityObject, MacromineSample> sampleData)
        {
            var dxf = new DxfDocument();
            var color = ColorHelper.GetRandomColor();


            // ============================================================
            // STRINGS / КОНТУРЫ
            // ============================================================

            if (parsedLines != null)
            {
                foreach (var lineGroup in parsedLines)
                {
                    if (lineGroup.Count < 2)
                        continue;


                    var vertices = lineGroup
                        .Select(p =>
                            new Vector3(
                                p.X,
                                p.Y,
                                p.Z))
                        .ToList();


                    var polyline =
                        new Polyline3D(vertices);


                    polyline.Color =
                        new AciColor(
                            color.R,
                            color.G,
                            color.B);


                    // ----------------------------------------------------
                    // Слой по JOIN
                    // ----------------------------------------------------

                    string layerJoinId =
                        lineGroup[0].JoinId;


                    string layerName =
                        string.IsNullOrEmpty(layerJoinId)
                            ? "Default_Line"
                            : $"Line_{layerJoinId}";


                    if (!dxf.Layers.Contains(layerName))
                    {
                        dxf.Layers.Add(
                            new netDxf.Tables.Layer(
                                layerName));
                    }


                    polyline.Layer =
                        dxf.Layers[layerName];


                    dxf.Entities.Add(polyline);
                }
            }


            // ============================================================
            // SAMPLES / ПРОБЫ
            // ============================================================

            if (samples != null)
            {
                const string layerName = "Samples";

                if (!dxf.Layers.Contains(layerName))
                {
                    dxf.Layers.Add(
                        new netDxf.Tables.Layer(layerName));
                }

                var sampleLayer = dxf.Layers[layerName];

                foreach (var sample in samples)
                {
                    // Создаём прямоугольник пробы
                    var rectangle = CreateSampleLine(sample);

                    rectangle.Layer = sampleLayer;

                    rectangle.Color =
                        new AciColor(
                            255,
                            255,
                            0);

                    // Добавляем прямоугольник в DXF
                    dxf.Entities.Add(rectangle);

                    // Связываем DXF Entity с данными пробы
                    sampleData[rectangle] = sample;
                }
            }

            return dxf;
        }

        private Polyline3D CreateSampleLine(MacromineSample sample)
        {
            double halfLength = sample.Length / 2;

            double zTop = sample.Z + halfLength;
            double zBottom = sample.Z - halfLength;

            var vertices = new List<Vector3>
            {
                new Vector3(
                    sample.X,
                    sample.Y,
                    zTop),

                new Vector3(
                    sample.X,
                    sample.Y,
                    zBottom)
            };

            var line = new Polyline3D(vertices);

            line.Color = new AciColor(
                255,
                255,
                0);

            return line;
        }

        // ============================================================
        // HEADER
        // ============================================================

        private (
            int contentStartIndex,
            List<(string Name, int Width)> columns)
            ParseHeader(string[] lines)
        {
            int variablesCount = -1;
            int variablesIndex = -1;


            // --------------------------------------------------------
            // Ищем VARIABLES
            // --------------------------------------------------------

            for (int i = 0;
                 i < lines.Length;
                 i++)
            {
                var l = lines[i].Trim();


                if (l.IndexOf(
                        "VARIABLES",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var parts =
                        l.Split(
                            new[] { ' ', '\t' },
                            StringSplitOptions.RemoveEmptyEntries);


                    if (parts.Length > 0 &&
                        int.TryParse(
                            parts[0],
                            out int count))
                    {
                        variablesCount = count;

                        variablesIndex = i + 1;

                        break;
                    }
                }
            }


            if (variablesIndex == -1 ||
                variablesCount == -1)
            {
                throw new InvalidDataException(
                    "Не найден блок VARIABLES в заголовке DAT файла.");
            }


            // --------------------------------------------------------
            // Читаем описание колонок
            // --------------------------------------------------------

            var columns =
                new List<(string Name, int Width)>();

            for (int i = variablesIndex;
                 i < variablesIndex + variablesCount;
                 i++)
            {
                if (i >= lines.Length)
                {
                    throw new InvalidDataException(
                        "Количество VARIABLES не соответствует содержимому заголовка.");
                }

                var parts =
                    lines[i].Split(
                        new[] { ' ', '\t' },
                        StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 3)
                {
                    throw new InvalidDataException(
                        $"Некорректное описание переменной: {lines[i]}");
                }

                string name;
                int width;

                // ========================================================
                // Нормальный формат:
                //
                // Au_g/t N 20 9
                // Ограничени N 20 9
                //
                // parts:
                // [0] name
                // [1] type
                // [2] width
                // [3] decimals
                // ========================================================

                if (parts.Length >= 4 &&
                    (parts[1].Equals("C", StringComparison.OrdinalIgnoreCase) ||
                     parts[1].Equals("N", StringComparison.OrdinalIgnoreCase)))
                {
                    name = parts[0];

                    if (!int.TryParse(parts[2], out width))
                    {
                        throw new InvalidDataException(
                            $"Не удалось определить ширину колонки: {lines[i]}");
                    }
                }

                // ========================================================
                // Macromine иногда пишет:
                //
                // ОграничениN 20 9
                //
                // То есть тип N "прилип" к имени.
                // ========================================================

                else if (parts.Length >= 3 &&
                         (parts[0].EndsWith("N", StringComparison.OrdinalIgnoreCase) ||
                          parts[0].EndsWith("C", StringComparison.OrdinalIgnoreCase)))
                {
                    name = parts[0][..^1];

                    if (!int.TryParse(parts[1], out width))
                    {
                        throw new InvalidDataException(
                            $"Не удалось определить ширину колонки: {lines[i]}");
                    }
                }

                else
                {
                    throw new InvalidDataException(
                        $"Неизвестный формат переменной: {lines[i]}");
                }

                columns.Add((name, width));
            }


            int contentStartIndex =
                variablesIndex + variablesCount;


            return (
                contentStartIndex,
                columns);
        }

        private MacromineSample CreateSample(MacromineRecord record, double minAu)
        {
            return new MacromineSample
            {
                HoleId = record.Get("HoleID"),

                From = record.GetDouble("From"),
                To = record.GetDouble("To"),
                Length = record.GetDouble("Length"),

                SampleId = record.Get("SampleID"),

                Au = record.GetDouble("Au_g/t"),
                Restriction = record.GetDouble("Ограничени"),

                Ovp = record.Get("ОВП"),
                Code = record.Get("код"),

                MinAu = minAu,

                X = record.GetDouble("X"),
                Y = record.GetDouble("Y"),
                Z = record.GetDouble("Z")
            };
        }

        // ============================================================
        // DOUBLE
        // ============================================================

        private double ParseDouble(string value)
        {
            if (double.TryParse(
                    value,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double result))
            {
                return result;
            }


            return 0.0;
        }
    }
}