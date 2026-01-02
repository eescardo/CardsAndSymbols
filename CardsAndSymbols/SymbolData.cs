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
            this.imageId = data.imageId;
            this.size = data.size;
        }

        private string? imageId;
        public string? ImageId
        {
            get { return this.imageId; }
            set { this.SetProperty(ref this.imageId, value); }
        }

        private SymbolSize size = SymbolSize.M;
        public SymbolSize Size
        {
            get { return this.size; }
            set { this.SetProperty(ref this.size, value); }
        }

        private double offsetX = 0.0;
        private bool offsetXInitialized = false;
        public double OffsetX
        {
            get { return this.offsetX; }
            set {
                this.SetProperty(ref this.offsetX, value);
                this.offsetXInitialized = true;
            }
        }

        private double offsetY = 0.0;
        private bool offsetYInitialized = false;
        public double OffsetY
        {
            get { return this.offsetY; }
            set {
                this.SetProperty(ref this.offsetY, value);
                this.offsetYInitialized = true;
            }
        }

        public bool OffsetXInitialized
        {
            get { return this.offsetXInitialized; }
        }

        public bool OffsetYInitialized
        {
            get { return this.offsetYInitialized; }
        }

        public bool PositionsInitialized
        {
            get { return this.offsetXInitialized && this.offsetYInitialized; }
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(this.ImageId))
                return string.Empty;
            var info = new FileInfo(this.ImageId);
            return info.Exists ? info.Name : this.ImageId;
        }

        public override int GetHashCode()
        {
            return this.ImageId != null ? this.ImageId.GetHashCode() : 0;
        }

        public override bool Equals(object? obj)
        {
            var other = obj as SymbolData;
            return this.Equals(other);
        }

        public bool Equals(SymbolData? other)
        {
            return other != null ? this.ImageId == other.ImageId : false;
        }

        public int CompareTo(SymbolData? other)
        {
            if (other == null)
            {
                return 1;
            }

            return string.Compare(this.ImageId, other.ImageId);
        }
    }
}
