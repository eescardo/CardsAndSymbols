
namespace CardsAndSymbols
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Windows;

    using ProjectivePlane;
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int DefaultNumCards = 13;

        public static DependencyProperty CardsProperty = DependencyProperty.Register(
            "Cards",
            typeof(ICollection<CardData>),
            typeof(MainWindow));

        public MainWindow()
        {
            this.InitializeComponent();

            this.DataContext = this;
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

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            this.ComputeCards("Images", DefaultNumCards);
        }

        private void ComputeCards(string symbolDir, int numCards)
        {
            // Construct a projective plane and map points to cards and lines to symbols
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

            return dirInfo.EnumerateFiles().Select(f => new SymbolData { ImageFile = f.FullName }).ToList();
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
    }
}
