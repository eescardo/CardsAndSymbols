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
    /// Interaction logic for CardViewer.xaml
    /// </summary>
    public partial class CardViewer : UserControl
    {
        public static DependencyProperty CardDataProperty = DependencyProperty.Register(
            "CardData",
            typeof(CardData),
            typeof(CardViewer),
            new PropertyMetadata());

        public static DependencyProperty CardBaseSizeProperty = DependencyProperty.Register(
            "CardBaseSize",
            typeof(double),
            typeof(CardViewer),
            new PropertyMetadata(100.0, (o, a) => ((CardViewer)o).CardBaseSizeChangedCallback(a)));

        public static DependencyProperty CardScaleFactorProperty = DependencyProperty.Register(
            "CardScaleFactor",
            typeof(double),
            typeof(CardViewer),
            new PropertyMetadata(1.0, (o, a) => ((CardViewer)o).CardScaleFactorChangedCallback(a)));

        public CardViewer()
        {
            this.InitializeComponent();
            this.DataContextChanged += HandleDataContextChanged;
        }

        public CardData CardData
        {
            get
            {
                return (CardData)this.GetValue(CardDataProperty);
            }

            set
            {
                this.SetValue(CardDataProperty, value);
            }
        }

        public double CardBaseSize
        {
            get
            {
                return (double)this.GetValue(CardBaseSizeProperty);
            }

            set
            {
                this.SetValue(CardBaseSizeProperty, value);
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

        private void AdjustDimensions()
        {
            this.Width = this.Height = this.CardBaseSize * this.CardScaleFactor;
        }

        private void CardScaleFactorChangedCallback(DependencyPropertyChangedEventArgs e)
        {
            this.AdjustDimensions();
        }

        private void CardBaseSizeChangedCallback(DependencyPropertyChangedEventArgs e)
        {
            this.AdjustDimensions();
        }

        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.AdjustDimensions();
        }
    }
}
