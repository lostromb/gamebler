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

        private bool _configured = false;
        private bool _finished = false;

        protected override void InitializeInternal()
        {
            _headerFont = Resources.Fonts["questionheader"];
            _questionFont = Resources.Fonts["default_60pt"];

            _drawing = new QFontDrawing();
            _configured = true;

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

            if (!_configured)
            {
                return;
            }

            _drawing.ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(0.0f, InternalResolutionX, 0.0f, InternalResolutionY, -1.0f, 1.0f);
            _drawing.DrawingPrimitives.Clear();

            float sidePadding = 50;
            SizeF maxWidth = new SizeF(InternalResolutionX - (sidePadding * 2), -1f);
            _drawing.Print(
                _headerFont, "Betrayal", new Vector3(InternalResolutionX / 2, InternalResolutionY - sidePadding, 0), maxWidth, QFontAlignment.Centre, _renderOptions);
            _drawing.Print(
                _questionFont,
                "Each team must secretly\r\nchoose Trust or Betray\r\n\r\nIf both TRUST, nothing happens\r\nIf only one BETRAYS,\r\nthey win 250 points\r\nIf both BETRAY, all scores go to zero",
                new Vector3(InternalResolutionX / 2, InternalResolutionY - 250, 0), maxWidth, QFontAlignment.Centre, _renderOptions);
            _drawing.RefreshBuffers();

            _drawing.Draw();
        }

        public override void Logic(double msElapsed)
        {
            if (!_configured)
            {
                return;
            }
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
