using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace CardsAndSymbols
{
    /// <summary>
    /// Interaction logic for CardViewer.xaml
    /// </summary>
    public partial class CardViewer : UserControl
    {
        public static readonly StyledProperty<CardData?> CardDataProperty = AvaloniaProperty.Register<CardViewer, CardData?>(
            "CardData", defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        public static readonly StyledProperty<double> CardBaseSizeProperty = AvaloniaProperty.Register<CardViewer, double>(
            "CardBaseSize", 100.0);

        public static readonly StyledProperty<double> CardScaleFactorProperty = AvaloniaProperty.Register<CardViewer, double>(
            "CardScaleFactor", 1.0);

        public CardViewer()
        {
            this.InitializeComponent();
            // Set the scale transform using the constant
            var symbolsControl = this.FindControl<ItemsControl>("SymbolsItemsControl");
            // Initial sizing will be done when properties are set
            // But ensure we adjust dimensions once loaded
            this.Loaded += (s, e) => AdjustDimensions();
        }

        public CardData? CardData
        {
            get => this.GetValue(CardDataProperty);
            set => this.SetValue(CardDataProperty, value);
        }

        public double CardBaseSize
        {
            get => this.GetValue(CardBaseSizeProperty);
            set => this.SetValue(CardBaseSizeProperty, value);
        }

        public double CardScaleFactor
        {
            get => this.GetValue(CardScaleFactorProperty);
            set => this.SetValue(CardScaleFactorProperty, value);
        }

        private void AdjustDimensions()
        {
            var size = this.CardBaseSize * this.CardScaleFactor;
            if (size > 0)
            {
                this.Width = size;
                this.Height = size;
            }
            // SymbolViewers will reposition themselves via AdjustOffsets() when CardScaleFactor changes
        }


        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == CardDataProperty)
            {
                // When CardData is set, also set DataContext so bindings work
                if (change.NewValue is CardData cardData)
                {
                    this.DataContext = cardData;
                }
                this.AdjustDimensions();
                // Compute positions after dimensions are adjusted, but only if positions haven't been set yet
                if (change.NewValue is CardData cardDataAfter)
                {
                    // Check if positions are already initialized
                    bool positionsInitialized = cardDataAfter?.Symbols != null &&
                        cardDataAfter.Symbols.Any(s => s.PositionsInitialized);

                    if (!positionsInitialized && cardDataAfter != null)
                    {
                        // Defer position calculation to ensure dimensions are set
                        var cardDataForClosure = cardDataAfter; // Capture for closure
                        this.Loaded += (s, e) =>
                        {
                            if (this.CardData == cardDataForClosure && cardDataForClosure != null)
                            {
                                this.ComputeSymbolPositions(cardDataForClosure);
                            }
                        };
                        // Also try immediately in case dimensions are already set
                        if (this.Width > 0 && !double.IsNaN(this.Width))
                        {
                            this.ComputeSymbolPositions(cardDataAfter);
                        }
                    }
                }
            }
            else if (change.Property == CardScaleFactorProperty || 
                     change.Property == CardBaseSizeProperty)
            {
                this.AdjustDimensions();
            }
        }

        /// <summary>
        /// Computes random positions for symbols across the entire card and sets their OffsetX/OffsetY values.
        /// Offsets are relative to card center and stored in base units.
        /// </summary>
        private void ComputeSymbolPositions(CardData cardData)
        {
            if (cardData?.Symbols == null || cardData.Symbols.Count == 0) return;

            // Use actual rendered dimensions if available, otherwise use CardBaseSize * CardScaleFactor
            var cardSize = (this.Width > 0 && !double.IsNaN(this.Width))
                ? this.Width
                : this.CardBaseSize * this.CardScaleFactor;


            var cardCenterX = cardSize / 2.0;
            var cardCenterY = cardSize / 2.0;
            var cardRadius = cardSize / 2.0;

            // Calculate symbol radii for overlap detection using actual rendered size.
            var symbolRadii = new List<double>();
            foreach (var symbol in cardData.Symbols)
            {
                var symbolScale = symbol.Size.ToScale();
                // Symbol size in base units
                var symbolSizeBase = Constants.BaseSymbolSize * symbolScale;
                // Actual rendered size (scaled by CardScaleFactor)
                var symbolSizeRendered = symbolSizeBase * this.CardScaleFactor;
                var radius = symbolSizeRendered / 2.0;
                symbolRadii.Add(radius);
            }

            // Find the largest symbol radius to ensure all symbols fit within the circle
            var maxSymbolRadius = symbolRadii.Count > 0 ? symbolRadii.Max() : (Constants.BaseSymbolSize * this.CardScaleFactor / 2.0);

            // Maximum radius for symbol centers - ensures symbol edges stay within card circle
            var maxCenterRadius = Math.Max(0, cardRadius - maxSymbolRadius - 5.0);

            // Randomize positions across the entire card area
            var random = new Random();
            const int maxAttempts = 50;
            const double minDistancePadding = 8.0;

            int index = 0;
            foreach (var symbol in cardData.Symbols)
            {
                var symbolRadius = symbolRadii[index];
                bool placed = false;
                int attempts = 0;

                while (!placed && attempts < maxAttempts)
                {
                    attempts++;

                    // Generate random position uniformly distributed within the card circle
                    // Use sqrt of random for uniform distribution in circular area
                    var angle = random.NextDouble() * 2.0 * Math.PI;
                    var radius = Math.Sqrt(random.NextDouble()) * maxCenterRadius;

                    var finalX = cardCenterX + radius * Math.Cos(angle);
                    var finalY = cardCenterY + radius * Math.Sin(angle);

                    // Verify position is within circular boundary (accounting for symbol radius)
                    var distanceFromCenter = Math.Sqrt((finalX - cardCenterX) * (finalX - cardCenterX) + (finalY - cardCenterY) * (finalY - cardCenterY));
                    if (distanceFromCenter + symbolRadius > cardRadius)
                    {
                        continue; // Outside circle, try again
                    }

                    // Check for overlaps with previously placed symbols
                    bool overlaps = false;
                    for (int j = 0; j < index; j++)
                    {
                        var otherSymbol = cardData.Symbols.ElementAt(j);
                        var otherRadius = symbolRadii[j];

                        // Get other symbol's final position (offsets are relative to card center)
                        var otherOffsetX = otherSymbol.OffsetX * this.CardScaleFactor;
                        var otherOffsetY = otherSymbol.OffsetY * this.CardScaleFactor;
                        var otherFinalX = cardCenterX + otherOffsetX;
                        var otherFinalY = cardCenterY + otherOffsetY;

                        var dx = finalX - otherFinalX;
                        var dy = finalY - otherFinalY;
                        var distance = Math.Sqrt(dx * dx + dy * dy);
                        var minDistance = symbolRadius + otherRadius + minDistancePadding;

                        if (distance < minDistance)
                        {
                            overlaps = true;
                            break;
                        }
                    }

                    if (!overlaps)
                    {
                        // Store offset relative to card center, in base units
                        // These will be scaled by CardScaleFactor when applied in SymbolViewer
                        var offsetX = finalX - cardCenterX;
                        var offsetY = finalY - cardCenterY;
                        symbol.OffsetX = offsetX / this.CardScaleFactor;
                        symbol.OffsetY = offsetY / this.CardScaleFactor;
                        placed = true;
                    }
                }

                // If we couldn't place it after max attempts, place at a random position near center
                // This is a fallback - should rarely happen
                if (!placed)
                {
                    System.Console.WriteLine($"Failed to place symbol {symbol.ImageId} after {attempts} attempts");
                    var angle = random.NextDouble() * 2.0 * Math.PI;
                    var radius = random.NextDouble() * maxCenterRadius * 0.7; // Reduced radius as fallback
                    var fallbackX = cardCenterX + radius * Math.Cos(angle);
                    var fallbackY = cardCenterY + radius * Math.Sin(angle);
                    symbol.OffsetX = (fallbackX - cardCenterX) / this.CardScaleFactor;
                    symbol.OffsetY = (fallbackY - cardCenterY) / this.CardScaleFactor;
                }

                index++;
            }
        }
    }
}
