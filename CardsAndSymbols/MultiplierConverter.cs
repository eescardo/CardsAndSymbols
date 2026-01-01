using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace CardsAndSymbols
{
    public class MultiplierConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType,
            object? parameter, CultureInfo culture)
        {
            if (value is not double doubleValue || parameter is not double doubleParameter)
            {
                throw new ArgumentException("value and parameter must be double");
            }

            return doubleValue * doubleParameter;
        }

        public object? ConvertBack(object? value, Type targetType,
            object? parameter, CultureInfo culture)
        {
            if (value is not double doubleValue || parameter is not double doubleParameter)
            {
                throw new ArgumentException("value and parameter must be double");
            }

            return doubleValue / doubleParameter;
        }

    }
}
