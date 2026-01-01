using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace CardsAndSymbols
{
    /// <summary>
    /// Interaction logic for Symbol.xaml
    /// </summary>
    public partial class SymbolViewer : UserControl
    {
        private const double DragMovementThreshold = 4.0;

        public static readonly StyledProperty<SymbolData?> SymbolDataProperty = AvaloniaProperty.Register<SymbolViewer, SymbolData?>(
            "SymbolData");

        public static readonly StyledProperty<double> CardScaleFactorProperty = AvaloniaProperty.Register<SymbolViewer, double>(
            "CardScaleFactor", 1.0);

        public static readonly StyledProperty<double> ScaledOffsetXProperty = AvaloniaProperty.Register<SymbolViewer, double>(
            "ScaledOffsetX", 0.0);

        public static readonly StyledProperty<double> ScaledOffsetYProperty = AvaloniaProperty.Register<SymbolViewer, double>(
            "ScaledOffsetY", 0.0);

        private Point? capturePoint = null;
        private double anchorX = 0.0;
        private double anchorY = 0.0;
        private bool triggerSizeChange = false;
        private bool shouldDrag = false;

        public SymbolViewer()
        {
            this.InitializeComponent();
            // Don't set DataContext here - let the DataTemplate set it to the SymbolData item
            // We'll set SymbolData property in OnDataContextChanged
            
            // Set ImageCache on the converter from App resources
            if (this.Resources != null && this.Resources.TryGetResource("ImageSourceConverter", Avalonia.Styling.ThemeVariant.Default, out var converterObj))
            {
                if (converterObj is FileIdToImageConverter converter)
                {
                    var app = (App)Application.Current!;
                    if (app?.Resources != null && app.Resources.TryGetResource("ImageCache", Avalonia.Styling.ThemeVariant.Default, out var cacheObj) && cacheObj is ImageCache cache)
                    {
                        converter.ImageCache = cache;
                    }
                }
            }
        }
        
        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            // When DataContext is set to SymbolData by DataTemplate, also set SymbolData property
            if (this.DataContext is SymbolData symbolData)
            {
                this.SymbolData = symbolData;
            }
        }
        
        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            // Update size based on symbol scale
            this.UpdateSize();
            // Position ourselves when loaded - the container should be available now
            this.AdjustOffsets();
        }
        
        private void UpdateSize()
        {
            // Scale the SymbolViewer itself based on the symbol's size
            // The SymbolViewer needs to be the full size (before ItemsControl scale)
            // so that when the ItemsControl scales it, there's no clipping
            var symbolScale = this.SymbolData?.Size.ToScale() ?? 1.0;
            var scaledSize = Constants.BaseSymbolSize * symbolScale;
            
            // Scale the SymbolViewer itself, not the Image within it
            this.Width = scaledSize;
            this.Height = scaledSize;
        }

        public SymbolData? SymbolData
        {
            get => this.GetValue(SymbolDataProperty);
            set => this.SetValue(SymbolDataProperty, value);
        }

        public double CardScaleFactor
        {
            get => this.GetValue(CardScaleFactorProperty);
            set => this.SetValue(CardScaleFactorProperty, value);
        }

        public double ScaledOffsetX
        {
            get => this.GetValue(ScaledOffsetXProperty);
            set => this.SetValue(ScaledOffsetXProperty, value);
        }

        public double ScaledOffsetY
        {
            get => this.GetValue(ScaledOffsetYProperty);
            set => this.SetValue(ScaledOffsetYProperty, value);
        }

        private Point? GetPositionFromParent(PointerEventArgs e)
        {
            var parent = this.FindAncestorOfType<CardViewer>();
            return (parent != null) ? e.GetPosition(parent) : (Point?)null;
        }

        private void AdjustOffsets()
        {
            if (this.SymbolData != null)
            {
                var cardViewer = this.FindAncestorOfType<CardViewer>();
                if (cardViewer != null && cardViewer.CardData != null)
                {
                    // Account for the scale transform on ItemsControl
                    var cardSize = cardViewer.CardBaseSize * cardViewer.CardScaleFactor * Constants.SymbolItemsControlScaleFactor;
                    var columns = cardViewer.CardData.Columns;
                    var rows = (int)Math.Ceiling((double)cardViewer.CardData.Symbols!.Count / columns);
                    
                    var cellWidth = cardSize / columns;
                    var cellHeight = cardSize / rows;
                    
                    // Find index of this symbol
                    int index = 0;
                    foreach (var symbol in cardViewer.CardData.Symbols)
                    {
                        if (symbol == this.SymbolData) break;
                        index++;
                    }
                    
                    var row = index / columns;
                    var col = index % columns;
                    
                    // Center position in grid cell, then apply offset
                    var baseX = (col + 0.5) * cellWidth;
                    var baseY = (row + 0.5) * cellHeight;
                    
                    // Find the ContentPresenter container that wraps this SymbolViewer
                    var container = this.GetVisualParent() as ContentPresenter;
                    if (container != null)
                    {
                        // Use actual size for positioning
                        var symbolSize = this.Width > 0 && !double.IsNaN(this.Width) 
                            ? this.Width 
                            : Constants.BaseSymbolSize * this.SymbolData.Size.ToScale();
                        Canvas.SetLeft(container, baseX + this.SymbolData.OffsetX * cardViewer.CardScaleFactor * Constants.SymbolItemsControlScaleFactor - symbolSize / 2);
                        Canvas.SetTop(container, baseY + this.SymbolData.OffsetY * cardViewer.CardScaleFactor * Constants.SymbolItemsControlScaleFactor - symbolSize / 2);
                    }
                }
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == SymbolDataProperty)
            {
                if (change.OldValue is SymbolData oldData)
                {
                    oldData.PropertyChanged -= this.HandleSymbolDataPropertyChanged;
                }
                if (change.NewValue is SymbolData newData)
                {
                    newData.PropertyChanged += this.HandleSymbolDataPropertyChanged;
                    this.UpdateSize();
                    this.AdjustOffsets();
                }
            }
            else if (change.Property == CardScaleFactorProperty)
            {
                this.AdjustOffsets();
            }
        }

        private void HandleSymbolDataPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if ((e.PropertyName == nameof(SymbolData.OffsetX)) || (e.PropertyName == nameof(SymbolData.OffsetY)))
            {
                this.AdjustOffsets();
            }
            else if (e.PropertyName == nameof(SymbolData.Size))
            {
                this.UpdateSize();
                this.AdjustOffsets();
            }
        }



        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!this.IsEnabled) return;
            e.Handled = true;
            // Store initial position for drag/size change detection
            this.capturePoint = this.GetPositionFromParent(e);
            this.triggerSizeChange = true;
            this.shouldDrag = false;
            this.anchorX = this.SymbolData?.OffsetX ?? 0.0;
            this.anchorY = this.SymbolData?.OffsetY ?? 0.0;
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (!this.IsEnabled) return;
            e.Handled = true;
            
            if (this.triggerSizeChange && this.SymbolData != null)
            {
                this.SymbolData.Size = this.SymbolData.Size.NextSize();
            }
            
            this.capturePoint = null;
            this.triggerSizeChange = false;
            this.shouldDrag = false;
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (this.capturePoint == null)
            {
                return;
            }

            var position = this.GetPositionFromParent(e);
            if (position == null)
            {
                return;
            }

            var pointDiff = position.Value - this.capturePoint.Value;

            if (this.shouldDrag)
            {
                if (this.SymbolData != null)
                {
                    // Account for the scale transform on ItemsControl
                    this.SymbolData.OffsetX = this.anchorX + (pointDiff.X / (this.CardScaleFactor * Constants.SymbolItemsControlScaleFactor));
                    this.SymbolData.OffsetY = this.anchorY + (pointDiff.Y / (this.CardScaleFactor * Constants.SymbolItemsControlScaleFactor));
                }
            }
            else if (Math.Sqrt(pointDiff.X * pointDiff.X + pointDiff.Y * pointDiff.Y) >= DragMovementThreshold)
            {
                this.triggerSizeChange = false;
                this.shouldDrag = true;
            }
        }

    }
}
