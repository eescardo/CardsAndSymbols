using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Svg;

namespace CardsAndSymbols
{
    public class ImageCache
    {
        private Dictionary<string, string> idToFileName = new Dictionary<string, string>();
        private Dictionary<string, ImageSource> fileNameToImage = new Dictionary<string, ImageSource>();
        private int nextId = 0;

        public ImageCache()
        {
        }

        public string AssignNewId(string fileName)
        {
            string id = (nextId++).ToString();
            this.idToFileName.Add(id, fileName);

            return id;
        }

        public string GetFileName(string fileId)
        {
            return this.idToFileName[fileId];
        }

        public ImageSource GetImage(string fileId)
        {
            var fileName = this.GetFileName(fileId);

            ImageSource imageSource;
            if (this.fileNameToImage.TryGetValue(fileName, out imageSource))
            {
                return imageSource;
            }

            if (fileName.EndsWith(".svg"))
            {
                var doc = SvgDocument.Open(fileName);
                doc.Width = (int)((doc.Width / (double)doc.Height) * 512);
                doc.Height = 512;
                imageSource = doc.Draw().ToImage();
            }
            else
            {
                imageSource = new BitmapImage(new Uri(fileName));
            }

            this.fileNameToImage.Add(fileName, imageSource);
            return imageSource;
        }

        public void Clear(ImageCacheFlags flags)
        {
            if (flags.HasFlag(ImageCacheFlags.ClearIds))
            {
                this.idToFileName.Clear();
            }

            if (flags.HasFlag(ImageCacheFlags.ClearFiles))
            {
                this.fileNameToImage.Clear();
            }
        }
    }
}
