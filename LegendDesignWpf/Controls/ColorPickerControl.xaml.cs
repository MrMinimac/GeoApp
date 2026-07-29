using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ColorConverter = LegendDesignWpf.Converters.ColorConverter;

namespace LegendDesignWpf.Controls
{
    // Обязательно указываем INotifyPropertyChanged в объявлении класса
    public partial class ColorPickerControl : UserControl, INotifyPropertyChanged
    {
        #region Private Fields

        private Color _selectedColor;
        private double _hue;
        private double _saturation;
        private double _value;
        private byte _r, _g, _b;

        // Поля для хранения текущих динамических размеров зоны SV
        private double _svWidth = 250;
        private double _svHeight = 250;

        #endregion

        #region Properties

        public Color SelectedColor
        {
            get => _selectedColor;
            private set
            {
                _selectedColor = value;
                OnPropertyChanged(nameof(SelectedColor));
                OnPropertyChanged(nameof(SelectedBrush));
            }
        }

        public double Hue
        {
            get => _hue;
            set
            {
                _hue = value;
                UpdateFromHSV();
                OnPropertyChanged(nameof(Hue));
                OnPropertyChanged(nameof(PureHueColor));
            }
        }

        public byte R
        {
            get => _r;
            set { _r = value; UpdateFromRGB(); }
        }

        public byte G
        {
            get => _g;
            set { _g = value; UpdateFromRGB(); }
        }

        public byte B
        {
            get => _b;
            set { _b = value; UpdateFromRGB(); }
        }

        public string Hex
        {
            get => $"#{R:X2}{G:X2}{B:X2}";
            set
            {
                if (value.StartsWith("#") && value.Length == 7)
                {
                    R = byte.Parse(value.Substring(1, 2), NumberStyles.HexNumber);
                    G = byte.Parse(value.Substring(3, 2), NumberStyles.HexNumber);
                    B = byte.Parse(value.Substring(5, 2), NumberStyles.HexNumber);
                }
            }
        }

        public Brush SelectedBrush => new SolidColorBrush(Color.FromRgb(R, G, B));

        public Color PureHueColor => ColorConverter.HsvToColor(Hue, 1, 1);

        // Динамический расчет маркера: ширина/высота зоны минус половина размера маркера (7.5) для центрирования
        public double MarkerX => (_saturation * _svWidth) - 7.5;
        public double MarkerY => ((1 - _value) * _svHeight) - 7.5;

        #endregion

        public ColorPickerControl()
        {
            InitializeComponent();
            SetFromColor(Color.FromArgb(255, 255, 255, 255));
        }

        // Обновляем координаты при изменении размеров области градиентов
        private void SvArea_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width > 0 && e.NewSize.Height > 0)
            {
                _svWidth = e.NewSize.Width;
                _svHeight = e.NewSize.Height;

                // Триггерим обновление биндингов маркера
                OnPropertyChanged(nameof(MarkerX));
                OnPropertyChanged(nameof(MarkerY));
            }
        }

        public void SetSV(double s, double v)
        {
            _saturation = Clamp(s, 0, 1);
            _value = Clamp(v, 0, 1);
            UpdateFromHSV();
        }

        public void SetFromColor(Color color)
        {
            ColorConverter.ColorToHsv(color, out _hue, out _saturation, out _value);
            _r = color.R;
            _g = color.G;
            _b = color.B;
            SelectedColor = color;
            NotifyAll();
        }

        private void SvMouse(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            var pos = e.GetPosition((Canvas)sender);

            // Используем динамические размеры для расчета H/V
            SetSV(pos.X / _svWidth, 1 - (pos.Y / _svHeight));
        }

        private void UpdateFromHSV()
        {
            var c = ColorConverter.HsvToColor(Hue, _saturation, _value);
            _r = c.R;
            _g = c.G;
            _b = c.B;
            SelectedColor = c;
            NotifyAll();
        }

        private void UpdateFromRGB()
        {
            var c = Color.FromRgb(R, G, B);
            ColorConverter.ColorToHsv(c, out _hue, out _saturation, out _value);
            SelectedColor = c;
            NotifyAll();
        }

        private void NotifyAll()
        {
            OnPropertyChanged(nameof(R));
            OnPropertyChanged(nameof(G));
            OnPropertyChanged(nameof(B));
            OnPropertyChanged(nameof(Hex));
            OnPropertyChanged(nameof(SelectedBrush));
            OnPropertyChanged(nameof(MarkerX));
            OnPropertyChanged(nameof(MarkerY));
            OnPropertyChanged(nameof(Hue));
            OnPropertyChanged(nameof(PureHueColor));
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private void FlatButton_Click(object sender, RoutedEventArgs e)
        {
            // Логика нажатия
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}