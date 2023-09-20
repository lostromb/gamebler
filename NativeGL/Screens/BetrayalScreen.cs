using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using OpenTK.Graphics.OpenGL;
using QuickFont;
using QuickFont.Configuration;
using OpenTK;
using System.Drawing;
using NativeGL.Structures;
using OpenTK.Input;

namespace NativeGL.Screens
{
    public class BetrayalScreen : GameScreen
    {
        private QFont _questionFont;
        private QFont _headerFont;
        private QFontDrawing _drawing;
        private QFontRenderOptions _renderOptions;

        private bool _finished = false;

        protected override void InitializeInternal()
        {
            _headerFont = Resources.Fonts["questionheader"];
            _questionFont = Resources.Fonts["default_60pt"];

            _drawing = new QFontDrawing();

            _renderOptions = new QFontRenderOptions()
            {
                UseDefaultBlendFunction = true,
                CharacterSpacing = 0.15f
            };
        }

        public override void KeyDown(KeyboardKeyEventArgs args)
        {
            if (args.Key == OpenTK.Input.Key.BackSpace)
            {
                _finished = true;
            }
        }

        public override void KeyTyped(KeyPressEventArgs args)
        {
        }

        protected override void RenderInternal()
        {
            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _drawing.Draw();
        }

        public override void Logic(double msElapsed)
        {
        }

        public override bool Finished
        {
            get
            {
                return _finished;
            }
        }
    }
}
