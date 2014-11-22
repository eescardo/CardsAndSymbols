using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAndSymbols.ConsoleTest
{
    public class Symbol : IComparable<Symbol>, IEquatable<Symbol>
    {
        public Symbol(string name)
        {
            this.Name = name;
        }

        public string Name { get; private set; }

        public static List<Symbol> CreateDefaultSymbolList(int numSymbols)
        {
            var symbols = new List<Symbol>();

            for (int i = 0; i < numSymbols; ++i)
            {
                var symbol = new Symbol(i.ToString());
                symbols.Add(symbol);
            }

            return symbols;
        }

        public override string ToString()
        {
            return this.Name;
        }

        public override int GetHashCode()
        {
            return this.Name != null ? this.Name.GetHashCode() : 0;
        }

        public override bool Equals(object obj)
        {
            var other = obj as Symbol;
            return this.Equals(other);
        }

        public bool Equals(Symbol other)
        {
            return other != null ? this.Name == other.Name : false;
        }

        public int CompareTo(Symbol other)
        {
            if (other == null)
            {
                return 1;
            }

            return string.Compare(this.Name, other.Name);
        }
    }
}
