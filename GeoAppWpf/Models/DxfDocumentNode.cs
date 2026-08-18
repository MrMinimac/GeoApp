using GeoAppWpf.Enums;
using GeoAppWpf.Interfaces;
using GeoAppWpf.Services;
using Microsoft.Win32;
using netDxf;
using netDxf.Entities;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;

namespace GeoAppWpf.Models
{
    public class DxfDocumentNode : GeoTreeNode
    {
        public MacromineDatResult? DatResult { get; set; }
        public DxfDocument Document { get; }
        public ObservableCollection<DxfEntityView> EntityViews { get; } = new();

        public override IReadOnlyList<TreeMenuItem> MenuItems =>
        [
            new()
            {
                Header = "Добавить в 3D-просмотр",
                Command = CommandProvider.Open3DCommand
            },
            new()
            {
                Header = "Переименовать композиты",
                Command = CommandProvider.RenameCompositesCommand
            },
            new()
            {
                Header = "Сохранить как...",
                Items =
                [
                    new()
                    {
                        Header = "DXF",
                        Command = CommandProvider.SaveAsCommand,
                        CommandParameter = new SaveRequest(this, SupportExtensions.DXF)
                    },
                    new()
                    {
                        Header = "DAT",
                        Command = CommandProvider.SaveAsCommand,
                        CommandParameter = new SaveRequest(this, SupportExtensions.DAT)
                    }
                ]
            }
        ];

        public DxfDocumentNode(DxfDocument doc, ITreeCommandProvider commandProvider) : base(commandProvider)
        {
            Document = doc;
            Name = doc.Name;

            foreach (var entity in doc.Entities.All)
            {
                Children.Add(new EntitiesNode(entity, CommandProvider));
                EntityViews.Add(new DxfEntityView(entity));
            }
        }

        public DxfDocumentNode(DxfDocument doc, ITreeCommandProvider commandProvider, MacromineDatResult result) : base(commandProvider)
        {
            Document = doc;
            Name = doc.Name;

            foreach (var entity in doc.Entities.All)
            {
                Children.Add(new EntitiesNode(entity, CommandProvider));
                EntityViews.Add(new DxfEntityView(entity));
            }

            DatResult = result;
        }

        public void Save(SupportExtensions extension)
        {
            switch (extension)
            {
                case SupportExtensions.DXF:
                    SaveDXF();
                    break;
                case SupportExtensions.DAT:
                    SaveDAT();
                    break;
            }
        }

        private void SaveDAT()
        {

            switch (DatResult.Type)
            {
                case MacromineDatType.Strings:
                    SaveStrings();
                    break;
                case MacromineDatType.Table:
                    SaveTable();
                    break;
            }
           
        }

        private void SaveTable()
        {
            if (DatResult == null)
                return;

            var dialog = new SaveFileDialog
            {
                Filter = "DAT files (*.dat)|*.dat",
                DefaultExt = ".dat",
                FileName = Name
            };

            if (dialog.ShowDialog() != true)
                return;

            var filePath = dialog.FileName;

            var culture = CultureInfo.InvariantCulture;

            // Важно для старых Macromine DAT с кириллицей
            Encoding.RegisterProvider(
                CodePagesEncodingProvider.Instance);

            using var writer = new StreamWriter(
                filePath,
                false,
                Encoding.GetEncoding(1251));

            bool hasRestrictionColumn = 
                DatResult.Samples
                    .Where(x => x.Restriction != 0.0)
                    .Any();

            // ============================================================
            // HEADER
            // ============================================================

            writer.WriteLine(new string(' ', 40));

            int variableCount = hasRestrictionColumn ? 12 : 11;

            writer.WriteLine(
                $"{variableCount}   VARIABLES");

            writer.WriteLine("HoleID    C 20  0");
            writer.WriteLine("From      N 20  9");
            writer.WriteLine("To        N 20  9");
            writer.WriteLine("Length    N 20  9");
            writer.WriteLine("SampleID  C 20  0");
            writer.WriteLine("Au_g/t    N 20  9");

            if (hasRestrictionColumn)
            {
                writer.WriteLine("ОграничениN 20  9");
            }

            writer.WriteLine("ОВП       C 20  0");
            writer.WriteLine("код       C 20  0");
            writer.WriteLine("X         N 20  9");
            writer.WriteLine("Y         N 20  9");
            writer.WriteLine("Z         N 20  9");


            // ============================================================
            // DATA
            // ============================================================

            foreach (var sample in DatResult.Samples)
            {
                string holeId =
                    FormatString(sample.HoleId, 20);

                string from =
                    FormatNumber(sample.From, 20);

                string to =
                    FormatNumber(sample.To, 20);

                string length =
                    FormatNumber(sample.Length, 20);

                string sampleId =
                    FormatString(sample.SampleId, 20);

                string au =
                    FormatNumber(sample.Au, 20);

                string ovp =
                    FormatString(sample.Ovp, 20);

                string code =
                    FormatString(sample.Code, 20);

                string x =
                    FormatNumber(sample.X, 20);

                string y =
                    FormatNumber(sample.Y, 20);

                string z =
                    FormatNumber(sample.Z, 20);


                writer.Write(
                    holeId);
                writer.Write(
                    from);
                writer.Write(
                    to);
                writer.Write(
                    length);
                writer.Write(
                    sampleId);
                writer.Write(
                    au);


                // Только если колонка была в исходном DAT
                if (hasRestrictionColumn)
                {
                    string restriction =
                        FormatNumber(
                            sample.Restriction,
                            20);

                    writer.Write(
                        restriction);
                }


                writer.Write(
                    ovp);

                writer.Write(
                    code);

                writer.Write(
                    x);

                writer.Write(
                    y);

                writer.Write(
                    z);

                writer.WriteLine();
            }
        }

