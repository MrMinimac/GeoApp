using GeoAppWpf.Converters;
using GeoAppWpf.Models;
using System.Windows;

namespace GeoAppCore.Services
{
    public enum Message
    {
        CrtErr,
        ErrTittle,
        AcImpErr,
        AcDocEmp,
        AcImpOk,
    }

    public static class LocaleService
    {
        public static void ShowError(Exception e)
        {
            MessageBox.Show(
                e.Message,
                LocaleService.Get(Message.ErrTittle),
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static string Get(Message m)
        {
            return m switch
            {
                Message.ErrTittle => "Ошибка",
                Message.CrtErr => "Произошла критическая ошибка",
                Message.AcDocEmp => "Документ пуст!",
                Message.AcImpOk => "Проект отправлен в AutoCAD",
                Message.AcImpErr => "Произошла ошибка при импорте.\nУбедитесь что у вас запущен AutoCad и загружен плагин.",
                _ => "NULL"
            };
        }

        public static ColumnInfo? GetColumnInfo(string propertyName)
        {
            ColumnsData.TryGetValue(propertyName, out var info);
            return info;
        }

        private static readonly Dictionary<string, ColumnInfo> ColumnsData = new()
        {
            // BoreholeLine
            [nameof(BoreholeLine.Id)] = ColumnInfo.Create(nameof(BoreholeLine.Id), "№ БЛ"),
            [nameof(BoreholeLine.Azimuth)] = ColumnInfo.Create(nameof(BoreholeLine.Azimuth), "Азимут"),
            [nameof(BoreholeLine.First)] = ColumnInfo.CreateHidden(nameof(BoreholeLine.First)),
            [nameof(BoreholeLine.Last)] = ColumnInfo.CreateHidden(nameof(BoreholeLine.Last)),
            [nameof(BoreholeLine.Boreholes)] = ColumnInfo.CreateHidden(nameof(BoreholeLine.Boreholes)),
            [nameof(BoreholeLine.MinZ)] = ColumnInfo.CreateHidden(nameof(BoreholeLine.MinZ)),
            [nameof(BoreholeLine.MaxZ)] = ColumnInfo.CreateHidden(nameof(BoreholeLine.MaxZ)),

            // Borehole
            [nameof(Borehole.Id)] = ColumnInfo.Create(nameof(Borehole.Id), "№"),
            [nameof(Borehole.Deapth)] = ColumnInfo.Create(nameof(Borehole.Deapth), "Глубина"),
            [nameof(Borehole.SamplesCount)] = ColumnInfo.Create(nameof(Borehole.SamplesCount), "Кол-во проб"),
            [nameof(Borehole.LithologyIntervals)] = ColumnInfo.CreateHidden(nameof(Borehole.LithologyIntervals)),
            [nameof(Borehole.BoreholeLineId)] = ColumnInfo.CreateHidden(nameof(Borehole.BoreholeLineId)),

            // Sample
            [nameof(Sample.Length)] = ColumnInfo.Create(nameof(Sample.Length), "Длина"),
            [nameof(Sample.From)] = ColumnInfo.Create(nameof(Sample.From), "От"),
            [nameof(Sample.To)] = ColumnInfo.Create(nameof(Sample.To), "До"),
            [nameof(Sample.Capacity)] = ColumnInfo.Create(nameof(Sample.Capacity), "Объем"),
            [nameof(Sample.Grade)] = ColumnInfo.Create(nameof(Sample.Grade), "Сод.", new ValueConverter()),
            [nameof(Sample.AvgGrade)] = ColumnInfo.Create(nameof(Sample.AvgGrade), "Ср. сод.", new ValueConverter()),
            [nameof(Sample.VertReserve)] = ColumnInfo.Create(nameof(Sample.VertReserve), "Верт. запас", new ValueConverter()),
            [nameof(Sample.PureAvgGrade)] = ColumnInfo.Create(nameof(Sample.PureAvgGrade), "Ср. сод. (чист.)", new ValueConverter()),
            [nameof(Sample.PureVertReserve)] = ColumnInfo.Create(nameof(Sample.PureVertReserve), "Верт. запас (чист.)", new ValueConverter()),
            [nameof(Sample.Lithologies)] = ColumnInfo.Create(nameof(Sample.Lithologies), "Литология", new LitologiesConverter()),

            // DXF
            ["IsNormalized"] = ColumnInfo.CreateHidden("IsNormalized"),
            ["Vertexes"] = ColumnInfo.Create("IsNormalized", "Вершины"),
            ["Layer"] = ColumnInfo.Create("Layer", "Слой"),
            ["Entity"] = ColumnInfo.Create("Entity", "Объект"),

            // Other
            ["Color"] = ColumnInfo.Create("Color", "Цвет", cellTemplate: "ColorTemplate")
        };
    }
}
