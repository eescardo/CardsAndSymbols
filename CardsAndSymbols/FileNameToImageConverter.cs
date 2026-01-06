using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace CardsAndSymbols
{
    public class FileIdToImageConverter : IValueConverter
    {
        private ImageCache? imageCache;

        public ImageCache? ImageCache
        {
            get => this.imageCache;
            set
            {
                // Unsubscribe from old cache
                if (this.imageCache != null)
                {
                    this.imageCache.CacheInvalidated -= this.OnCacheInvalidated;
                }

                this.imageCache = value;

                // Subscribe to new cache
                if (this.imageCache != null)
                {
                    this.imageCache.CacheInvalidated += this.OnCacheInvalidated;
                }
            }
        }

        private void OnCacheInvalidated(object? sender, EventArgs e)
        {
            // When cache is invalidated, we don't need to do anything here
            // The ImageCache will return fresh images on next GetImage call
            // The binding refresh is handled by SymbolViewer.RefreshImageBinding()
        }

        public FileIdToImageConverter()
        {
        }

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
                if (fileId == null || string.IsNullOrEmpty(fileId))
                {
                    return null;
                }

                // Always get fresh image from ImageCache
                // When cache is cleared, it will return fresh images.
                var image = this.ImageCache.GetImage(fileId);
                return image;
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
