
namespace CardsAndSymbols
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Documents;
    using System.Windows.Markup;
    using System.Windows.Media;

    using ProjectivePlane;
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int ControlsColumnIndex = 0;
        private const int CardsColumnIndex = 1;

        private const int DefaultNumCards = 55;
        private const int DefaultNumCardColumns = 3;
        private const double DefaultCardSize = 256.0;
        private const double DefaultCardMargin = 5.0;
        private const double DefaultCardScaleFactor = 1.0;

        private const string DefaultImageDirectory = "Images\\Svg";

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
            new PropertyMetadata(DefaultCardScaleFactor, (o, a) => ((MainWindow)o).CardScaleFactorChangedCallback(a)));

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
            this.ComputeColumns(this.AppGrid.ColumnDefinitions[CardsColumnIndex].ActualWidth, this.CardBaseSize * this.CardScaleFactor, this.CardMargin);
        }

        private void CardScaleFactorChangedCallback(DependencyPropertyChangedEventArgs e)
        {
            this.ComputeColumns(this.AppGrid.ColumnDefinitions[CardsColumnIndex].ActualWidth, this.CardBaseSize * ((double)e.NewValue), this.CardMargin);
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
                var cardGrid = this.CardContainer.FindChild<UniformGrid>("CardGrid");
                var printPageSize = new Size(printDialog.PrintableAreaWidth, printDialog.PrintableAreaHeight);
                ///////////////////////////////////////////
                // With custom paginator
                var paginator = new Paginator(cardGrid, printPageSize);
                printDialog.PrintDocument(paginator, "Card printout");

                ///////////////////////////////////////////
                // With fixed document (throws exception due to multi-parented visual)
                //var document = this.CreateFixedDocument(cardGrid, printPageSize);
                // printDialog.PrintDocument(document.DocumentPaginator, "Card printout");

                ///////////////////////////////////////////
                // With direct visual printing
                // printDialog.PrintVisual(cardGrid, "Card printout");
            }
        }

        FixedDocument CreateFixedDocument(UniformGrid control, Size printPageSize)
        {
            var document = new FixedDocument();
            var pageCount = 0;
            var pageOffset = 0.0;

            var elemCount = control.Children.Count;
            if (elemCount <= 0)
            {
                pageCount = 1;
            }
            else
            {
                var firstChild = control.Children[0] as FrameworkElement;
                if (firstChild == null)
                {
                    throw new InvalidOperationException("Can't determine size of control children for pagination");
                }

                var elemSize = firstChild.GetActualSize();
                var rows = elemCount / control.Columns;
                var rowRemainder = elemCount % control.Columns;
                var totalRows = rows + (rowRemainder > 0 ? 1 : 0);
                var rowsPerPage = (int)(printPageSize.Height / elemSize.Height);
                pageOffset = rowsPerPage * elemSize.Height;
                var pages = totalRows / rowsPerPage;
                var pageRemainder = totalRows % rowsPerPage;
                pageCount = pages + (pageRemainder > 0 ? 1 : 0);
            }

            for (int iPage = 0; iPage < pageCount; ++iPage)
            {
                FixedPage page = new FixedPage();
                page.Width = printPageSize.Width;
                page.Height = printPageSize.Height;
                var contentBox = new Rect(0.0, iPage * pageOffset, control.ActualWidth, pageOffset);
                page.Children.Add(control);
                page.ContentBox = contentBox;
                page.BleedBox = contentBox;
                PageContent pageContent = new PageContent();
                ((IAddChild)pageContent).AddChild(page);
                document.Pages.Add(pageContent);
            }

            return document;
        }

        private class Paginator : DocumentPaginator
        {
            private readonly UniformGrid control;
            private double pageOffset = 0.0;
            private Dictionary<int, DocumentPage> pages = new Dictionary<int, DocumentPage>();

            public Paginator(UniformGrid control, Size pageSize)
            {
                this.control = control;

                this._source = new PaginatorSource(this);
                this.pageSize = pageSize;

                this.ComputePageCount();
            }

            private bool isPageCountValid = false;
            public override bool IsPageCountValid
            {
                get { return this.isPageCountValid; }
            }

            private int pageCount = 0;
            public override int PageCount
            {
                get { return this.pageCount;  }
            }

            private Size pageSize;
            public override Size PageSize
            {
                get { return this.pageSize; }
                set { this.pageSize = value; }
            }

            private IDocumentPaginatorSource _source;
            public override IDocumentPaginatorSource Source
            {
                get { return this._source; }
            }

            public override DocumentPage GetPage(int pageNumber)
            {
                if (pageNumber >= this.PageCount)
                {
                    throw new ArgumentException("Invalid page number", "pageNumber");
                }

                DocumentPage documentPage;
                if (this.pages.TryGetValue(pageNumber, out documentPage))
                {
                    return documentPage;
                }

                var rect = new Rect(0.0, pageNumber * this.pageOffset, this.control.ActualWidth, this.pageOffset);
                //var pageContainer = new ContainerVisual();
                //pageContainer.Children.Add(this.control);
                //pageContainer.Transform = new TranslateTransform(0, -(pageNumber * this.pageOffset));
                documentPage = new DocumentPage(this.control, this.PageSize, rect, rect);
                this.pages[pageNumber] = documentPage;
                return documentPage;
            }

            public override void GetPageAsync(int pageNumber)
            {
                throw new NotImplementedException();
            }

            public override void GetPageAsync(int pageNumber, object userState)
            {
                throw new NotImplementedException();
            }

            public override void ComputePageCount()
            {
                this.pages.Clear();
                var elemCount = this.control.Children.Count;
                if (elemCount <= 0)
                {
                    this.pageCount = 1;
                    this.isPageCountValid = true;
                    return;
                }

                var firstChild = this.control.Children[0] as FrameworkElement;
                if (firstChild == null)
                {
                    throw new InvalidOperationException("Can't determine size of control children for pagination");
                }

                var elemSize = firstChild.GetActualSize();
                var rows = elemCount / this.control.Columns;
                var rowRemainder = elemCount % this.control.Columns;
                var totalRows = rows + (rowRemainder > 0 ? 1 : 0);
                var rowsPerPage = (int)(this.PageSize.Height / elemSize.Height);
                this.pageOffset = rowsPerPage * elemSize.Height;
                var pages = totalRows / rowsPerPage;
                var pageRemainder = totalRows % rowsPerPage;
                this.pageCount = pages + (pageRemainder > 0 ? 1 : 0);
                this.isPageCountValid = true;
            }
        }

        private class PaginatorSource : IDocumentPaginatorSource
        {
            public PaginatorSource(Paginator paginator)
            {
                this.DocumentPaginator = paginator;
            }

            public DocumentPaginator DocumentPaginator { get; private set; }
        }
    }
}
