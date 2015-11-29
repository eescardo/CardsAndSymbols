using System;
using System.Globalization;
using System.Windows.Data;

namespace CardsAndSymbols
{
    public class SymbolSizeToScaleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            // Do the conversion from size to scale
            var size = value as SymbolSize?;
            if (size == null)
            {
                return 0.0;
            }

            return size.Value.ToScale();
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            // Do the conversion from scale to size
            var scale = value as double?;
            if (scale == null)
            {
                throw new ArgumentException("invalid scale value", "value");
            }

            return scale.Value.ToSymbolSize();
        }

    }
}
