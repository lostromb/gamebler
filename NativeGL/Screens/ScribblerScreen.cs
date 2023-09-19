﻿using System;
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
    public class ScribblerScreen : GameScreen
    {
        private QFont _questionFont;
        private QFont _headerFont;
        private QFontDrawing _drawing;
        private QFontRenderOptions _renderOptions;

        private bool _finished = false;

        protected override void InitializeInternal()
        {
            _headerFont = Resources.Fonts["questionheader"];
            _questionFont = Resources.Fonts["default_40pt"];

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
                //Resources.AudioSubsystem.StopMusic();
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
            

            _drawing.ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(0.0f, InternalResolutionX, 0.0f, InternalResolutionY, -1.0f, 1.0f);
            _drawing.DrawingPrimitives.Clear();

            float sidePadding = 50;
            SizeF maxWidth = new SizeF(InternalResolutionX - (sidePadding * 2), -1f);
            _drawing.Print(_headerFont, "DRAWBAGE", new Vector3(InternalResolutionX / 2, InternalResolutionY - sidePadding, 0), maxWidth, QFontAlignment.Centre, _renderOptions);
            _drawing.Print(_questionFont, "One person will draw the given prompt on the board.\r\n\r\n150 points are awarded if their team can guess correctly what you are drawing within the time limit.\r\n\r\nM.C. will provide the prompt.\r\n\r\nOther team will be allowed 1 guess to steal points if you fail.", new Vector3(sidePadding, InternalResolutionY - 250, 0), maxWidth, QFontAlignment.Justify, _renderOptions);
            _drawing.RefreshBuffers();

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
