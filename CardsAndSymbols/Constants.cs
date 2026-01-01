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
        public const double SymbolItemsControlScaleFactor = 0.85;
    }
}

