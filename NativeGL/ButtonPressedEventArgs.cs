using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NativeGL
{
    public class ButtonPressedEventArgs : EventArgs
    {
        public ButtonPressedEventArgs(string id, MouseButton mouseButton)
        {
            SourceButtonId = id;
            MouseButton = mouseButton;
        }

        public string SourceButtonId { get; set; }
        public MouseButton MouseButton { get; set; }
    }
}
