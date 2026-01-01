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
            if (symbolsControl != null)
            {
                symbolsControl.RenderTransform = new ScaleTransform(
                    Constants.SymbolItemsControlScaleFactor,
                    Constants.SymbolItemsControlScaleFactor);
            }
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
            }
            else if (change.Property == CardScaleFactorProperty || 
                     change.Property == CardBaseSizeProperty)
            {
                this.AdjustDimensions();
            }
        }
    }
}
