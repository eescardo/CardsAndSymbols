

namespace CardsAndSymbols.ConsoleTest
{
    using System;
    using System.Text;
    using System.Collections.Generic;

    public class Card<Sym> where Sym : IComparable<Sym>
    {
        public Card()
        {
            this.Symbols = new HashSet<Sym>();
        }

        public Card(ICollection<Sym> symbols)
        {
            this.Symbols = new HashSet<Sym>(symbols);
        }

        public ISet<Sym> Symbols { get; private set; }

        public override string ToString()
        {
            var builder = new StringBuilder();
            bool isFirst = true;
            builder.Append("{");

            foreach (var symbol in this.Symbols)
            {
                if (!isFirst)
                {
                    builder.Append(", ");
                }

                builder.Append(symbol);
                isFirst = false;
            }

            builder.Append("}");

            return builder.ToString();
        }
    }
}
