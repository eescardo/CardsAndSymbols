using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAndSymbols
{
    static class Extensions
    {
        public static SymbolSize NextSize(this SymbolSize size)
        {
            switch (size)
            {
                case SymbolSize.Small:
                    return SymbolSize.Medium;

                case SymbolSize.Medium:
                    return SymbolSize.Large;

                case SymbolSize.Large:
                    return SymbolSize.Small;

                default:
                    throw new ArgumentException("Unexpected symbol size", "size");
            }
        }

        public static double ToScale(this SymbolSize size)
        {
            switch (size)
            {
                case SymbolSize.Small:
                    return 0.5;

                case SymbolSize.Medium:
                    return 1.0;

                case SymbolSize.Large:
                    return 2.0;

                default:
                    throw new ArgumentException("Unexpected symbol size", "size");
            }
        }

        public static SymbolSize ToSymbolSize(this double scale)
        {
            if (scale < 0.75)
            {
                return SymbolSize.Small;
            }

            if (scale < 1.5)
            {
                return SymbolSize.Medium;
            }

            return SymbolSize.Large;
        }
    }
}
