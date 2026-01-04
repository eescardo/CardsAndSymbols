using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CardsAndSymbols
{
    public partial class PdfGenerationDialog : Window
    {
        public bool DialogResult { get; private set; } = false;
        public bool GenerateSinglePdf { get; private set; } = true;
        public string? OutputPath { get; private set; }

        private const string DefaultOutputFileName = "cards.pdf";

        public PdfGenerationDialog()
        {
            InitializeComponent();
            this.SinglePdfRadio.IsCheckedChanged += (s, e) => UpdateOutputFileVisibility();
            this.MultiplePdfsRadio.IsCheckedChanged += (s, e) => UpdateOutputFileVisibility();
            UpdateOutputFileVisibility();
        }

        private void UpdateOutputFileVisibility()
        {
            var isSingle = this.SinglePdfRadio.IsChecked == true;
            this.OutputFileLabel.IsVisible = isSingle;
            this.OutputFileTextBox.IsVisible = isSingle;
        }

        private async void HandleBrowseClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                if (this.SinglePdfRadio.IsChecked == true)
                {
                    // Browse for file
                    var file = await topLevel.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
                    {
                        Title = "Save PDF As",
                        DefaultExtension = "pdf",
                        SuggestedFileName = this.OutputFileTextBox.Text ?? DefaultOutputFileName,
                        FileTypeChoices = new[]
                        {
                            new Avalonia.Platform.Storage.FilePickerFileType("PDF files")
                            {
                                Patterns = new[] { "*.pdf" }
                            }
                        }
                    });

                    if (file != null)
                    {
                        this.OutputFileTextBox.Text = Path.GetFileName(file.Path.LocalPath);
                        this.OutputDirectoryTextBox.Text = Path.GetDirectoryName(file.Path.LocalPath) ?? "";
                    }
                }
                else
                {
                    // Browse for directory
                    var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
                    {
                        Title = "Select Output Directory"
                    });

                    if (folders.Count > 0 && folders[0] != null)
                    {
                        this.OutputDirectoryTextBox.Text = folders[0].Path.LocalPath;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error browsing: {ex.Message}");
            }
        }

        private void HandleGenerateClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                this.GenerateSinglePdf = this.SinglePdfRadio.IsChecked == true;
                var outputDir = this.OutputDirectoryTextBox.Text?.Trim();

                if (string.IsNullOrEmpty(outputDir))
                {
                    // Validation error - user can see the empty field
                    System.Diagnostics.Debug.WriteLine("Output directory is required");
                    return;
                }

                if (this.GenerateSinglePdf)
                {
                    var fileName = this.OutputFileTextBox.Text?.Trim() ?? "cards.pdf";
                    if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        fileName += ".pdf";
                    }
                    this.OutputPath = Path.Combine(outputDir, fileName);
                }
                else
                {
                    this.OutputPath = outputDir;
                }

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GenerateClick: {ex.Message}");
            }
        }

        private void HandleCancelClick(object? sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}

