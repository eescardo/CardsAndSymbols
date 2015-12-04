using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsAndSymbols
{
    [Flags]
    public enum ImageCacheFlags
    {
        ClearIds = 0x01,
        ClearFiles = 0x02,
        ClearAll = 0x03
    }
}
