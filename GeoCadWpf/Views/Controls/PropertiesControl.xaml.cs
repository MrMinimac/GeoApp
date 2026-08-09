using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace GeoCadWpf.Views.Controls
{
    public class PropertyItemViewModel : INotifyPropertyChanged
    {
        private readonly object _targetObject;
        private readonly PropertyInfo _propertyInfo;

        public PropertyItemViewModel(object targetObject, PropertyInfo propertyInfo)
        {
            _targetObject = targetObject;
            _propertyInfo = propertyInfo;
        }

        public string Name => _propertyInfo.Name;

        public object? Value
        {
            get => _propertyInfo.GetValue(_targetObject);
            set
            {
                if (_propertyInfo.CanWrite)
                {
                    try
                    {
                        var convertedValue = Convert.ChangeType(value, _propertyInfo.PropertyType);
                        _propertyInfo.SetValue(_targetObject, convertedValue);
                        OnPropertyChanged();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка конвертации: {ex.Message}");
                    }
                }
            }
        }

        public bool IsReadOnly => !_propertyInfo.CanWrite;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public partial class PropertiesControl : UserControl
    {
        public PropertiesControl()
        {
            InitializeComponent();
        }

        public ObservableCollection<PropertyItemViewModel> PropertiesList { get; } = new();

        #region Item Source Property

        public static readonly DependencyProperty SelectedObject =
            DependencyProperty.Register(nameof(Item), typeof(object),
                typeof(PropertiesControl), new PropertyMetadata(null, OnItemChanged));

        public object? Item
        {
            get => GetValue(SelectedObject);
            set => SetValue(SelectedObject, value);
        }

        private static void OnItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PropertiesControl control)
            {
                control.LoadProperties(e.NewValue);
            }
        }

        #endregion
        
        private void LoadProperties(object? obj)
        {
            PropertiesList.Clear();

            if (obj == null) return;

            var type = obj.GetType();

            var properties = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetIndexParameters().Length == 0)
                .Reverse();

            foreach (var prop in properties)
            {
                PropertiesList.Add(new PropertyItemViewModel(obj, prop));
            }
        }
    }
}
