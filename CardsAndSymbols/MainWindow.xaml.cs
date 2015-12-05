
namespace CardsAndSymbols
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;

    using ProjectivePlane;
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int ControlsColumnIndex = 0;
        private const int CardsColumnIndex = 1;

        private const int DefaultNumCards = 13;
        private const int DefaultNumCardColumns = 3;
        private const double DefaultCardSize = 256.0;
        private const double DefaultCardMargin = 5.0;

        private const string DefaultImageDirectory = "Images\\Png";

        public static DependencyProperty CardsProperty = DependencyProperty.Register(
            "Cards",
            typeof(ICollection<CardData>),
            typeof(MainWindow));

        public static DependencyProperty NewNumCardsProperty = DependencyProperty.Register(
            "NewNumCards",
            typeof(int),
            typeof(MainWindow),
            new PropertyMetadata(DefaultNumCards));

        public static DependencyProperty NumCardColumnsProperty = DependencyProperty.Register(
            "NumCardColumns",
            typeof(int),
            typeof(MainWindow),
            new PropertyMetadata(DefaultNumCardColumns));

        public static DependencyProperty CardBaseSizeProperty = DependencyProperty.Register(
            "CardBaseSize",
            typeof(double),
            typeof(MainWindow),
            new PropertyMetadata(DefaultCardSize));

        public static DependencyProperty CardScaleFactorProperty = DependencyProperty.Register(
            "CardScaleFactor",
            typeof(double),
            typeof(MainWindow),
            new PropertyMetadata(1.0, (o, a) => ((MainWindow)o).CardScaleFactorChangedCallback(a)));

        public static DependencyProperty CardMarginProperty = DependencyProperty.Register(
            "CardMargin",
            typeof(double),
            typeof(MainWindow),
            new PropertyMetadata(DefaultCardMargin));

        public static DependencyProperty ImageDirectoryProperty = DependencyProperty.Register(
            "ImageDirectory",
            typeof(string),
            typeof(MainWindow),
            new PropertyMetadata(DefaultImageDirectory));

        public static DependencyProperty ImageCacheProperty = DependencyProperty.Register(
            "ImageCache",
            typeof(ImageCache),
            typeof(MainWindow),
            new PropertyMetadata(null));

        public MainWindow()
        {
            this.InitializeComponent();

            this.DataContext = this;
            this.ImageCache = (ImageCache)this.FindResource("ImageCache");
        }

        public ICollection<CardData> Cards
        {
            get
            {
                return (ICollection<CardData>)this.GetValue(CardsProperty);
            }

            set
            {
                this.SetValue(CardsProperty, value);
            }
        }

        public int NewNumCards
        {
            get
            {
                return (int)this.GetValue(NewNumCardsProperty);
            }

            set
            {
                this.SetValue(NewNumCardsProperty, value);
            }
        }

        public int NumCardColumns
        {
            get
            {
                return (int)this.GetValue(NumCardColumnsProperty);
            }

            set
            {
                this.SetValue(NumCardColumnsProperty, value);
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

        public double CardMargin
        {
            get
            {
                return (double)this.GetValue(CardMarginProperty);
            }

            set
            {
                this.SetValue(CardMarginProperty, value);
            }
        }

        public string ImageDirectory
        {
            get
            {
                return (string)this.GetValue(ImageDirectoryProperty);
            }

            set
            {
                this.SetValue(ImageDirectoryProperty, value);
            }
        }

        public ImageCache ImageCache
        {
            get
            {
                return (ImageCache)this.GetValue(ImageCacheProperty);
            }

            set
            {
                this.SetValue(ImageCacheProperty, value);
            }
        }

        private void ComputeCards(string symbolDir, int numCards)
        {
            // Construct a projective plane and map points to cards and lines to symbols
            this.ImageCache.Clear(ImageCacheFlags.ClearIds);
            var symbols = this.GetSymbols(symbolDir);
            var planeConstructor = new ProjectivePlaneConstructor<SymbolData>(symbols, numCards);
            var planePoints = planeConstructor.PlanePoints;
            this.Cards = planePoints.Select(point => new CardData(point.Lines)).ToList();
        }

        private IList<SymbolData> GetSymbols(string symbolDir)
        {
            var dirInfo = new DirectoryInfo(symbolDir);
            if (!dirInfo.Exists)
            {
                throw new ArgumentException(string.Format("Invalid symbol directory '{0}' specified", symbolDir), "symbolDir");
            }

            return dirInfo.EnumerateFiles().Select(f => new SymbolData { ImageId = this.ImageCache.AssignNewId(f.FullName) }).ToList();
        }

        private void ComputeColumns(double cardAreaWidth, double cardSize, double cardMargin)
        {
            if (cardSize <= 0 || cardAreaWidth <= 0)
            {
                return;
            }

            var idealColumns = cardAreaWidth / (cardSize + (4 * cardMargin));
            this.NumCardColumns = Math.Max(1, (int)Math.Floor(idealColumns));
        }

        ////////////////////////////////////////////////////////////////
        // Event callbacks

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            this.ComputeCards(this.ImageDirectory, NewNumCards);
        }

        private void WindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.ComputeColumns(this.CardGrid.ColumnDefinitions[CardsColumnIndex].ActualWidth, this.CardBaseSize * this.CardScaleFactor, this.CardMargin);
        }

        private void CardScaleFactorChangedCallback(DependencyPropertyChangedEventArgs e)
        {
            this.ComputeColumns(this.CardGrid.ColumnDefinitions[CardsColumnIndex].ActualWidth, this.CardBaseSize * ((double)e.NewValue), this.CardMargin);
        }

        private void HandleNewClick(object sender, RoutedEventArgs e)
        {
            this.ComputeCards(this.ImageDirectory, NewNumCards);
        }

        private void HandleSaveClick(object sender, RoutedEventArgs e)
        {
            var json = JsonConvert.SerializeObject(this.Cards, Formatting.Indented);
            using (var writer = new StreamWriter("cards.json"))
            {
                writer.Write(json);
            }
        }

        private void HandleLoadClick(object sender, RoutedEventArgs e)
        {
            using (var reader = new StreamReader("cards.json"))
            {
                var json = reader.ReadToEnd();
                var cards = JsonConvert.DeserializeObject<List<CardData>>(json);
                this.Cards = cards;
            }
        }

        private void HandlePrintClick(object sender, RoutedEventArgs e)
        {
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintVisual(this.CardContainer, "Card printout");
            }
        }
    }
}
