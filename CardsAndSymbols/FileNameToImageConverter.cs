using System;
using System.Globalization;
using System.Windows.Data;

namespace CardsAndSymbols
{
    public class FileIdToImageConverter : IValueConverter
    {
        public ImageCache ImageCache { get; set; }

        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (this.ImageCache == null)
            {
                throw new InvalidOperationException("Cache has not been initialized");
            }
            // Do the conversion from file name to ImageSource
            var fileId = value as string;
            if (fileId == null)
            {
                return null;
            }

            return this.ImageCache.GetImage(fileId);
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }
}
