using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAndSymbols
{
    public class CardData : BindableBase
    {
        public CardData()
        {
            this.Symbols = new List<SymbolData>();
        }

        public CardData(ICollection<SymbolData> symbols)
        {
            this.Symbols = symbols.Select(x => new SymbolData(x)).ToList();
        }

        private ICollection<SymbolData> symbols;
        public ICollection<SymbolData> Symbols
        { 
            get { return this.symbols; }
            set
            {
                if (value != null)
                {
                    this.Columns = (int)Math.Ceiling(Math.Sqrt(value.Count));
                }

                this.SetProperty(ref this.symbols, value);
            }
        }

        private int columns;
        public int Columns
        {
            get { return this.columns; }
            set { this.SetProperty(ref this.columns, value); }
        }

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
