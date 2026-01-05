using System;
using System.IO;

namespace CardsAndSymbols
{
    public class SymbolData : BindableBase, IComparable<SymbolData>, IEquatable<SymbolData>
    {
        /// <summary>
        /// Number of rotation stations available (0-7, each representing 45 degrees).
        /// </summary>
        public const int RotationStationCount = 8;

        public SymbolData()
        {
        }

        public SymbolData(SymbolData data)
        {
            this.imageId = data.imageId;
            this.size = data.size;
            this.rotationStation = data.rotationStation;
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

        private int rotationStation = 0; // 0 to (RotationStationCount-1) representing rotation stations (0°, 36°, 72°, ..., 324°)
        public int RotationStation
        {
            get { return this.rotationStation; }
            set
            {
                // Clamp to valid range 0 to (RotationStationCount-1)
                var clampedValue = Math.Max(0, Math.Min(RotationStationCount - 1, value));
                if (this.SetProperty(ref this.rotationStation, clampedValue))
                {
                    // Notify that RotationDegrees also changed, as it's computed from RotationStation
                    this.OnPropertyChanged(nameof(RotationDegrees));
                }
            }
        }

        /// <summary>
        /// Gets the rotation angle in degrees for the current rotation station.
        /// Each station represents 360 / RotationStationCount degrees.
        /// </summary>
        public double RotationDegrees => this.RotationStation * (360.0 / RotationStationCount);

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
