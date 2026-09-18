using LegendDesignWpf.Controls;
using LegendDesignWpf.Core.MVVM;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace GeoAppWpf.Views.Windows
{
    public abstract class PropertyItem : BaseViewModel
    {
        private string _header = string.Empty;
        private string? _description;
        private string? _validationError;

        private bool _isEnabled = true;

        public object? Data { get; set; }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        private bool _isVisible = true;

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public string Header
        {
            get => _header;
            set => SetProperty(ref _header, value);
        }

        public string? Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public string? ValidationError
        {
            get => _validationError;
            protected set => SetProperty(ref _validationError, value);
        }

        public abstract bool Validate();
    }

    public abstract class SelectionPropertyItem : PropertyItem
    {
        public abstract string FormatItem(object? item);
    }

    public class SelectionPropertyItem<T> : SelectionPropertyItem
    {
        private IEnumerable<T> _items = Array.Empty<T>();

        public IEnumerable<T> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        public Action<T>? ValueChanged { get; set; }

        public Func<T, string>? DisplayFormatter { get; set; }

        private T _value = default!;

        public T Value
        {
            get => _value;
            set
            {
                if (SetProperty(ref _value, value))
                {
                    Validate();
                    ValueChanged?.Invoke(value);
                }
            }
        }

        private readonly Func<T, bool>? _validationRule;

        public SelectionPropertyItem(
            string header,
            IEnumerable<T> items,
            T value = default!,
            Func<T, string>? displayFormatter = null,
            Func<T, bool>? validationRule = null)
        {
            Header = header;
            Items = items;
            _value = value;
            DisplayFormatter = displayFormatter;
            _validationRule = validationRule;
        }

        public override string FormatItem(object? item)
        {
            if (item is T typedItem && DisplayFormatter != null)
                return DisplayFormatter(typedItem);

            // Значения по умолчанию для часто используемых типов
            return item switch
            {
                Type t => t.Name,
                PropertyInfo p => p.Name,
                _ => item?.ToString() ?? string.Empty
            };
        }

        public override bool Validate()
        {
            if (!IsEnabled || !IsVisible)
                return true;

            if (_validationRule != null)
                return _validationRule(Value);

            return true;
        }
    }

    public class BoolPropertyItem : PropertyItem
    {
        public Action<bool> ValueChanged { get; set; }

        private bool _value;
        public bool Value
        {
            get => _value;
            set
            {
                if (SetProperty(ref _value, value))
                {
                    Validate();
                    ValueChanged?.Invoke(value);
                }
            }
        }

        public Func<bool, string?>? Validator { get; set; }

        public override bool Validate()
        {
            if (!IsEnabled || !IsVisible) return true;

            ValidationError = Validator?.Invoke(Value);
            return ValidationError == null;
        }
    }

    public class StringPropertyItem : PropertyItem
    {
        public Action<string> ValueChanged { get; set; }

        private string _value = string.Empty;

        public string Value
        {
            get => _value;
            set
            {
                if (SetProperty(ref _value, value))
                {
                    Validate();
                    ValueChanged?.Invoke(value);
                }
            }
        }

        public Func<string, string?>? Validator { get; set; }

        public override bool Validate()
        {
            if (!IsEnabled || !IsVisible) return true;

            ValidationError = Validator?.Invoke(Value);
            return ValidationError == null;
        }
    }

    public class DoublePropertyItem : PropertyItem
    {
        public Action<double> ValueChanged { get; set; }

        private double _value;
        private string _text = string.Empty;

        public string Text
        {
            get => _text;
            set
            {
                if (!SetProperty(ref _text, value))
                    return;

                if (TryParseDouble(value, out var result))
                {
                    _value = result;
                    Validate();
                }
                else
                {
                    ValidationError = "Некорректное число";
                }

                OnPropertyChanged(nameof(Value));
            }
        }

        public double Value
        {
            get => _value;
            set
            {
                if (SetProperty(ref _value, value))
                {
                    _text = value.ToString();
                    OnPropertyChanged(nameof(Text));
                    Validate();
                    ValueChanged?.Invoke(value);
                }
            }
        }

        public Func<double, string?>? Validator { get; set; }

        public override bool Validate()
        {
            if (!IsEnabled || !IsVisible) return true;

            if (!TryParseDouble(_text, out var result))
                return false;

            ValidationError = Validator?.Invoke(Value);
            return ValidationError == null;
        }

        private bool TryParseDouble(string input, out double result)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                result = 0;
                return false;
            }

            string separator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            string normalizedInput = input.Replace(".", separator).Replace(",", separator);
            return double.TryParse(normalizedInput, out result);
        }
    }

    public class IntegerPropertyItem : PropertyItem
    {
        public Action<int> ValueChanged { get; set; }

        private int _value;
        private string _text = string.Empty;

        public string Text
        {
            get => _text;
            set
            {
                if (!SetProperty(ref _text, value))
                    return;

                if (int.TryParse(value, out var result))
                {
                    _value = result;
                    Validate();
                }
                else
                {
                    ValidationError = "Некорректное число";
                }

                OnPropertyChanged(nameof(Value));
            }
        }

        public int Value
        {
            get => _value;
            set
            {
                if (SetProperty(ref _value, value))
                {
                    _text = value.ToString();
                    OnPropertyChanged(nameof(Text));
                    Validate();
                    ValueChanged?.Invoke(value);
                }
            }
        }

        public Func<int, string?>? Validator { get; set; }

        public override bool Validate()
        {
            if (!IsEnabled || !IsVisible) return true;

            if (!int.TryParse(_text, out var result))
                return false;

            ValidationError = Validator?.Invoke(Value);
            return ValidationError == null;
        }
    }

    public class FileDialogPropertyItem : PropertyItem
    {
        private ObservableCollection<string> _values = new();

        public ObservableCollection<string> Values
        {
            get => _values;
            set
            {
                if (SetProperty(ref _values, value))
                {
                    Validate();
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        public string Placeholder { get; set; } = "Выберите файл";

        public string Value =>
            Values.Count == 0
                ? string.Empty
                : string.Join("; ", Values.Select(Path.GetFileName));

        public ICommand OpenFileDialogCommand { get; set; }

        public Func<IEnumerable<string>, string?>? Validator { get; set; }

        public FileDialogPropertyItem()
        {
            OpenFileDialogCommand = new RelayCommand(OpenFileDialog);
        }

        private void OpenFileDialog()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "DAT files (*.dat)|*.dat",
                DefaultExt = ".dat",
                Multiselect = true
            };

            if (dialog.ShowDialog() != true)
                return;

            Values = new ObservableCollection<string>(dialog.FileNames);
        }

        public override bool Validate()
        {
            if (!IsEnabled || !IsVisible) return true;

            ValidationError = Validator?.Invoke(Values);
            return ValidationError == null;
        }
    }

    public partial class InputWindow : LDWindow
    {
        public IEnumerable<PropertyItem> Items { get; }
        public ICollectionView ItemsView { get; }
        public PropertyItem? HeaderItem { get; }

        public InputWindow(string title, IEnumerable<PropertyItem> items, PropertyItem? headerItem = null, Window? owner = null)
        {
            InitializeComponent();

            Title = title;
            Items = items.ToList();
            ItemsView = CollectionViewSource.GetDefaultView(Items);
            HeaderItem = headerItem;
            Owner = owner;
            DataContext = this;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!GetItemsValidate(Items))
                return;

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        public static bool Show(string title, IEnumerable<PropertyItem> items, PropertyItem? headerItem = null, Window? owner = null)
        {
            var dialog = new InputWindow(title, items, headerItem, owner);

            foreach (var item in dialog.Items)
            {
                item.Validate();

                item.PropertyChanged += (_, _) =>
                {
                    dialog.OkButton.IsEnabled = GetItemsValidate(dialog.Items);
                };
            }

            dialog.OkButton.IsEnabled = GetItemsValidate(dialog.Items);

            if (Application.Current?.MainWindow != null && Application.Current.MainWindow != dialog)
                dialog.Owner = Application.Current.MainWindow;

            return dialog.ShowDialog() == true;
        }

        private static bool GetItemsValidate(IEnumerable<PropertyItem> items)
        {
            bool isValid = true;

            foreach (var item in items)
            {
                if (!item.Validate())
                    isValid = false;
            }

            return isValid;
        }
    }
}
