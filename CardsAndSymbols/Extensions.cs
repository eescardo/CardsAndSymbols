using System;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace CardsAndSymbols
{
    static class Extensions
    {
        public static SymbolSize NextSize(this SymbolSize size)
        {
            switch (size)
            {
                case SymbolSize.XS:
                    return SymbolSize.S;

                case SymbolSize.S:
                    return SymbolSize.M;

                case SymbolSize.M:
                    return SymbolSize.L;

                case SymbolSize.L:
                    return SymbolSize.XL;

                case SymbolSize.XL:
                    return SymbolSize.XS;

                default:
                    throw new ArgumentException("Unexpected symbol size", "size");
            }
        }

        public static double ToScale(this SymbolSize size)
        {
            switch (size)
            {
                case SymbolSize.XS:
                    return 0.5;

                case SymbolSize.S:
                    return 0.707;

                case SymbolSize.M:
                    return 1.0;

                case SymbolSize.L:
                    return 1.414;

                case SymbolSize.XL:
                    return 2.0;

                default:
                    throw new ArgumentException("Unexpected symbol size", "size");
            }
        }

        public static SymbolSize ToSymbolSize(this double scale)
        {
            if (scale < 0.6)
            {
                return SymbolSize.XS;
            }

            if (scale < 0.9)
            {
                return SymbolSize.S;
            }

            if (scale < 1.3)
            {
                return SymbolSize.M;
            }

            if (scale < 1.7)
            {
                return SymbolSize.L;
            }

            return SymbolSize.XL;
        }

        public static T? FindParent<T>(this Control child) where T : Control
        {
            return child.FindAncestorOfType<T>();
        }

        /// <summary>
        /// Finds a Child of a given item in the visual tree. 
        /// </summary>
        public static T? FindChild<T>(this Control parent, string childName)
           where T : Control
        {
            if (parent == null) return null;

            return parent.FindControl<T>(childName);
        }

        public static Avalonia.Size GetActualSize(this Control parent)
        {
            return new Avalonia.Size(parent.Bounds.Width, parent.Bounds.Height);
        }
    }
}
