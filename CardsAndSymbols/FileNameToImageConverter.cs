using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace CardsAndSymbols
{
    public class FileIdToImageConverter : IValueConverter
    {
        private ImageCache? imageCache;
        private bool isSubscribed = false;

        private ImageCache? GetImageCache()
        {
            // Get ImageCache from App resources and subscribe to events if not already subscribed
            if (this.imageCache == null && Application.Current?.Resources != null)
            {
                if (Application.Current.Resources.TryGetResource("ImageCache", Avalonia.Styling.ThemeVariant.Default, out var cacheObj) && cacheObj is ImageCache cache)
                {
                    this.imageCache = cache;

                    // Subscribe to cache invalidation event
                    if (!this.isSubscribed)
                    {
                        this.imageCache.CacheInvalidated += this.OnCacheInvalidated;
                        this.isSubscribed = true;
                    }
                }
            }
            return this.imageCache;
        }

        private void OnCacheInvalidated(object? sender, EventArgs e)
        {
            // When cache is invalidated, we don't need to do anything here
            // The ImageCache will return fresh images on next GetImage call
        }

        public FileIdToImageConverter()
        {
        }

        public object? Convert(object? value, Type targetType,
            object? parameter, CultureInfo culture)
        {
            try
            {
                var imageCache = this.GetImageCache();
                if (imageCache == null)
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
                var image = imageCache.GetImage(fileId);
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
