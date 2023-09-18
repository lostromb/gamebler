using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NativeGL
{
    [Flags]
    public enum Difficulty
    {
        NotSet = 0x00,
        Level1 = 0x01,
        Level2 = 0x02,
        Level3 = 0x04,
        Level4 = 0x08,
        All = 0x0F
    }
}
