using System;
using System.Collections.Generic;
using System.IO;
using Svg.Skia;
using SkiaSharp;
using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;

namespace CardsAndSymbols
{
    public class ImageCache
    {
        private Dictionary<string, string> idToFileName = new Dictionary<string, string>();
        private Dictionary<string, AvaloniaBitmap> fileNameToImage = new Dictionary<string, AvaloniaBitmap>();
        private int nextId = 0;

        /// <summary>
        /// Event fired when the cache is cleared/invalidated.
        /// Subscribers should refresh their bindings to reload images.
        /// </summary>
        public event EventHandler? CacheInvalidated;

        public ImageCache()
        {
        }

        public string AssignNewId(string fileName)
        {
            string id = (nextId++).ToString();
            this.idToFileName.Add(id, fileName);

            return id;
        }

        public string GetFileName(string fileId)
        {
            if (!this.idToFileName.TryGetValue(fileId, out var fileName))
            {
                throw new KeyNotFoundException($"ImageId '{fileId}' not found in cache. Cache may have been cleared.");
            }
            return fileName;
        }

        public AvaloniaBitmap? GetImage(string fileId)
        {
            try
            {
                // Check if ID exists in cache
                if (!this.idToFileName.ContainsKey(fileId))
                {
                    return null;
                }

                var fileName = this.GetFileName(fileId);

                // Check if file exists
                if (!File.Exists(fileName))
                {
                    return null;
                }

                AvaloniaBitmap? imageSource;
                if (this.fileNameToImage.TryGetValue(fileName, out imageSource))
                {
                    return imageSource;
                }

                if (fileName.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                {
                    var svg = new SKSvg();
                    svg.Load(fileName);
                    
                    // Use the SVG's natural size - Avalonia will scale it as needed
                    // The Image control has Stretch="Uniform" so it will handle scaling
                    var originalWidth = svg.Picture?.CullRect.Width ?? 64;
                    var originalHeight = svg.Picture?.CullRect.Height ?? 64;
                    var width = (int)Math.Ceiling(originalWidth);
                    var height = (int)Math.Ceiling(originalHeight);
                    
                    // Create SkiaSharp surface and render SVG at natural size
                    var info = new SKImageInfo(width, height);
                    using (var surface = SKSurface.Create(info))
                    {
                        var canvas = surface.Canvas;
                        canvas.Clear(SKColors.Transparent);
                        canvas.DrawPicture(svg.Picture);
                        
                        using (var image = surface.Snapshot())
                        using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                        {
                            using (var stream = new MemoryStream(data.ToArray()))
                            {
                                imageSource = new AvaloniaBitmap(stream);
                            }
                        }
                    }
                }
                else
                {
                    imageSource = new AvaloniaBitmap(fileName);
                }

                this.fileNameToImage.Add(fileName, imageSource);
                return imageSource;
            }
            catch (KeyNotFoundException ex)
            {
                System.Diagnostics.Debug.WriteLine($"ImageCache.GetImage ERROR: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ImageCache.GetImage ERROR: {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        public void Clear(ImageCacheFlags flags)
        {
            bool wasCleared = false;

            if (flags.HasFlag(ImageCacheFlags.ClearIds))
            {
                this.idToFileName.Clear();
                this.nextId = 0;
                wasCleared = true;
            }

            if (flags.HasFlag(ImageCacheFlags.ClearFiles))
            {
                this.fileNameToImage.Clear();
                wasCleared = true;
            }

            // Notify subscribers if anything was cleared
            if (wasCleared)
            {
                this.CacheInvalidated?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
