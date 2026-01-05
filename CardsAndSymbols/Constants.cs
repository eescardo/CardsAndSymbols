namespace CardsAndSymbols
{
    /// <summary>
    /// Application-wide constants for sizing and layout.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Base size in pixels for symbols. Symbols are scaled from this base size.
        /// </summary>
        public const double BaseSymbolSize = 64.0;

        /// <summary>
        /// Scale factor applied to the ItemsControl containing symbols within a card.
        /// This provides visual spacing and prevents symbols from touching the card edges.
        /// </summary>
        public const double SymbolItemsControlScaleFactor = 0.7;

        /// <summary>
        /// Conversion factor from pixels to points at 96 DPI.
        /// 1 pixel = 0.75 points (since 1 point = 1/72 inch and 96 DPI means 1 inch = 96 pixels).
        /// </summary>
        public const double PixelsToPoints = 0.75;

        /// <summary>
        /// Number of points per inch (standard PostScript/PDF measurement).
        /// 1 inch = 72 points.
        /// </summary>
        public const double PointsPerInch = 72.0;

        /// <summary>
        /// Conversion factor from millimeters to points.
        /// 1mm = 2.83465 points (since 1 inch = 25.4mm and 1 inch = 72 points).
        /// </summary>
        public const double MillimetersToPoints = 2.83465;

        /// <summary>
        /// Correction factor for QuestPDF image embedding.
        /// QuestPDF embeds bitmap images, and the actual printed size may differ slightly
        /// from the theoretical pixel-to-point conversion due to how QuestPDF
        /// handles embedded image scaling. Based on empirical measurement: actual printed size (2.5") / calculated size (2.67") ≈ 0.936
        /// TODO: Figure this out in a less work-aroundy way.
        /// </summary>
        public const double ImageEmbeddingCorrectionFactor = 1.0;
    }
}

