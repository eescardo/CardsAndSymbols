namespace CardsAndSymbols
{
    /// <summary>
    /// Application settings that are persisted to disk.
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// Whether auto-save is enabled for cards.
        /// </summary>
        public bool AutoSaveEnabled { get; set; } = false;

        /// <summary>
        /// The scale factor for cards (affects card size in UI and PDF).
        /// </summary>
        public double CardScaleFactor { get; set; } = 1.0;

        /// <summary>
        /// The last directory where PDFs were generated.
        /// </summary>
        public string? LastPdfDirectory { get; set; }

        /// <summary>
        /// The last file name used for single PDF generation.
        /// </summary>
        public string? LastPdfFileName { get; set; }
    }
}

