using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CardsAndSymbols
{
    /// <summary>
    /// Interaction logic for Symbol.xaml
    /// </summary>
    public partial class SymbolViewer : UserControl
    {
        private const double DragMovementThreshold = 4.0;

        public static DependencyProperty SymbolDataProperty = DependencyProperty.Register(
            "SymbolData",
            typeof(SymbolData),
            typeof(SymbolViewer),
            new PropertyMetadata(null, (o,a) => ((SymbolViewer)o).HandleSymbolDataChanged(a)));

        public static DependencyProperty CardScaleFactorProperty = DependencyProperty.Register(
            "CardScaleFactor",
            typeof(double),
            typeof(SymbolViewer),
            new PropertyMetadata(1.0, (o,a) => ((SymbolViewer)o).HandleCardScaleFactorChanged(a)));

        public static DependencyProperty ScaledOffsetXProperty = DependencyProperty.Register(
            "ScaledOffsetX",
            typeof(double),
            typeof(SymbolViewer),
            new PropertyMetadata(0.0));

        public static DependencyProperty ScaledOffsetYProperty = DependencyProperty.Register(
            "ScaledOffsetY",
            typeof(double),
            typeof(SymbolViewer),
            new PropertyMetadata(0.0));

        private Point? capturePoint = null;
        private double anchorX = 0.0;
        private double anchorY = 0.0;
        private bool triggerSizeChange = false;
        private bool shouldDrag = false;

        public SymbolViewer()
        {
            this.InitializeComponent();
            this.GotMouseCapture += HandleGotMouseCapture;
            this.LostMouseCapture += HandleLostMouseCapture;
        }

        public SymbolData SymbolData
        {
            get
            {
                return (SymbolData)this.GetValue(SymbolDataProperty);
            }

            set
            {
                this.SetValue(SymbolDataProperty, value);
            }
        }

        public double CardScaleFactor
        {
            get
            {
                return (double)this.GetValue(CardScaleFactorProperty);
            }

            set
            {
                this.SetValue(CardScaleFactorProperty, value);
            }
        }

        public double ScaledOffsetX
        {
            get
            {
                return (double)this.GetValue(ScaledOffsetXProperty);
            }

            set
            {
                this.SetValue(ScaledOffsetXProperty, value);
            }
        }

        public double ScaledOffsetY
        {
            get
            {
                return (double)this.GetValue(ScaledOffsetYProperty);
            }

            set
            {
                this.SetValue(ScaledOffsetYProperty, value);
            }
        }

        private Point? GetPositionFromParent(MouseEventArgs e)
        {
            var parent = this.FindParent<CardViewer>();
            return (parent != null) ? e.GetPosition(parent) : (Point?)null;
        }

        private void AdjustOffsets()
        {
            this.ScaledOffsetX = this.SymbolData.OffsetX * this.CardScaleFactor;
            this.ScaledOffsetY = this.SymbolData.OffsetY * this.CardScaleFactor;
        }

        private void HandleSymbolDataChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                ((SymbolData)e.OldValue).PropertyChanged -= this.HandleSymbolDataPropertyChanged;
            }

            if (e.NewValue != null)
            {
                ((SymbolData)e.NewValue).PropertyChanged += this.HandleSymbolDataPropertyChanged;
            }
        }

        private void HandleSymbolDataPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((e.PropertyName == "OffsetX") || (e.PropertyName == "OffsetY"))
            {
                this.AdjustOffsets();
            }
        }

        private void HandleCardScaleFactorChanged(DependencyPropertyChangedEventArgs e)
        {
            this.AdjustOffsets();
        }

        private void HandleMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!this.IsEnabled)
            {
                return;
            }

            e.Handled = true;

            var el = (UIElement)sender;
            if (el.IsMouseCaptured)
            {
                el.ReleaseMouseCapture();
            }
        }

        private void HandleMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!this.IsEnabled)
            {
                return;
            }

            e.Handled = true;
            var el = (UIElement)sender;
            if (!el.CaptureMouse())
            {
                throw new InvalidOperationException();
            }
        }

        private void HandleMouseMove(object sender, MouseEventArgs e)
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

            var pointDiff = Point.Subtract(position.Value, this.capturePoint.Value);

            if (this.shouldDrag)
            {
                if (this.SymbolData != null)
                {
                    var parent = this.FindParent<CardViewer>();
                    var source = PresentationSource.FromVisual(parent);
                    pointDiff = source.CompositionTarget.TransformToDevice.Transform(pointDiff);
                    this.SymbolData.OffsetX = this.anchorX + (pointDiff.X / this.CardScaleFactor);
                    this.SymbolData.OffsetY = this.anchorY + (pointDiff.Y / this.CardScaleFactor);
                }
            }
            else if (pointDiff.Length >= DragMovementThreshold)
            {
                this.triggerSizeChange = false;
                this.shouldDrag = true;
            }
        }

        private void HandleGotMouseCapture(object sender, MouseEventArgs e)
        {
            this.capturePoint = this.GetPositionFromParent(e);
            this.triggerSizeChange = true;
            this.shouldDrag = false;
            this.anchorX = this.SymbolData != null ? this.SymbolData.OffsetX : 0.0;
            this.anchorY = this.SymbolData != null ? this.SymbolData.OffsetY : 0.0;
        }

        private void HandleLostMouseCapture(object sender, MouseEventArgs e)
        {
            if (this.triggerSizeChange && this.SymbolData != null)
            {
                this.SymbolData.Size = this.SymbolData.Size.NextSize();
            }

            this.capturePoint = null;
            this.triggerSizeChange = false;
            this.shouldDrag = false;
            this.anchorX = 0.0;
            this.anchorY = 0.0;
        }
    }
}
