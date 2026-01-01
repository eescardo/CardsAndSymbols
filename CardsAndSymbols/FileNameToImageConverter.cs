using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace CardsAndSymbols
{
    public class FileIdToImageConverter : IValueConverter
    {
        public ImageCache? ImageCache { get; set; }

        public object? Convert(object? value, Type targetType,
            object? parameter, CultureInfo culture)
        {
            try
            {
                if (this.ImageCache == null)
                {
                    return null;
                }
                // Do the conversion from file name to ImageSource
                var fileId = value as string;
                if (fileId == null)
                {
                    return null;
                }

                return this.ImageCache.GetImage(fileId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileIdToImageConverter.Convert EXCEPTION: {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        public object? ConvertBack(object? value, Type targetType,
            object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }
}
