using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using Svg.Skia;
using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;

namespace CardsAndSymbols
{
    public class PdfGenerator
    {
        // PDF layout constants
        private static readonly PageSize PdfPageSize = PageSizes.Letter; // US Letter size (8.5" x 11")
        private const float PageMarginMillimeters = 20.0f;
        private const float MillimetersToPoints = 2.83465f; // 1mm = 2.83465 points
        private const float PixelsToPoints = 0.75f; // At 96 DPI: 1 pixel = 0.75 points
        private const int DefaultFontSize = 12;
        private const int PngEncodingQuality = 100; // Maximum quality (0-100)
        
        // Card rendering constants
        private const float CircleBorderStrokeWidth = 2.0f;
        private const float CircleBorderRadiusAdjustment = 1.0f; // Adjustment to keep border inside circle
        
        private readonly ImageCache? imageCache;
        private readonly double cardBaseSize;
        private readonly double cardScaleFactor;

        public PdfGenerator(ImageCache? imageCache, double cardBaseSize, double cardScaleFactor)
        {
            this.imageCache = imageCache;
            this.cardBaseSize = cardBaseSize;
            this.cardScaleFactor = cardScaleFactor;
        }

        public void GenerateSinglePdf(IEnumerable<CardData> cards, string outputPath)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            
            var cardList = cards.ToList();
            if (cardList.Count == 0) return;

            // Calculate card size in points
            var cardSize = this.cardBaseSize * this.cardScaleFactor;
            var cardSizePoints = (float)(cardSize * PixelsToPoints);
            
            // Calculate available page area (with margins)
            var pageWidthPoints = PdfPageSize.Width;
            var pageHeightPoints = PdfPageSize.Height;
            var marginPoints = PageMarginMillimeters * MillimetersToPoints;
            var availableWidth = pageWidthPoints - (marginPoints * 2);
            var availableHeight = pageHeightPoints - (marginPoints * 2);
            
            // Calculate how many cards fit per row and column
            var cardsPerRow = Math.Max(1, (int)Math.Floor(availableWidth / cardSizePoints));
            var cardsPerColumn = Math.Max(1, (int)Math.Floor(availableHeight / cardSizePoints));
            var cardsPerPage = cardsPerRow * cardsPerColumn;
            
            // Calculate spacing between cards
            var horizontalSpacing = cardsPerRow > 1 ? (availableWidth - (cardsPerRow * cardSizePoints)) / (cardsPerRow - 1) : 0;
            var verticalSpacing = cardsPerColumn > 1 ? (availableHeight - (cardsPerColumn * cardSizePoints)) / (cardsPerColumn - 1) : 0;
            
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PdfPageSize);
                    page.Margin(PageMarginMillimeters, Unit.Millimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(DefaultFontSize));

