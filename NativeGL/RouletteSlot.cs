using OpenTK;
using QuickFont;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NativeGL
{
    public class RouletteSlot
    {
        public float Weight;
        public Vector3 Color;
        public string Label;
        public QFontDrawing RenderedLabel;
        public RouletteSlotType Type;
    }
}
