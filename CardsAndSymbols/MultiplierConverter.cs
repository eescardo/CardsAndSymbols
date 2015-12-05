using System;
using System.Globalization;
using System.Windows.Data;

namespace CardsAndSymbols
{
    public class MultiplierConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (value.GetType() != typeof(double) || parameter.GetType() != typeof(double))
            {
                throw new ArgumentException("value and parameter must be double");
            }

            return ((double)value) * ((double)parameter);
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (value.GetType() != typeof(double) || parameter.GetType() != typeof(double))
            {
                throw new ArgumentException("value and parameter must be double");
            }

            return ((double)value) / ((double)parameter);
        }

    }
}
