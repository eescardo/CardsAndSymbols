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
        private string imageFile;
        public string ImageFile
        {
            get { return this.imageFile; }
            set { this.SetProperty(ref this.imageFile, value); }
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