                    page.Content()
                        .Padding(0)
                        .Column(column =>
                        {
                            int cardIndex = 0;
                            int cardsOnCurrentPage = 0;
                            
                            while (cardIndex < cardList.Count)
                            {
                                // Create rows for this page
                                for (int row = 0; row < cardsPerColumn && cardIndex < cardList.Count && cardsOnCurrentPage < cardsPerPage; row++)
                                {
                                    column.Item().Row(rowContainer =>
                                    {
                                        for (int col = 0; col < cardsPerRow && cardIndex < cardList.Count && cardsOnCurrentPage < cardsPerPage; col++)
                                        {
                                            if (col > 0)
                                            {
                                                rowContainer.Spacing(horizontalSpacing);
                                            }
                                            
                                            rowContainer.ConstantItem(cardSizePoints)
                                                .Element(container => RenderCard(container, cardList[cardIndex]));
                                            
                                            cardIndex++;
                                            cardsOnCurrentPage++;
                                        }
                                    });
                                    
                                    // Add vertical spacing between rows (except for the last row on a page)
                                    if (row < cardsPerColumn - 1 && cardIndex < cardList.Count && cardsOnCurrentPage < cardsPerPage)
                                    {
                                        column.Item().Height(verticalSpacing);
                                    }
                                }
                                
                                // Page break when we've filled a page
                                if (cardIndex < cardList.Count)
                                {
                                    cardsOnCurrentPage = 0;
                                    column.Item().PageBreak();
                                }
                            }
                        });
                });
            });

            document.GeneratePdf(outputPath);
        }

        public void GenerateMultiplePdfs(IEnumerable<CardData> cards, string outputDirectory)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            int cardIndex = 0;
            foreach (var card in cards)
            {
                cardIndex++;
                var fileName = Path.Combine(outputDirectory, $"Card_{cardIndex:D3}.pdf");

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PdfPageSize);
                        page.Margin(PageMarginMillimeters, Unit.Millimetre);
                        page.PageColor(Colors.White);

                        page.Content()
                            .Element(container => RenderCard(container, card));
                    });
                });

                document.GeneratePdf(fileName);
            }
        }

        private void RenderCard(IContainer container, CardData card)
        {
            var cardSize = this.cardBaseSize * this.cardScaleFactor;
            // Convert pixels to points
            var cardSizePoints = (float)(cardSize * PixelsToPoints);

            // Render card to bitmap first, then embed in PDF
            var cardImageBytes = RenderCardToImage(card, (int)cardSize);
            
            if (cardImageBytes != null)
            {
                container
                    .AlignCenter()
                    .AlignMiddle()
                    .Width(cardSizePoints)
                    .Height(cardSizePoints)
                    .Image(cardImageBytes);
            }
        }

        private byte[]? RenderCardToImage(CardData card, int cardSize)
        {
            try
            {
                var info = new SKImageInfo(cardSize, cardSize);
                using (var surface = SKSurface.Create(info))
                {
                    var canvas = surface.Canvas;
                    canvas.Clear(SKColors.White);
                    
                    var centerX = cardSize / 2.0f;
                    var centerY = cardSize / 2.0f;
                    var radius = cardSize / 2.0f;

                    // Draw card circle border
                    using (var paint = new SKPaint
                    {
                        Style = SKPaintStyle.Stroke,
                        Color = SKColors.Black,
                        StrokeWidth = CircleBorderStrokeWidth,
                        IsAntialias = true
                    })
                    {
                        canvas.DrawCircle(centerX, centerY, radius - CircleBorderRadiusAdjustment, paint);
                    }

                    // Draw symbols
                    if (card.Symbols != null && this.imageCache != null)
                    {
                        foreach (var symbol in card.Symbols)
                        {
                            DrawSymbolOnSkiaCanvas(canvas, symbol, centerX, centerY);
                        }
                    }

                    // Convert to PNG bytes
                    using (var image = surface.Snapshot())
                    using (var data = image.Encode(SKEncodedImageFormat.Png, PngEncodingQuality))
                    {
                        return data.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error rendering card to image: {ex.Message}");
                return null;
            }
        }

        private void DrawSymbolOnSkiaCanvas(SKCanvas canvas, SymbolData symbol, float cardCenterX, float cardCenterY)
        {
            if (this.imageCache == null || string.IsNullOrEmpty(symbol.ImageId))
                return;

            try
            {
                // Match the exact calculation from SymbolViewer.UpdateSize()
                var symbolScale = symbol.Size.ToScale();
                var symbolSizeRendered = (float)(Constants.BaseSymbolSize * symbolScale * this.cardScaleFactor);

                // Calculate symbol position - match AdjustOffsets() logic exactly
                // In AdjustOffsets: symbolSize = this.Width (which is symbolSizeRendered)
                // Position: centerX + offsetX - symbolSize / 2
                var offsetX = (float)(symbol.OffsetX * this.cardScaleFactor);
                var offsetY = (float)(symbol.OffsetY * this.cardScaleFactor);
                
                // Position the symbol container (matches AdjustOffsets exactly)
                var x = cardCenterX + offsetX - (symbolSizeRendered / 2.0f);
                var y = cardCenterY + offsetY - (symbolSizeRendered / 2.0f);

                // Get image - try file path first, then bitmap conversion
                SKImage? skImage = null;
                var fileName = this.imageCache.GetFileName(symbol.ImageId);
                
                if (File.Exists(fileName))
                {
                    // For PNG files, load directly
                    if (fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var data = SKData.Create(fileName))
                        {
                            skImage = SKImage.FromEncodedData(data);
                        }
                    }
                    // For SVG files, render directly at target size for high quality
                    else if (fileName.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                    {
                        skImage = RenderSvgToSkImageAtSize(fileName, (int)symbolSizeRendered);
                    }
                }

                // Fallback: convert from Avalonia bitmap
                if (skImage == null)
                {
                    var avaloniaImage = this.imageCache.GetImage(symbol.ImageId);
                    if (avaloniaImage != null)
                    {
                        skImage = ConvertAvaloniaBitmapToSkImage(avaloniaImage, (int)symbolSizeRendered);
                    }
                }

                if (skImage != null)
                {
                    // If SVG was rendered at target size, it's already the right size - draw directly
                    // Otherwise, scale uniformly to match Stretch="Uniform" behavior
                    var imageWidth = skImage.Width;
                    var imageHeight = skImage.Height;
                    
                    if (imageWidth == (int)symbolSizeRendered && imageHeight == (int)symbolSizeRendered)
                    {
                        // Already at target size (e.g., SVG rendered at target size) - draw directly
                        canvas.DrawImage(skImage, x, y);
                    }
                    else
                    {
                        // Calculate uniform scale to fill the symbolSizeRendered area (like Stretch="Uniform")
                        var scaleX = symbolSizeRendered / imageWidth;
                        var scaleY = symbolSizeRendered / imageHeight;
                        var uniformScale = Math.Min(scaleX, scaleY); // Uniform scaling
                        
                        var scaledWidth = imageWidth * uniformScale;
                        var scaledHeight = imageHeight * uniformScale;
                        
                        // Center the scaled image within the symbolSizeRendered area
                        var drawX = x + (symbolSizeRendered - scaledWidth) / 2.0f;
                        var drawY = y + (symbolSizeRendered - scaledHeight) / 2.0f;
                        
                        // Draw with uniform scaling, matching Avalonia's Stretch="Uniform" behavior
                        canvas.Save();
                        canvas.Translate(drawX, drawY);
                        canvas.Scale(uniformScale, uniformScale);
                        canvas.DrawImage(skImage, 0, 0);
                        canvas.Restore();
                    }
                    
                    skImage.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error drawing symbol {symbol.ImageId}: {ex.Message}");
            }
        }

        private SKImage? RenderSvgToSkImageAtSize(string svgPath, int targetSize)
        {
            try
            {
                var svg = new SKSvg();
                svg.Load(svgPath);
                
                if (svg.Picture == null)
                    return null;

                // Render SVG directly at target size for high quality (vector to raster at final size)
                // This avoids pixelation from scaling up a small raster image
                var info = new SKImageInfo(targetSize, targetSize);
                using (var surface = SKSurface.Create(info))
                {
                    var canvas = surface.Canvas;
                    canvas.Clear(SKColors.Transparent);
                    
                    // Scale to fit target size uniformly (matching Stretch="Uniform" behavior)
                    var bounds = svg.Picture.CullRect;
                    var scale = Math.Min((float)targetSize / bounds.Width, (float)targetSize / bounds.Height);
                    
                    // Center the SVG within the target size
                    var centerX = bounds.Left + bounds.Width / 2.0f;
                    var centerY = bounds.Top + bounds.Height / 2.0f;
                    
                    canvas.Save();
                    canvas.Translate(targetSize / 2.0f, targetSize / 2.0f);
                    canvas.Scale(scale, scale);
                    canvas.Translate(-centerX, -centerY);
                    canvas.DrawPicture(svg.Picture);
                    canvas.Restore();
                    
                    return surface.Snapshot();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error rendering SVG {svgPath}: {ex.Message}");
                return null;
            }
        }

        private SKImage? ConvertAvaloniaBitmapToSkImage(AvaloniaBitmap avaloniaBitmap, int targetSize)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    avaloniaBitmap.Save(memoryStream);
                    memoryStream.Position = 0;
                    
                    var imageData = memoryStream.ToArray();
                    var data = SKData.CreateCopy(imageData);
                    var originalImage = SKImage.FromEncodedData(data);
                    
                    if (originalImage == null)
                    {
                        data.Dispose();
                        return null;
                    }

                    // Don't resize here - we'll scale uniformly in DrawSymbolOnSkiaCanvas to match Stretch="Uniform"
                    // Just return the original image at its natural size (create a copy)
                    var copyData = SKData.CreateCopy(imageData);
                    var copy = SKImage.FromEncodedData(copyData);
                    originalImage.Dispose();
                    data.Dispose();
                    return copy;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error converting Avalonia bitmap to SKImage: {ex.Message}");
                return null;
            }
        }

    }
}

