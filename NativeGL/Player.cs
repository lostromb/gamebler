using NativeGL.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NativeGL
{
    public class Player
    {
        public string Name { get; set; }
        public GLTexture Avatar { get; set; }
        public int Score { get; set; }
    }
}