        private void SaveStrings()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "DAT files (*.dat)|*.dat",
                DefaultExt = ".dat",
                FileName = Name
            };

            if (dialog.ShowDialog() != true)
                return;

            var filePath = dialog.FileName;
            var culture = CultureInfo.InvariantCulture;

            using (var writer = new StreamWriter(filePath, false, Encoding.Default))
            {
                writer.WriteLine(new string(' ', 40));
                writer.WriteLine("5   VARIABLES");
                writer.WriteLine("EAST      N 12  3");
                writer.WriteLine("NORTH     N 12  3");
                writer.WriteLine("RL        N 12  3");
                writer.WriteLine("STRING    C 20  0");
                writer.WriteLine("JOIN      C 10  0");

                int stringId = 1;

                // Локальная функция для форматированной записи точки (избавляет от дублирования кода)
                void WritePoint(double x, double y, double z)
                {
                    string east = x.ToString("F3", culture);
                    string north = y.ToString("F3", culture);
                    string rl = z.ToString("F3", culture);

                    writer.WriteLine(
                        $"{east,-12}" +
                        $"{north,-12}" +
                        $"{rl,-12}" +
                        $"{string.Empty,-20}" +
                        $"{stringId,-10}"
                    );
                }

                foreach (var entity in Document.Entities.All)
                {
                    // 1. Сохранение обычных линий
                    if (entity is Polyline3D polyline)
                    {
                        foreach (var v in polyline.Vertexes)
                        {
                            WritePoint(v.X, v.Y, v.Z);
                        }
                        stringId++;
                    }
                    // 2. Сохранение каркаса (треугольников)
                    else if (entity is Face3D face)
                    {
                        WritePoint(face.FirstVertex.X, face.FirstVertex.Y, face.FirstVertex.Z);
                        WritePoint(face.SecondVertex.X, face.SecondVertex.Y, face.SecondVertex.Z);
                        WritePoint(face.ThirdVertex.X, face.ThirdVertex.Y, face.ThirdVertex.Z);

                        // Замыкаем треугольник, дублируя первую точку
                        WritePoint(face.FirstVertex.X, face.FirstVertex.Y, face.FirstVertex.Z);

                        stringId++;
                    }
                }
            }
        }

        private void SaveDXF()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "DXF files (*.dxf)|*.dxf",
                DefaultExt = ".dxf",
                FileName = Name
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    Document.Save(dialog.FileName);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
        }


        // REMOVE

        public void Save(string filePath)
        {
            var culture = CultureInfo.InvariantCulture;

            using (var writer = new StreamWriter(filePath, false, Encoding.Default))
            {
                writer.WriteLine(new string(' ', 40));
                writer.WriteLine("5   VARIABLES");
                writer.WriteLine("EAST      N 12  3");
                writer.WriteLine("NORTH     N 12  3");
                writer.WriteLine("RL        N 12  3");
                writer.WriteLine("STRING    C 20  0");
                writer.WriteLine("JOIN      C 10  0");

                int stringId = 1;

                // Локальная функция для форматированной записи точки (избавляет от дублирования кода)
                void WritePoint(double x, double y, double z)
                {
                    string east = x.ToString("F3", culture);
                    string north = y.ToString("F3", culture);
                    string rl = z.ToString("F3", culture);

                    writer.WriteLine(
                        $"{east,-12}" +
                        $"{north,-12}" +
                        $"{rl,-12}" +
                        $"{string.Empty,-20}" +
                        $"{stringId,-10}"
                    );
                }

                foreach (var entity in Document.Entities.All)
                {
                    // 1. Сохранение обычных линий
                    if (entity is Polyline3D polyline)
                    {
                        foreach (var v in polyline.Vertexes)
                        {
                            WritePoint(v.X, v.Y, v.Z);
                        }
                        stringId++;
                    }
                    // 2. Сохранение каркаса (треугольников)
                    else if (entity is Face3D face)
                    {
                        WritePoint(face.FirstVertex.X, face.FirstVertex.Y, face.FirstVertex.Z);
                        WritePoint(face.SecondVertex.X, face.SecondVertex.Y, face.SecondVertex.Z);
                        WritePoint(face.ThirdVertex.X, face.ThirdVertex.Y, face.ThirdVertex.Z);

                        // Замыкаем треугольник, дублируя первую точку
                        WritePoint(face.FirstVertex.X, face.FirstVertex.Y, face.FirstVertex.Z);

                        stringId++;
                    }
                }
            }
        }

        // REMOVE

        public void Save(string filePath, List<DrawerObject> visuals)
        {
            var culture = CultureInfo.InvariantCulture;

            using (var writer = new StreamWriter(filePath, false, Encoding.Default))
            {
                writer.WriteLine(new string(' ', 40));
                writer.WriteLine("5   VARIABLES");
                writer.WriteLine("EAST      N 12  3");
                writer.WriteLine("NORTH     N 12  3");
                writer.WriteLine("RL        N 12  3");
                writer.WriteLine("STRING    C 20  0");
                writer.WriteLine("JOIN      C 10  0");

                int stringId = 1;

                // Локальная функция для форматированной записи точки (избавляет от дублирования кода)
                void WritePoint(double x, double y, double z)
                {
                    string east = x.ToString("F3", culture);
                    string north = y.ToString("F3", culture);
                    string rl = z.ToString("F3", culture);

                    writer.WriteLine(
                        $"{east,-12}" +
                        $"{north,-12}" +
                        $"{rl,-12}" +
                        $"{string.Empty,-20}" +
                        $"{stringId,-10}"
                    );
                }

                foreach (var visual in visuals)
                {
                    // 1. Сохранение обычных линий
                    if (visual.Entity is Polyline3D polyline)
                    {
                        foreach (var v in polyline.Vertexes)
                        {
                            WritePoint(v.X, v.Y, v.Z);
                        }
                        stringId++;
                    }
                    // 2. Сохранение каркаса (треугольников)
                    else if (visual.Entity is Face3D face)
                    {
                        WritePoint(face.FirstVertex.X, face.FirstVertex.Y, face.FirstVertex.Z);
                        WritePoint(face.SecondVertex.X, face.SecondVertex.Y, face.SecondVertex.Z);
                        WritePoint(face.ThirdVertex.X, face.ThirdVertex.Y, face.ThirdVertex.Z);

                        // Замыкаем треугольник, дублируя первую точку
                        WritePoint(face.FirstVertex.X, face.FirstVertex.Y, face.FirstVertex.Z);

                        stringId++;
                    }
                }
            }
        }

        private string FormatNumber(
    double value,
    int width)
        {
            return value
                .ToString("F9", CultureInfo.InvariantCulture)
                .PadRight(width);
        }

        private string FormatString(
            string? value,
            int width)
        {
            value ??= string.Empty;

            if (value.Length > width)
                value = value[..width];

            return value.PadRight(width);
        }

        /*

        public void Save(string filePath)
        {
            var culture = CultureInfo.InvariantCulture;

            using (var writer = new StreamWriter(filePath, false, Encoding.Default))
            {
                writer.WriteLine(new string(' ', 40));
                writer.WriteLine("5   VARIABLES");
                writer.WriteLine("EAST      N 12  3");
                writer.WriteLine("NORTH     N 12  3");
                writer.WriteLine("RL        N 12  3");
                writer.WriteLine("STRING    C 20  0");
                writer.WriteLine("JOIN      C 10  0");

                int stringId = 1;

                foreach (var entity in Document.Entities.All)
                {
                    if (entity is not Polyline3D polyline)
                        continue;

                    foreach (var v in polyline.Vertexes)
                    {
                        string east = v.X.ToString("F3", culture);
                        string north = v.Y.ToString("F3", culture);
                        string rl = v.Z.ToString("F3", culture);

                        writer.WriteLine(
                            $"{east,-12}" +
                            $"{north,-12}" +
                            $"{rl,-12}" +
                            $"{string.Empty,-20}" +
                            $"{stringId,-10}"
                        );
                    }

                    stringId++;
                }
            }
        }

        */
    }
}
