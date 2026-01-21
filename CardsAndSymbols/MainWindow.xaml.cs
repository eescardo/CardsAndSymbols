
namespace CardsAndSymbols
{
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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

        public static readonly StyledProperty<double> CardWidthInchesProperty = AvaloniaProperty.Register<MainWindow, double>(
            "CardWidthInches", 0.0);

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

            // Initialize card width display
            this.UpdateCardWidthInches();

            // Load settings
            this.LoadSettings();
        }

        public static readonly StyledProperty<bool> AutoSaveEnabledProperty = AvaloniaProperty.Register<MainWindow, bool>(
            "AutoSaveEnabled", false);

        public bool AutoSaveEnabled
        {
            get => this.GetValue(AutoSaveEnabledProperty);
            set => this.SetValue(AutoSaveEnabledProperty, value);
        }

        private Settings currentSettings = new Settings();
        private bool isLoadingSettings = false;

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFileName))
                {
                    var json = File.ReadAllText(SettingsFileName);
                    var settings = JsonConvert.DeserializeObject<Settings>(json);
                    if (settings != null)
                    {
                        this.currentSettings = settings;

                        // Prevent SaveSettings from being called during load
                        this.isLoadingSettings = true;
                        try
                        {
                            // Set the StyledProperties to mirror currentSettings
                            this.SetValue(AutoSaveEnabledProperty, settings.AutoSaveEnabled);
                            this.SetValue(CardScaleFactorProperty, settings.CardScaleFactor);
                            if (!string.IsNullOrEmpty(settings.ImageDirectory))
                            {
                                this.SetValue(ImageDirectoryProperty, settings.ImageDirectory);
                            }
                        }
                        finally
                        {
                            this.isLoadingSettings = false;
                        }

                        // Update UI after loading settings
                        this.UpdateCardWidthInches();
                        this.UpdateCardLayout();
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
            // Don't save during initial load to avoid overwriting settings
            if (this.isLoadingSettings)
                return;

            try
            {
                // Sync currentSettings from StyledProperties
                this.currentSettings.AutoSaveEnabled = this.AutoSaveEnabled;
                this.currentSettings.CardScaleFactor = this.CardScaleFactor;
                this.currentSettings.ImageDirectory = this.ImageDirectory;

                var json = JsonConvert.SerializeObject(this.currentSettings, Formatting.Indented);
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

        public double CardWidthInches
        {
            get => this.GetValue(CardWidthInchesProperty);
            private set => this.SetValue(CardWidthInchesProperty, value);
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

        private void ComputeCards(string? symbolDir, int numCards, bool clearFileCache = false)
        {
            // Construct a projective plane and map points to cards and lines to symbols
            var clearFlags = clearFileCache
                ? ImageCacheFlags.ClearIds | ImageCacheFlags.ClearFiles
                : ImageCacheFlags.ClearIds;
            this.ImageCache?.Clear(clearFlags);

            if (symbolDir == null)
            {
                this.Cards = null;
                return;
            }

            try
            {
                var symbols = this.GetSymbols(symbolDir);
                var planeConstructor = new ProjectivePlaneConstructor<SymbolData>(symbols, numCards);
                var planePoints = planeConstructor.PlanePoints;
                this.Cards = planePoints.Select(point => new CardData(point.Lines)).ToList();

                // Auto-load cards if auto-save is enabled
                if (this.AutoSaveEnabled)
                {
                    if (!File.Exists(CardsFileName))
                    {
                        this.CopyDefaultCardsFromResourceToFile();
                    }

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
            catch (Exception ex)
            {
                // Catch any other exceptions
                this.Cards = null;
                this.ShowErrorMessage("Error", $"An error occurred while generating cards: {ex.Message}");
            }
        }

        private async void ShowErrorMessage(string title, string message)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var panel = new StackPanel
            {
                Margin = new Thickness(20)
            };

            var textBlock = new TextBlock
            {
                Text = message,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 20)
            };
            panel.Children.Add(textBlock);

            var button = new Button
            {
                Content = "OK",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Width = 80
            };
            button.Click += (s, e) => dialog.Close();
            panel.Children.Add(button);

            dialog.Content = panel;
            await dialog.ShowDialog(this);
        }

        private IList<SymbolData> GetSymbols(string symbolDir)
        {
            var dirInfo = new DirectoryInfo(symbolDir);
            if (!dirInfo.Exists)
            {
                throw new ArgumentException(string.Format("Invalid symbol directory '{0}' specified", symbolDir), "symbolDir");
            }

            // Valid image file extensions
            var imageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".svg", ".webp"
            };

            // Sort files deterministically (case-insensitive for cross-platform compatibility)
            // Sort by Name first, then FullName as tiebreaker to ensure absolute determinism
            // Use ToList() to materialize the enumeration before sorting to ensure consistency
            // Filter out non-image files (e.g., .DS_Store, Thumbs.db, etc.)
            var allFiles = dirInfo.EnumerateFiles()
                .Where(f => imageExtensions.Contains(f.Extension))
                .ToList();
            var files = allFiles
                .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(f => f.FullName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return files.Select(f => new SymbolData { ImageId = this.ImageCache?.AssignNewId(f.FullName) ?? f.FullName })
                .ToList();
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
            this.UpdateCardLayout();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == AutoSaveEnabledProperty || change.Property == CardScaleFactorProperty || change.Property == ImageDirectoryProperty)
            {
                // Sync currentSettings from StyledProperty and save
                this.SaveSettings();
                
                if (change.Property == CardScaleFactorProperty)
                {
                    // Update card width in inches for display
                    this.UpdateCardWidthInches();
                    this.UpdateCardLayout();
                }
                else if (change.Property == ImageDirectoryProperty)
                {
                    // Don't regenerate during initial load
                    if (!this.isLoadingSettings)
                    {
                        // Regenerate image cache and remap existing cards
                        this.ComputeCards(this.ImageDirectory, this.NewNumCards, true);
                    }
                }
            }
            else if (change.Property == CardBaseSizeProperty)
            {
                // Update card width in inches for display
                this.UpdateCardWidthInches();
                
                this.UpdateCardLayout();
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
            // Auto-save when symbol properties change (OffsetX, OffsetY, Size, RotationStation)
            if (e.PropertyName == nameof(SymbolData.OffsetX) ||
                e.PropertyName == nameof(SymbolData.OffsetY) ||
                e.PropertyName == nameof(SymbolData.Size) ||
                e.PropertyName == nameof(SymbolData.RotationStation))
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
        
        private void UpdateCardLayout()
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

        private void UpdateCardWidthInches()
        {
            // Calculate card width in inches for printed output
            // Note: QuestPDF embeds images as bitmaps, and the actual printed size may differ
            // slightly from the theoretical pixel-to-point conversion due to how QuestPDF
            // handles embedded image scaling. Based on empirical measurement, we apply a
            // correction factor to match actual printed output.
            var cardSizePixels = this.CardBaseSize * this.CardScaleFactor;
            var cardSizePoints = cardSizePixels * Constants.PixelsToPoints;
            var cardSizeInches = cardSizePoints / Constants.PointsPerInch;
            
            // Apply correction factor to account for how QuestPDF actually renders embedded bitmap images
            var correctedCardSizeInches = cardSizeInches * Constants.ImageEmbeddingCorrectionFactor;
            
            this.CardWidthInches = correctedCardSizeInches;
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

        private void CopyDefaultCardsFromResourceToFile(string fileName = CardsFileName)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "CardsAndSymbols.Assets.cards.json";
                string? json = null;

                // Strategy 1: Try embedded resource first
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        using (var reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            json = reader.ReadToEnd();
                        }
                    }
                    else
                    {
                        // Try to find the resource by listing all resources (for debugging)
                        var allResources = assembly.GetManifestResourceNames();
                        var matchingResource = allResources.FirstOrDefault(r => r.EndsWith("cards.json", StringComparison.OrdinalIgnoreCase));

                        if (matchingResource != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Found resource with different name: {matchingResource}");
                            using (var fallbackStream = assembly.GetManifestResourceStream(matchingResource))
                            {
                                if (fallbackStream != null)
                                {
                                    using (var reader = new StreamReader(fallbackStream, Encoding.UTF8))
                                    {
                                        json = reader.ReadToEnd();
                                    }
                                }
                            }
                        }

                        if (json == null)
                        {
                            System.Console.WriteLine($"WARNING: Could not find embedded resource: {resourceName}");
                            System.Console.WriteLine($"Available resources: {string.Join(", ", allResources)}");
                            System.Console.WriteLine("Attempting file-based fallback...");
                        }
                    }
                }

                // Strategy 2: If embedded resource failed, try file-based lookup within app bundle
                if (json == null)
                {
                    var baseDir = AppContext.BaseDirectory;
                    var possiblePaths = new[]
                    {
                        Path.Combine(baseDir, "cards.json"), // Same directory as executable
                        Path.Combine(baseDir, "Assets", "cards.json"), // Assets subdirectory
                        Path.Combine(baseDir, "..", "Resources", "Assets", "cards.json"), // macOS bundle Resources
                        Path.Combine(baseDir, "..", "..", "Resources", "Assets", "cards.json"), // Alternative bundle path
                    };

                    foreach (var path in possiblePaths)
                    {
                        var fullPath = Path.GetFullPath(path);
                        if (File.Exists(fullPath))
                        {
                            System.Console.WriteLine($"Found cards.json at: {fullPath}");
                            json = File.ReadAllText(fullPath, Encoding.UTF8);
                            break;
                        }
                    }

                    if (json == null)
                    {
                        System.Console.WriteLine($"ERROR: Could not find cards.json in any expected location:");
                        foreach (var path in possiblePaths)
                        {
                            System.Console.WriteLine($"  - {Path.GetFullPath(path)}");
                        }
                        return;
                    }
                }

                // Write the JSON to the target file
                if (json != null)
                {
                    File.WriteAllText(fileName, json, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to copy default cards from resource to file: {ex.Message}");
                System.Console.WriteLine($"ERROR: Exception while copying default cards: {ex.Message}");
                System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void HandleDefaultClick(object? sender, RoutedEventArgs e)
        {
            this.CopyDefaultCardsFromResourceToFile(CardsFileName);
            this.LoadCardsFromFile(CardsFileName);
        }

        private async void HandlePrintClick(object? sender, RoutedEventArgs e)
        {
            if (this.Cards == null || !this.Cards.Any())
            {
                // Show message that there are no cards to export
                return;
            }

            // Load last used directory and file name from settings
            var lastDirectory = this.currentSettings.LastPdfDirectory ?? Directory.GetCurrentDirectory();
            var lastFileName = this.currentSettings.LastPdfFileName ?? "cards.pdf";

            var dialog = new PdfGenerationDialog(lastDirectory, lastFileName);

            await dialog.ShowDialog(this);
            
            if (dialog.DialogResult && dialog.OutputPath != null)
            {
                var outputPath = dialog.OutputPath;

                try
                {
                    var generator = new PdfGenerator(this.ImageCache, this.CardBaseSize, this.CardScaleFactor);
                    
                    if (dialog.GenerateSinglePdf)
                    {
                        generator.GenerateSinglePdf(this.Cards, outputPath);
                        // Update settings with the directory and file name used
                        var outputDir = Path.GetDirectoryName(outputPath) ?? Directory.GetCurrentDirectory();
                        var outputFile = Path.GetFileName(outputPath);
                        this.currentSettings.LastPdfDirectory = outputDir;
                        this.currentSettings.LastPdfFileName = outputFile;
                        this.SaveSettings();
                        // Show success message
                        System.Diagnostics.Debug.WriteLine($"PDF generated successfully: {outputPath}");
                    }
                    else
                    {
                        generator.GenerateMultiplePdfs(this.Cards, outputPath);
                        // Update settings with the directory used (no file name for multiple PDFs)
                        this.currentSettings.LastPdfDirectory = outputPath;
                        this.currentSettings.LastPdfFileName = null;
                        this.SaveSettings();
                        // Show success message
                        System.Diagnostics.Debug.WriteLine($"PDFs generated successfully in: {outputPath}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error generating PDF: {ex.Message}");
                }
            }
        }

        private async void HandleBrowseImageDirectory(object? sender, RoutedEventArgs e)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
                {
                    Title = "Select Image Directory"
                });

                if (folders.Count > 0 && folders[0] != null)
                {
                    this.ImageDirectory = folders[0].Path.LocalPath;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error browsing image directory: {ex.Message}");
            }
        }
    }
}
