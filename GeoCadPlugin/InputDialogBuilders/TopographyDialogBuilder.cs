using GeoUIWpf.Views.Windows;
using System.Windows;

namespace GeoCadPlugin
{
    namespace InputDialogBuilders
    {
        public class TopographyProperties
        {
            // Настройки DEM и горизонталей
            public double Interval { get; set; } = 1.0;
            public bool SmoothDem { get; set; } = true;
            public int SmoothDemIterations { get; set; } = 2;

            // Настройки обработки полученных линий
            public bool SimplifyLines { get; set; } = true;
            public double SimplifyTolerance { get; set; } = 3.0;

            public bool SmoothLines { get; set; } = true;
            public double SmoothStep { get; set; } = 10.0;

            public bool JoinLines { get; set; } = true;
            public double JoinMaxGap { get; set; } = 100.0;
            public double JoinMaxTail { get; set; } = 300.0;
        }

        public class TopographyDialogBuilder
        {
            private static TopographyProperties _lastProperties = new();

            public static TopographyProperties? Build(IntPtr? ownerHandle = null)
            {
                #region Items

                var intervalItem = new DoublePropertyItem
                {
                    Header = "Шаг горизонталей в метрах",
                    Value = _lastProperties.Interval,
                };

                var smoothDemItem = new BoolPropertyItem
                {
                    Header = "Сгладить DEM",
                    Value = _lastProperties.SmoothDem,
                };

                var smoothDemIterationsItem = new IntegerPropertyItem
                {
                    Header = "Итераций для сглаживания DEM",
                    Value = _lastProperties.SmoothDemIterations,
                    IsVisible = _lastProperties.SmoothDem,
                };

                var simplifyLinesItem = new BoolPropertyItem
                {
                    Header = "Упростить линии",
                    Value = _lastProperties.SimplifyLines,
                };

                var simplifyToleranceItem = new DoublePropertyItem
                {
                    Header = "Толерантность упрощения (м)",
                    Value = _lastProperties.SimplifyTolerance,
                    IsVisible = _lastProperties.SimplifyLines,
                };

                var smoothLinesItem = new BoolPropertyItem
                {
                    Header = "Сгладить линии",
                    Value = _lastProperties.SmoothLines,
                };

                var smoothStepItem = new DoublePropertyItem
                {
                    Header = "Шаг сглаживания линий (м)",
                    Value = _lastProperties.SmoothStep,
                    IsVisible = _lastProperties.SmoothLines,
                };

                var joinLinesItem = new BoolPropertyItem
                {
                    Header = "Объединить разорванные линии",
                    Value = _lastProperties.JoinLines,
                };

                var joinMaxGapItem = new DoublePropertyItem
                {
                    Header = "Максимальный разрыв (м)",
                    Value = _lastProperties.JoinMaxGap,
                    IsVisible = _lastProperties.JoinLines,
                };

                var joinMaxTailItem = new DoublePropertyItem
                {
                    Header = "Максимальная длина хвоста (м)",
                    Value = _lastProperties.JoinMaxTail,
                    IsVisible = _lastProperties.JoinLines,
                };

                #endregion

                #region Events

                smoothDemItem.ValueChanged += val => smoothDemIterationsItem.IsVisible = val;

                simplifyLinesItem.ValueChanged += val => simplifyToleranceItem.IsVisible = val;

                smoothLinesItem.ValueChanged += val => smoothStepItem.IsVisible = val;

                joinLinesItem.ValueChanged += val =>
                {
                    joinMaxGapItem.IsVisible = val;
                    joinMaxTailItem.IsVisible = val;
                };

                #endregion

                var items = new PropertyItem[]
                {
                    intervalItem,

                    smoothDemItem,
                    smoothDemIterationsItem,

                    simplifyLinesItem,
                    simplifyToleranceItem,

                    smoothLinesItem,
                    smoothStepItem,

                    joinLinesItem,
                    joinMaxGapItem,
                    joinMaxTailItem
                };

                bool result = InputWindow.Show(
                    "Topography builder",
                    items,
                    ownerHandle: ownerHandle);

                if (!result)
                    return null;

                _lastProperties = new TopographyProperties
                {
                    Interval = intervalItem.Value,
                    SmoothDem = smoothDemItem.Value,
                    SmoothDemIterations = smoothDemIterationsItem.Value,

                    SimplifyLines = simplifyLinesItem.Value,
                    SimplifyTolerance = simplifyToleranceItem.Value,

                    SmoothLines = smoothLinesItem.Value,
                    SmoothStep = smoothStepItem.Value,

                    JoinLines = joinLinesItem.Value,
                    JoinMaxGap = joinMaxGapItem.Value,
                    JoinMaxTail = joinMaxTailItem.Value
                };

                return _lastProperties;
            }
        }
    }
}