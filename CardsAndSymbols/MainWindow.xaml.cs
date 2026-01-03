
namespace CardsAndSymbols
{
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

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

        private const string DefaultImageDirectory = "Images/SVG";

        public static readonly StyledProperty<ICollection<CardData>?> CardsProperty = AvaloniaProperty.Register<MainWindow, ICollection<CardData>?>(
            "Cards");

        public static readonly StyledProperty<int> NewNumCardsProperty = AvaloniaProperty.Register<MainWindow, int>(
            "NewNumCards", DefaultNumCards);

        public static readonly StyledProperty<int> NumCardColumnsProperty = AvaloniaProperty.Register<MainWindow, int>(
            "NumCardColumns", DefaultNumCardColumns);

        public static readonly StyledProperty<double> CardBaseSizeProperty = AvaloniaProperty.Register<MainWindow, double>(
            "CardBaseSize", DefaultCardSize);

        public static readonly StyledProperty<double> CardScaleFactorProperty = AvaloniaProperty.Register<MainWindow, double>(
            "CardScaleFactor", DefaultCardScaleFactor);

        public static readonly StyledProperty<double> CardMarginProperty = AvaloniaProperty.Register<MainWindow, double>(
            "CardMargin", DefaultCardMargin);

        public static readonly StyledProperty<string?> ImageDirectoryProperty = AvaloniaProperty.Register<MainWindow, string?>(
            "ImageDirectory", DefaultImageDirectory);

        public static readonly StyledProperty<ImageCache?> ImageCacheProperty = AvaloniaProperty.Register<MainWindow, ImageCache?>(
            "ImageCache");

        private const string CardsFileName = "cards.json";
        private const string SettingsFileName = "settings.json";
        private System.Threading.Timer? autoSaveTimer;

        public MainWindow()
        {
            this.InitializeComponent();
            this.DataContext = this;
            var app = (App)Application.Current!;
            this.ImageCache = app.Resources["ImageCache"] as ImageCache;
            this.Opened += WindowLoaded;
            
            // Set default image directory if not set
            if (string.IsNullOrEmpty(this.ImageDirectory))
            {
                this.ImageDirectory = DefaultImageDirectory;
            }

            // Load settings
            this.LoadSettings();
        }

        public static readonly StyledProperty<bool> AutoSaveEnabledProperty = AvaloniaProperty.Register<MainWindow, bool>(
            "AutoSaveEnabled", false);

