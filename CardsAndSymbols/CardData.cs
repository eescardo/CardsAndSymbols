using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            var random = new Random();
            var symbolList = symbols.Select(x => new SymbolData(x)).ToList();
            
            // Create balanced pairs so average scale is M (1.0)
            // Pair sizes: XS+XL, S+L, and M stands alone
            var sizePairs = new[]
            {
                (SymbolSize.XS, SymbolSize.XL),
                (SymbolSize.S, SymbolSize.L),
                (SymbolSize.M, SymbolSize.M) // M pairs with itself
            };
            
            var assignedIndices = new HashSet<int>();
            var result = new List<SymbolData>(symbolList);
            
            // Assign pairs
            for (int i = 0; i < result.Count; i++)
            {
                if (assignedIndices.Contains(i)) continue;
                
                // Find a partner for pairing
                int? partnerIndex = null;
                for (int j = i + 1; j < result.Count; j++)
                {
                    if (!assignedIndices.Contains(j))
                    {
                        partnerIndex = j;
                        break;
                    }
                }
                
                if (partnerIndex.HasValue)
                {
                    // Assign a pair
                    var pair = sizePairs[random.Next(sizePairs.Length)];
                    result[i].Size = pair.Item1;
                    result[partnerIndex.Value].Size = pair.Item2;
                    assignedIndices.Add(i);
                    assignedIndices.Add(partnerIndex.Value);
                }
                else
                {
                    // Odd one out gets M
                    result[i].Size = SymbolSize.M;
                    assignedIndices.Add(i);
                }
            }
            
            // Shuffle to randomize positions
            for (int i = result.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (result[i], result[j]) = (result[j], result[i]);
            }
            
            // Randomize rotation stations for all symbols
            foreach (var symbol in result)
            {
                symbol.RotationStation = random.Next(SymbolData.RotationStationCount);
            }

            this.Symbols = result;
        }

        private ICollection<SymbolData>? symbols;
        public ICollection<SymbolData>? Symbols
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

            if (this.Symbols == null) return "{}";
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
