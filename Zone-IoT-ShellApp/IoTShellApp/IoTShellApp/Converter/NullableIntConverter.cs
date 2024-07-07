using System;
using System.Globalization;
using Xamarin.Forms;

namespace Zone.IoT.Converter
{
    internal class NullableIntConverter : IValueConverter
    {
        // Convert from MVVM Viewmodel Property -> XAML View
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int? nullable = value as int?;
            var result = string.Empty;

            if (nullable.HasValue)
            {
                result = nullable.Value.ToString();
            }

            return result;
        }

        // Convert from XAML View -> MVVM Viewmodel Property
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stringValue = value as string;
            int? result = null;

            if (int.TryParse(stringValue, out var intValue))
            {
                result = intValue;
            }

            return result;
        }
    }
}
