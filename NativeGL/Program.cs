using NativeGL;
using NativeGL.Screens;
using OpenTK;
using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using QuickFont;
using QuickFont.Configuration;
using System.Drawing.Text;
using NativeGL.Structures;
using OpenTK.Input;

namespace NativeGL
{
    public class Program
    {
        public static void Main(string[] args)
        {
            new MainWindow().Run();
        }
    }
}
