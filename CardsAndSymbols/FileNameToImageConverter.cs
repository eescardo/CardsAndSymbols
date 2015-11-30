using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

using Svg;

namespace CardsAndSymbols
{
    public class FileNameToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            // Do the conversion from file name to ImageSource
            var fileName = value as string;
            if (fileName == null)
            {
                return null;
            }

            if (fileName.EndsWith(".svg"))
            {
                var doc = SvgDocument.Open(fileName);
                doc.Width = (int)((doc.Width / (double)doc.Height) * 512);
                doc.Height = 512;
                return doc.Draw().ToImage();
            }

            return new BitmapImage(new Uri(fileName));
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }
}
