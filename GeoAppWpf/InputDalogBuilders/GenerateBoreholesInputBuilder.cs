using GeoAppCore;
using GeoAppWpf.Services;
using GeoAppWpf.Views.Windows;
using System.Windows;

namespace GeoAppWpf.InputDalogBuilders
{
    public class GenerateBoreholesInputBuilder
    {
        private static BoreholesGeneratorProperties _lastProperties = new();

        public static BoreholesGeneratorProperties? Build(Window? owner = null)
        {
            var generateSamplesItem = new BoolPropertyItem
            {
                Header = "Сгенирировать пробы",
                Value = _lastProperties.GenerateSamples,
            };

            var sampleLengthItem = new DoublePropertyItem
            {
                Header = "Длина пробы",
                Value = _lastProperties.SampleLength,
                IsVisible = generateSamplesItem.Value
            };

            generateSamplesItem.ValueChanged += value =>
            {
                sampleLengthItem.IsVisible = value;
            };

            var items = new PropertyItem[]
            {
                generateSamplesItem,
                sampleLengthItem,
            };

            bool result = InputWindow.Show(
                "Генерация",
                items,
                owner: owner);

            if (!result)
                return null;

            _lastProperties = new()
            {
                GenerateSamples = generateSamplesItem.Value,
                SampleLength = sampleLengthItem.Value,
            };

            return _lastProperties;
        }
    }

    public class GeoDocInputBuilder
    {
        private static GeoDoc _lastProperties = new();

        public static GeoDoc? Build(IEnumerable<BoreholeLine> boreholeLines, Window? owner = null)
        {
            var drawHatch = new BoolPropertyItem
            {
                Header = "Рисовать штриховку",
                Value = _lastProperties.DrawHatch,
            };


            var items = new PropertyItem[]
            {
                drawHatch,
            };

            bool result = InputWindow.Show(
                "Генерация",
                items,
                owner: owner);

            if (!result)
                return null;

            _lastProperties = new()
            {
                DrawHatch = drawHatch.Value,
                BoreholeLines = boreholeLines.ToList()
            };

            return _lastProperties;
        }
    }
}
