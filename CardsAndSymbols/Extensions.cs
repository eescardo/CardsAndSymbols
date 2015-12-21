using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

        /// <summary>
        /// Finds a Child of a given item in the visual tree. 
        /// </summary>
        /// <param name="parent">A direct parent of the queried item.</param>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="childName">x:Name or Name of child. </param>
        /// <returns>The first parent item that matches the submitted type parameter. 
        /// If not matching item can be found, 
        /// a null parent is being returned.</returns>
        public static T FindChild<T>(this DependencyObject parent, string childName)
           where T : DependencyObject
        {
            // Confirm parent and childName are valid. 
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                T childType = child as T;
                if (childType == null)
                {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    // If the child's name is set for search
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        // if the child's name is of the request name
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    // child element found.
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }

        public static BitmapImage ToImage(this Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }
    }
}
