using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using Svg.Skia;

namespace CardsAndSymbols
{
    public class PdfGenerator
    {
        // PDF layout constants
        private static readonly PageSize PdfPageSize = PageSizes.Letter; // US Letter size (8.5" x 11")
        private const float PageMarginMillimeters = 20.0f;
        private const int DefaultFontSize = 12;
        
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

            // Always use vector rendering - SVG symbols as vectors, PNG symbols as embedded images
            var cardSizePixels = (float)cardSize;
            var centerX = cardSizePixels / 2.0f;
            var centerY = cardSizePixels / 2.0f;
            var radius = cardSizePixels / 2.0f;

            // Build a composite SVG containing the circle and all symbols (SVG as vectors, PNG as images)
            var compositeSvg = BuildCompositeSvg(card, cardSizePixels, centerX, centerY, radius);
            
            if (compositeSvg != null)
            {
                container
                    .AlignCenter()
                    .AlignMiddle()
                    .Width(cardSizePoints)
                    .Height(cardSizePoints)
                    .Svg(compositeSvg);
            }
        }

        /// <summary>
        /// Builds a composite SVG containing the circle border and all symbols.
        /// SVG symbols are embedded as nested SVG elements (vectors), PNG symbols as embedded images.
        /// </summary>
        private string? BuildCompositeSvg(CardData card, float cardSizePixels, float cardCenterX, float cardCenterY, float radius)
        {
            if (card.Symbols == null || this.imageCache == null)
                return null;

            try
            {
                var svgBuilder = new System.Text.StringBuilder();
                svgBuilder.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                svgBuilder.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" width=\"{cardSizePixels}\" height=\"{cardSizePixels}\" viewBox=\"0 0 {cardSizePixels} {cardSizePixels}\">");

                // Draw circle border
                var strokeWidthPixels = CircleBorderStrokeWidth;
                var circleRadius = radius - CircleBorderRadiusAdjustment;
                svgBuilder.AppendLine($"  <circle cx=\"{cardCenterX}\" cy=\"{cardCenterY}\" r=\"{circleRadius}\" fill=\"none\" stroke=\"black\" stroke-width=\"{strokeWidthPixels}\"/>");

                // Add all symbols (SVG as vectors, PNG as embedded images)
                foreach (var symbol in card.Symbols)
                {
                    var symbolMarkup = BuildSymbolMarkup(symbol, cardCenterX, cardCenterY);
                    if (symbolMarkup != null)
                    {
                        svgBuilder.AppendLine(symbolMarkup);
                    }
                }

                svgBuilder.AppendLine("</svg>");
                return svgBuilder.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error building composite SVG: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Builds SVG markup for a single symbol with proper positioning, scaling, and rotation.
        /// Handles both SVG (as nested SVG) and PNG (as embedded image) symbols.
        /// </summary>
        private string? BuildSymbolMarkup(SymbolData symbol, float cardCenterX, float cardCenterY)
        {
            if (this.imageCache == null || string.IsNullOrEmpty(symbol.ImageId))
                return null;

            try
            {
                // Match the exact calculation from SymbolViewer.UpdateSize()
                var symbolScale = symbol.Size.ToScale();
                var symbolSizeRendered = (float)(Constants.BaseSymbolSize * symbolScale * this.cardScaleFactor);

                // Calculate symbol position - match AdjustOffsets() logic exactly
                var offsetX = (float)(symbol.OffsetX * this.cardScaleFactor);
                var offsetY = (float)(symbol.OffsetY * this.cardScaleFactor);

                // Position the symbol container (matches AdjustOffsets exactly)
                var x = cardCenterX + offsetX - (symbolSizeRendered / 2.0f);
                var y = cardCenterY + offsetY - (symbolSizeRendered / 2.0f);

                // Get rotation angle in degrees
                var rotationDegrees = symbol.RotationDegrees;

                // Get image file path
                var fileName = this.imageCache.GetFileName(symbol.ImageId);

                if (!File.Exists(fileName))
                    return null;

                // Handle PNG symbols as embedded images
                if (fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    return BuildPngSymbolMarkup(fileName, x, y, symbolSizeRendered, rotationDegrees);
                }

                // Handle SVG symbols as nested SVG elements
                if (!fileName.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                    return null;

                return BuildSvgSymbolMarkup(fileName, x, y, symbolSizeRendered, rotationDegrees);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error building symbol SVG {symbol.ImageId}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Builds SVG markup for an SVG symbol as a nested SVG element with proper positioning, scaling, and rotation.
        /// </summary>
        private string? BuildSvgSymbolMarkup(string fileName, float x, float y, float symbolSizeRendered, double rotationDegrees)
        {
            try
            {
                // Read SVG content
                var svgContent = File.ReadAllText(fileName);

                // Parse SVG XML to get actual width/height attributes (not viewBox)
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(svgContent);
                var svgElement = xmlDoc.DocumentElement;
                if (svgElement == null || svgElement.Name != "svg")
                    return null;

                // Get the viewBox attribute from the original SVG
                var viewBoxAttr = svgElement.GetAttribute("viewBox");

                // If no viewBox, load SVG to get bounds for constructing viewBox
                if (string.IsNullOrEmpty(viewBoxAttr))
                {
                    var svgForBounds = new SKSvg();
                    svgForBounds.Load(fileName);

                    if (svgForBounds.Picture == null)
                        return null;

                    var bounds = svgForBounds.Picture.CullRect;
                    viewBoxAttr = $"0 0 {bounds.Width} {bounds.Height}";
                }

                // Build a nested SVG with the original viewBox
                // The nested SVG will have its own coordinate system (viewBox), and we scale it to fit
                var nestedSvgWidth = symbolSizeRendered;
                var nestedSvgHeight = symbolSizeRendered;

                // Extract inner content
                var innerContent = new System.Text.StringBuilder();
                foreach (XmlNode child in svgElement.ChildNodes)
                {
                    innerContent.Append(child.OuterXml);
                }

                var nestedSvg = $"<svg x=\"{x}\" y=\"{y}\" width=\"{nestedSvgWidth}\" height=\"{nestedSvgHeight}\" viewBox=\"{viewBoxAttr}\" preserveAspectRatio=\"xMidYMid meet\">{innerContent}</svg>";

                // Apply rotation if needed by wrapping in a group
                if (rotationDegrees != 0)
                {
                    var rotCenterX = x + symbolSizeRendered / 2.0;
                    var rotCenterY = y + symbolSizeRendered / 2.0;
                    var rotationTransform = $"translate({rotCenterX}, {rotCenterY}) rotate({rotationDegrees}) translate({-rotCenterX}, {-rotCenterY})";
                    return $"  <g transform=\"{rotationTransform}\">{nestedSvg}</g>";
                }

                return $"  {nestedSvg}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error building SVG symbol markup {fileName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Builds SVG markup for a PNG symbol as an embedded image with proper positioning, scaling, and rotation.
        /// </summary>
        private string? BuildPngSymbolMarkup(string fileName, float x, float y, float symbolSizeRendered, double rotationDegrees)
        {
            try
            {
                // Read PNG file and convert to base64 data URI
                var imageBytes = File.ReadAllBytes(fileName);
                var base64 = Convert.ToBase64String(imageBytes);
                var dataUri = $"data:image/png;base64,{base64}";

                // Get image dimensions for uniform scaling
                using (var data = SKData.Create(fileName))
                {
                    var image = SKImage.FromEncodedData(data);
                    if (image == null)
                        return null;

                    var imageWidth = (float)image.Width;
                    var imageHeight = (float)image.Height;

                    // Calculate uniform scale to fit within symbolSizeRendered (matching Stretch="Uniform" behavior)
                    var scaleX = symbolSizeRendered / imageWidth;
                    var scaleY = symbolSizeRendered / imageHeight;
                    var uniformScale = Math.Min(scaleX, scaleY);

                    var scaledWidth = imageWidth * uniformScale;
                    var scaledHeight = imageHeight * uniformScale;

                    // Center the scaled image within the symbol area
                    var imageX = x + (symbolSizeRendered - scaledWidth) / 2.0f;
                    var imageY = y + (symbolSizeRendered - scaledHeight) / 2.0f;

                    // Center for rotation
                    var imageCenterX = imageX + scaledWidth / 2.0;
                    var imageCenterY = imageY + scaledHeight / 2.0;

                    // Build image element with rotation if needed
                    if (rotationDegrees != 0)
                    {
                        var rotationTransform = $"translate({imageCenterX}, {imageCenterY}) rotate({rotationDegrees}) translate({-imageCenterX}, {-imageCenterY})";
                        return $"  <g transform=\"{rotationTransform}\"><image x=\"{imageX}\" y=\"{imageY}\" width=\"{scaledWidth}\" height=\"{scaledHeight}\" xlink:href=\"{dataUri}\"/></g>";
                    }
                    else
                    {
                        return $"  <image x=\"{imageX}\" y=\"{imageY}\" width=\"{scaledWidth}\" height=\"{scaledHeight}\" xlink:href=\"{dataUri}\"/>";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error building PNG symbol markup {fileName}: {ex.Message}");
                return null;
            }
        }

    }
}