        public bool AutoSaveEnabled
        {
            get => this.GetValue(AutoSaveEnabledProperty);
            set
            {
                this.SetValue(AutoSaveEnabledProperty, value);
                this.SaveSettings();
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFileName))
                {
                    var json = File.ReadAllText(SettingsFileName);
                    var settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    if (settings != null && settings.ContainsKey("AutoSaveEnabled"))
                    {
                        this.AutoSaveEnabled = Convert.ToBoolean(settings["AutoSaveEnabled"]);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new Dictionary<string, object>
                {
                    { "AutoSaveEnabled", this.AutoSaveEnabled }
                };
                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(SettingsFileName, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }

        public ICollection<CardData>? Cards
        {
            get => this.GetValue(CardsProperty);
            set => this.SetValue(CardsProperty, value);
        }

        public int NewNumCards
        {
            get => this.GetValue(NewNumCardsProperty);
            set => this.SetValue(NewNumCardsProperty, value);
        }

        public int NumCardColumns
        {
            get => this.GetValue(NumCardColumnsProperty);
            set => this.SetValue(NumCardColumnsProperty, value);
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

        public double CardMargin
        {
            get => this.GetValue(CardMarginProperty);
            set => this.SetValue(CardMarginProperty, value);
        }

        public string? ImageDirectory
        {
            get => this.GetValue(ImageDirectoryProperty);
            set => this.SetValue(ImageDirectoryProperty, value);
        }

        public ImageCache? ImageCache
        {
            get => this.GetValue(ImageCacheProperty);
            set => this.SetValue(ImageCacheProperty, value);
        }

        private void ComputeCards(string? symbolDir, int numCards)
        {
            // Construct a projective plane and map points to cards and lines to symbols
            this.ImageCache?.Clear(ImageCacheFlags.ClearIds);
            if (symbolDir == null) return;
            var symbols = this.GetSymbols(symbolDir);
            var planeConstructor = new ProjectivePlaneConstructor<SymbolData>(symbols, numCards);
            var planePoints = planeConstructor.PlanePoints;
            this.Cards = planePoints.Select(point => new CardData(point.Lines)).ToList();

            // Auto-load cards if auto-save is enabled
            if (this.AutoSaveEnabled && File.Exists(CardsFileName))
            {
                try
                {
                    this.LoadCardsFromFile(CardsFileName);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to auto-load cards: {ex.Message}");
                }
            }
        }

        private IList<SymbolData> GetSymbols(string symbolDir)
        {
            var dirInfo = new DirectoryInfo(symbolDir);
            if (!dirInfo.Exists)
            {
                throw new ArgumentException(string.Format("Invalid symbol directory '{0}' specified", symbolDir), "symbolDir");
            }

            return dirInfo.EnumerateFiles().Select(f => new SymbolData { ImageId = this.ImageCache?.AssignNewId(f.FullName) ?? f.FullName }).ToList();
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

        private void WindowLoaded(object? sender, EventArgs e)
        {
            
            try
            {
                var imageDir = this.ImageDirectory ?? DefaultImageDirectory;
                
                // Try multiple path resolution strategies
                string? resolvedPath = null;
                
                // Strategy 1: If already absolute, use it
                if (Path.IsPathRooted(imageDir) && Directory.Exists(imageDir))
                {
                    resolvedPath = imageDir;
                }

                // Strategy 2: Try relative to executable directory
                else if (!Path.IsPathRooted(imageDir))
                {
                    var baseDir = AppContext.BaseDirectory;
                    var tryPath1 = Path.Combine(baseDir, imageDir);
                    if (Directory.Exists(tryPath1))
                    {
                        resolvedPath = tryPath1;
                    }
                    // Strategy 3: Try relative to current working directory
                    else
                    {
                        var tryPath2 = Path.Combine(Directory.GetCurrentDirectory(), imageDir);
                        if (Directory.Exists(tryPath2))
                        {
                            resolvedPath = tryPath2;
                        }
                        // Strategy 4: Try the path as-is (might be relative to project root)
                        else if (Directory.Exists(imageDir))
                        {
                            resolvedPath = Path.GetFullPath(imageDir);
                        }
                    }
                }

                if (resolvedPath == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Could not find image directory: {imageDir}");
                    System.Diagnostics.Debug.WriteLine($"Tried: {Path.Combine(AppContext.BaseDirectory, imageDir)}");
                    System.Diagnostics.Debug.WriteLine($"Tried: {Path.Combine(Directory.GetCurrentDirectory(), imageDir)}");
                    // Set empty cards to prevent crash
                    this.Cards = new List<CardData>();
                    return;
                }
                
                this.ComputeCards(resolvedPath, NewNumCards);
            }
            catch (Exception ex)
            {
                // Log error - in a real app you'd use proper logging
                System.Diagnostics.Debug.WriteLine($"Error loading window: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                // Set empty cards to prevent crash
                this.Cards = new List<CardData>();
            }
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);
            var appGrid = this.FindControl<Grid>("AppGrid");
            if (appGrid != null)
            {
                this.ComputeColumns(appGrid.ColumnDefinitions[CardsColumnIndex].ActualWidth, this.CardBaseSize * this.CardScaleFactor, this.CardMargin);
                
                // UniformGrid Columns is now bound, no need to update manually
                // Update CardViewers to maintain aspect ratio
                UpdateCardViewers();
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == AutoSaveEnabledProperty)
            {
                this.SaveSettings();
            }
            else if (change.Property == CardScaleFactorProperty || change.Property == CardBaseSizeProperty)
            {
                var appGrid = this.FindControl<Grid>("AppGrid");
                if (appGrid != null)
                {
                    this.ComputeColumns(appGrid.ColumnDefinitions[CardsColumnIndex].ActualWidth, this.CardBaseSize * this.CardScaleFactor, this.CardMargin);
                    
                    // UniformGrid Columns is now bound, no need to update manually
                    // Update all CardViewer instances
                    UpdateCardViewers();
                }
            }
            else if (change.Property == CardsProperty)
            {
                // Unsubscribe from old collection
                if (change.OldValue is INotifyCollectionChanged oldCollection)
                {
                    oldCollection.CollectionChanged -= this.OnCardsCollectionChanged;
                }
                // Unsubscribe from old cards' symbol changes
                if (change.OldValue is ICollection<CardData> oldCards)
                {
                    foreach (var card in oldCards)
                    {
                        this.UnsubscribeFromCardChanges(card);
                    }
                }

                // Subscribe to new collection
                if (change.NewValue is INotifyCollectionChanged newCollection)
                {
                    newCollection.CollectionChanged += this.OnCardsCollectionChanged;
                }
                // Subscribe to new cards' symbol changes
                if (change.NewValue is ICollection<CardData> newCards)
                {
                    foreach (var card in newCards)
                    {
                        this.SubscribeToCardChanges(card);
                    }
                }

                // When Cards change, update all CardViewers after UI is updated
                var dispatcher = Avalonia.Threading.Dispatcher.UIThread;
                dispatcher.Post(() => 
                {
                    UpdateCardViewers();
                    // Also try again after render to catch any late-loaded controls
                    dispatcher.Post(() => UpdateCardViewers(), Avalonia.Threading.DispatcherPriority.Render);
                }, Avalonia.Threading.DispatcherPriority.Loaded);
            }
        }

        private void OnCardsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (CardData card in e.NewItems)
                {
                    this.SubscribeToCardChanges(card);
                }
            }
            if (e.OldItems != null)
            {
                foreach (CardData card in e.OldItems)
                {
                    this.UnsubscribeFromCardChanges(card);
                }
            }
            this.ScheduleAutoSave();
        }

        private void SubscribeToCardChanges(CardData card)
        {
            if (card.Symbols is INotifyCollectionChanged symbolCollection)
            {
                symbolCollection.CollectionChanged += (s, e) => this.ScheduleAutoSave();
            }
            foreach (var symbol in card.Symbols ?? Enumerable.Empty<SymbolData>())
            {
                symbol.PropertyChanged += this.OnSymbolPropertyChanged;
            }
        }

        private void UnsubscribeFromCardChanges(CardData card)
        {
            foreach (var symbol in card.Symbols ?? Enumerable.Empty<SymbolData>())
            {
                symbol.PropertyChanged -= this.OnSymbolPropertyChanged;
            }
        }

        private void OnSymbolPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Auto-save when symbol properties change (OffsetX, OffsetY, Size)
            if (e.PropertyName == nameof(SymbolData.OffsetX) ||
                e.PropertyName == nameof(SymbolData.OffsetY) ||
                e.PropertyName == nameof(SymbolData.Size))
            {
                this.ScheduleAutoSave();
            }
        }

        private void ScheduleAutoSave()
        {
            if (!this.AutoSaveEnabled || this.Cards == null) return;

            // Debounce auto-save - wait 500ms after last change before saving
            this.autoSaveTimer?.Dispose();
            this.autoSaveTimer = new System.Threading.Timer(_ =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    this.AutoSaveCards();
                });
            }, null, 500, System.Threading.Timeout.Infinite);
        }

        private void AutoSaveCards()
        {
            if (!this.AutoSaveEnabled || this.Cards == null) return;

            try
            {
                this.SaveCardsToFile(CardsFileName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to auto-save cards: {ex.Message}");
            }
        }
        
        private void UpdateCardViewers()
        {
            var cardContainer = this.FindControl<ItemsControl>("CardContainer");
            if (cardContainer != null)
            {
                // Find all CardViewers in the visual tree
                foreach (var child in cardContainer.GetVisualDescendants().OfType<CardViewer>())
                {
                    child.CardBaseSize = this.CardBaseSize;
                    child.CardScaleFactor = this.CardScaleFactor;
                }
            }
        }

        private void HandleNewClick(object? sender, RoutedEventArgs e)
        {
            this.ComputeCards(this.ImageDirectory, NewNumCards);
        }

        private void HandleSaveClick(object? sender, RoutedEventArgs e)
        {
            this.SaveCardsToFile(CardsFileName);
        }

        private void SaveCardsToFile(string fileName)
        {
            var json = JsonConvert.SerializeObject(this.Cards, Formatting.Indented);
            File.WriteAllText(fileName, json);
        }

        private void HandleLoadClick(object? sender, RoutedEventArgs e)
        {
            this.LoadCardsFromFile(CardsFileName);
        }

        private void LoadCardsFromFile(string fileName)
        {
            if (File.Exists(fileName))
            {
                var json = File.ReadAllText(fileName);
                var cards = JsonConvert.DeserializeObject<List<CardData>>(json);
                this.Cards = cards ?? new List<CardData>();
            }
        }

        private void HandlePrintClick(object? sender, RoutedEventArgs e)
        {
            // Printing in Avalonia requires platform-specific implementation
            // For now, we'll skip printing functionality
            // TODO: Implement cross-platform printing using Avalonia's printing APIs when available
        }
    }
}
