using System;
using System.Collections.Generic;
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
            new PropertyMetadata());

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

        private Point? GetPositionFromParent(MouseEventArgs e)
        {
            var parent = this.FindParent<CardViewer>();
            return (parent != null) ? e.GetPosition(parent) : (Point?)null;
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
                    this.SymbolData.OffsetX = this.anchorX + pointDiff.X;
                    this.SymbolData.OffsetY = this.anchorY + pointDiff.Y;
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
