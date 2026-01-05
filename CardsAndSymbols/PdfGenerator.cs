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
            var cardSizePoints = (float)(cardSize * Constants.PixelsToPoints);
            
            // Calculate available page area (with margins)
            var pageWidthPoints = PdfPageSize.Width;
            var pageHeightPoints = PdfPageSize.Height;
            var marginPoints = PageMarginMillimeters * (float)Constants.MillimetersToPoints;
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
            var cardSizePoints = (float)(cardSize * Constants.PixelsToPoints);

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

                // Get rotation angle in degrees
                var rotationDegrees = symbol.RotationDegrees;

                // Get image file path
                var fileName = this.imageCache.GetFileName(symbol.ImageId);

                if (File.Exists(fileName) && fileName.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                {
                    // For SVG files, always render directly to canvas (with optional rotation)
                    // This avoids artifacts from rotating a rasterized image
                    RenderSvg(fileName, symbolSizeRendered, canvas, x, y, rotationDegrees);
                    return; // Already drawn, no need to continue
                }

                // For PNG files or fallback from Avalonia bitmap, load as image
                SKImage? skImage = null;

                if (File.Exists(fileName) && fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    using (var data = SKData.Create(fileName))
                    {
                        skImage = SKImage.FromEncodedData(data);
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
                    RenderBitmap(canvas, skImage, symbolSizeRendered, x, y, rotationDegrees);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error drawing symbol {symbol.ImageId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Renders an SVG file directly to the card canvas with optional rotation.
        /// This closely matches the earlier, working implementation and keeps all
        /// SVG-specific coordinate math local to this method to avoid subtle layout bugs.
        /// </summary>
        private void RenderSvg(string svgPath, float targetSize, SKCanvas canvas, float x, float y, double rotationDegrees = 0)
        {
            try
            {
                var svg = new SKSvg();
                svg.Load(svgPath);

                if (svg.Picture == null)
                    return;

                // Scale to fit target size uniformly (matching Stretch="Uniform" behavior)
                var bounds = svg.Picture.CullRect;
                var scale = Math.Min(targetSize / bounds.Width, targetSize / bounds.Height);

                // Center of the SVG content in its own coordinate system
                var svgCenterX = bounds.Left + bounds.Width / 2.0f;
                var svgCenterY = bounds.Top + bounds.Height / 2.0f;

                // Center of the symbol area on the card
                var imageCenterX = x + targetSize / 2.0f;
                var imageCenterY = y + targetSize / 2.0f;

                canvas.Save();

                // Rotate around the center of the symbol area (if rotation is needed)
                if (rotationDegrees != 0)
                {
                    canvas.Translate(imageCenterX, imageCenterY);
                    canvas.RotateDegrees((float)rotationDegrees);
                    canvas.Translate(-imageCenterX, -imageCenterY);
                }

                // Scale and center the SVG within the target area
                canvas.Translate(imageCenterX, imageCenterY);
                canvas.Scale(scale, scale);
                canvas.Translate(-svgCenterX, -svgCenterY);

                // Draw the SVG directly (vector rendering, no intermediate rasterization)
                canvas.DrawPicture(svg.Picture);

                canvas.Restore();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error rendering SVG {svgPath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Renders a bitmap (PNG or converted Avalonia bitmap) to the card canvas with
        /// uniform scaling and optional rotation around the bitmap's visual center.
        /// </summary>
        private void RenderBitmap(SKCanvas canvas, SKImage bitmap, float targetSize, float x, float y, double rotationDegrees)
        {
            // Scale uniformly to match Stretch="Uniform" behavior
            var imageWidth = (float)bitmap.Width;
            var imageHeight = (float)bitmap.Height;

            var scaleX = targetSize / imageWidth;
            var scaleY = targetSize / imageHeight;
            var uniformScale = (float)Math.Min(scaleX, scaleY);

            var scaledWidth = imageWidth * uniformScale;
            var scaledHeight = imageHeight * uniformScale;

            // Center the scaled image within the target area
            var drawX = x + (targetSize - scaledWidth) / 2.0f;
            var drawY = y + (targetSize - scaledHeight) / 2.0f;

            // Center of the scaled image (for rotation)
            var imageCenterX = drawX + scaledWidth / 2.0f;
            var imageCenterY = drawY + scaledHeight / 2.0f;

            canvas.Save();

            // Apply rotation around the actual image center (if rotation is needed)
            if (rotationDegrees != 0)
            {
                canvas.Translate(imageCenterX, imageCenterY);
                canvas.RotateDegrees((float)rotationDegrees);
                canvas.Translate(-imageCenterX, -imageCenterY);
            }

            // Now draw the image: translate to draw position, scale, then draw
            canvas.Translate(drawX, drawY);
            if (uniformScale != 1.0f)
            {
                canvas.Scale(uniformScale, uniformScale);
            }
            canvas.DrawImage(bitmap, 0, 0);

            canvas.Restore();
            bitmap.Dispose();
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

