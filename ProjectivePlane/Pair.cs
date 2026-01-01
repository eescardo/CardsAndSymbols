
namespace ProjectivePlane
{
    using System;

    /// <summary>
    /// Pair of comparable objects.
    /// </summary>
    /// <remarks>
    /// The pair constructed for any two specific objects is always identical regardless of order in
    /// which objects are specified during construction.
    /// </remarks>
    public class Pair<T> : IEquatable<Pair<T>> where T : IComparable<T>
    {
        /// <summary>
        /// Initializes a new instance of a new pair object.
        /// </summary>
        /// <param name="a">One of the objects in pair</param>
        /// <param name="b">The other object that is part of pair</param>
        /// <remarks>
        /// The constructed pair will be identical regardless of the order in which <paramref name="a"/>
        /// and <paramref name="b"/> are specified.
        /// </remarks>
        public Pair(T a, T b)
        {
            if (a == null)
            {
                throw new ArgumentNullException("a");
            }

            if (b == null)
            {
                throw new ArgumentNullException("b");
            }

            var compareResult = a.CompareTo(b);
            if (compareResult == 0)
            {
                throw new ArgumentException("Symbols must be distinct");
            }

            if (compareResult < 0)
            {
                this.First = a;
                this.Second = b;
            }
            else
            {
                this.First = a;
                this.Second = b;
            }
        }

        public T First { get; private set; }

        public T Second { get; private set; }

        public override int GetHashCode()
        {
            return (31 * this.First.GetHashCode()) + this.Second.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            var other = obj as Pair<T>;
            return this.Equals(other);
        }

        public bool Equals(Pair<T>? other)
        {
            if (other == null)
            {
                return false;
            }
            return (this.First.CompareTo(other.First) == 0) && (this.Second.CompareTo(other.Second) == 0);
        }

        public override string ToString()
        {
            return string.Format("{{{0},{1}}}", this.First, this.Second);
        }
    }
}
