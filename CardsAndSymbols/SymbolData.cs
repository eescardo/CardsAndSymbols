using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAndSymbols
{
    public class SymbolData : BindableBase, IComparable<SymbolData>, IEquatable<SymbolData>
    {
        public SymbolData()
        {
        }

        public SymbolData(SymbolData data)
        {
            this.imageFile = data.imageFile;
            this.size = data.size;
        }

        private string imageFile;
        public string ImageFile
        {
            get { return this.imageFile; }
            set { this.SetProperty(ref this.imageFile, value); }
        }

        private SymbolSize size = SymbolSize.M;
        public SymbolSize Size
        {
            get { return this.size; }
            set { this.SetProperty(ref this.size, value); }
        }

        private double offsetX = 0.0;
        public double OffsetX
        {
            get { return this.offsetX; }
            set { this.SetProperty(ref this.offsetX, value); }
        }

        private double offsetY = 0.0;
        public double OffsetY
        {
            get { return this.offsetY; }
            set { this.SetProperty(ref this.offsetY, value); }
        }

        public override string ToString()
        {
            var info = new FileInfo(this.ImageFile);
            return info.Exists ? info.Name : this.ImageFile;
        }

        public override int GetHashCode()
        {
            return this.ImageFile != null ? this.ImageFile.GetHashCode() : 0;
        }

        public override bool Equals(object obj)
        {
            var other = obj as SymbolData;
            return this.Equals(other);
        }

        public bool Equals(SymbolData other)
        {
            return other != null ? this.ImageFile == other.ImageFile : false;
        }

        public int CompareTo(SymbolData other)
        {
            if (other == null)
            {
                return 1;
            }

            return string.Compare(this.ImageFile, other.ImageFile);
        }
    }
}
