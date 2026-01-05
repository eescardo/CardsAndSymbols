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
        private bool triggerRotationChange = false;
        private bool shouldDrag = false;
        private bool isRightButtonPressed = false;

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
            // Scale the SymbolViewer itself based on the symbol's size and card scale factor
            // The SymbolViewer needs to be the full size (before ItemsControl scale)
            // so that when the ItemsControl scales it, there's no clipping
            var symbolScale = this.SymbolData?.Size.ToScale() ?? 1.0;
            var scaledSize = Constants.BaseSymbolSize * symbolScale * this.CardScaleFactor;

            // Scale the SymbolViewer itself, not the Image within it
            this.Width = scaledSize;
            this.Height = scaledSize;

            // Set Canvas size directly in code to avoid binding timing issues
            // Also set RenderTransformOrigin in code AFTER size is set, so it's calculated correctly
            var canvas = this.FindControl<Canvas>("SymbolCanvas");
            if (canvas != null)
            {
                canvas.Width = scaledSize;
                canvas.Height = scaledSize;
                // Set transform origin AFTER size is set, so it's calculated from correct bounds
                canvas.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            }
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
                if (cardViewer != null)
                {
                    // Account for the scale transform on ItemsControl
                    var cardSize = cardViewer.CardBaseSize * cardViewer.CardScaleFactor;

                    // Find the ContentPresenter container that wraps this SymbolViewer
                    var container = this.GetVisualParent() as ContentPresenter;
                    if (container != null)
                    {
                        // Use actual size for positioning
                        var symbolSize = this.Width > 0 && !double.IsNaN(this.Width) 
                            ? this.Width 
                            : Constants.BaseSymbolSize * this.SymbolData.Size.ToScale();

                        // Position based on OffsetX/OffsetY (which are in base units, scaled by CardScaleFactor)
                        // OffsetX/OffsetY are relative to card center (0,0)
                        var centerX = cardSize / 2.0;
                        var centerY = cardSize / 2.0;
                        var offsetX = this.SymbolData.OffsetX * cardViewer.CardScaleFactor;
                        var offsetY = this.SymbolData.OffsetY * cardViewer.CardScaleFactor;

                        var left = centerX + offsetX - symbolSize / 2;
                        var top = centerY + offsetY - symbolSize / 2;

                        Canvas.SetLeft(container, left);
                        Canvas.SetTop(container, top);
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
                this.UpdateSize();
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
                // Force immediate visual update
                this.InvalidateVisual();
                this.InvalidateArrange();
            }
            else if (e.PropertyName == nameof(SymbolData.RotationDegrees))
            {
                // Rotation is handled by XAML binding, just invalidate visual to ensure update
                this.InvalidateVisual();
            }
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!this.IsEnabled) return;
            e.Handled = true;

            // Track which button was pressed
            var point = e.GetCurrentPoint(this);
            this.isRightButtonPressed = point.Properties.IsRightButtonPressed;

            // Store initial position for drag/size change detection
            this.capturePoint = this.GetPositionFromParent(e);
            this.triggerSizeChange = !this.isRightButtonPressed; // Only trigger size change for left button
            this.triggerRotationChange = this.isRightButtonPressed; // Only trigger rotation change for right button
            this.shouldDrag = false;
            this.anchorX = this.SymbolData?.OffsetX ?? 0.0;
            this.anchorY = this.SymbolData?.OffsetY ?? 0.0;
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (!this.IsEnabled) return;
            e.Handled = true;

            // Check which button was actually released
            var point = e.GetCurrentPoint(this);
            var isRightButton = point.Properties.IsRightButtonPressed || 
                               (e.InitialPressMouseButton == MouseButton.Right);

            if (this.SymbolData != null && !this.shouldDrag)
            {
                // Only trigger size/rotation change if we didn't drag
                if (this.triggerRotationChange && (this.isRightButtonPressed || isRightButton))
                {
                    this.SymbolData.RotationStation = this.SymbolData.RotationStation.NextRotationStation();
                }
                else if (this.triggerSizeChange && !this.isRightButtonPressed && !isRightButton)
                {
                    this.SymbolData.Size = this.SymbolData.Size.NextSize();
                }
            }

            this.capturePoint = null;
            this.triggerSizeChange = false;
            this.triggerRotationChange = false;
            this.shouldDrag = false;
            this.isRightButtonPressed = false;
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
                    this.SymbolData.OffsetX = this.anchorX + (pointDiff.X / this.CardScaleFactor);
                    this.SymbolData.OffsetY = this.anchorY + (pointDiff.Y / this.CardScaleFactor);
                }
            }
            else if (Math.Sqrt(pointDiff.X * pointDiff.X + pointDiff.Y * pointDiff.Y) >= DragMovementThreshold)
            {
                // Cancel size/rotation change triggers when dragging starts
                // Only cancel the one that was active based on which button was pressed
                if (this.isRightButtonPressed)
                {
                    this.triggerRotationChange = false;
                }
                else
                {
                    this.triggerSizeChange = false;
                }
                this.shouldDrag = true;
            }
        }

    }
}
