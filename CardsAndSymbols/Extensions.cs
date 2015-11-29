using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

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

        public static T FindParent<T>(this DependencyObject child) where T : DependencyObject
        {
            //get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            //we've reached the end of the tree
            if (parentObject == null)
            {
                return null;
            }

            //check if the parent matches the type we're looking for
            T parent = parentObject as T;
            if (parent != null)
            {
                return parent;
            }

            return parentObject.FindParent<T>();
        }
    }
}
