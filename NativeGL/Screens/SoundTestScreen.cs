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
    public class SoundTestScreen : GameScreen
    {
        private QFont _headerFont;
        private QFont _answerFont;
        private QFontDrawing _drawing;
        private QFontRenderOptions _renderOptions;
        private QFontRenderOptions _shadowRenderOptions;

        private Matrix4 projectionMatrix;
        private bool _finished = false;
        private float[] _decayBuffer;
        private SoundTestPrompt _currentPrompt;
        private bool _showAnswer = false;

        protected override void InitializeInternal()
        {
            projectionMatrix = Matrix4.CreateOrthographicOffCenter(0, InternalResolutionX, InternalResolutionY, 0, -1.0f, 1.0f);
            _decayBuffer = new float[90];

            // Is there a song to play?
            if (GameState.MusicQuizSongs.Count > 0)
            {
                // Select a random one and play it
                _currentPrompt = GameState.MusicQuizSongs[new Random().Next(0, GameState.MusicQuizSongs.Count)];
                GameState.MusicQuizSongs.Remove(_currentPrompt);
                Resources.AudioSubsystem.PlayMusic(_currentPrompt.MusicName);
            }
            else
            {
                _finished = true;
            }

            _headerFont = Resources.Fonts["questionheader"];
            _answerFont = Resources.Fonts["default_40pt"];
            _drawing = new QFontDrawing();
            _renderOptions = new QFontRenderOptions()
            {
                UseDefaultBlendFunction = true,
                CharacterSpacing = 0.15f
            };

            _shadowRenderOptions = new QFontRenderOptions()
            {
                UseDefaultBlendFunction = true,
                CharacterSpacing = 0.15f,
                DropShadowColour = Color.Black,
                DropShadowActive = true,
                DropShadowOpacity = 1.0f,
            };
        }

        public override void KeyDown(KeyboardKeyEventArgs args)
        {
            if (args.Key == OpenTK.Input.Key.BackSpace)
            {
                Resources.AudioSubsystem.StopMusic();
                _finished = true;
            }
            if (args.Key == OpenTK.Input.Key.End)
            {
                _showAnswer = true;
            }
        }

        protected override void RenderInternal()
        {
            Matrix4 modelViewMatrix = Matrix4.Identity;

            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            int program = Resources.Shaders["solidcolor"].Handle;
            GL.UseProgram(program);
            GL.UniformMatrix4(GL.GetUniformLocation(program, "projectionMatrix"), false, ref projectionMatrix);
            GL.UniformMatrix4(GL.GetUniformLocation(program, "modelViewMatrix"), false, ref modelViewMatrix);

            float[] spectrum = Resources.AudioSubsystem.GetSpectrograph();

            float barWidth = InternalResolutionX / _decayBuffer.Length;
            float scale = 40f;
            float decayRate = 0.90f;
            float maxHeight = 18f;

            float x = 0;
            float y = InternalResolutionY;
            Vector4 bottomColor = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
            Vector4 topColor = new Vector4(0.0f, 1.0f, 1.0f, 1.0f);
            GL.Begin(PrimitiveType.Quads);
            {
                int decayBufferIdx = 0;
                for (int c = 5; c < spectrum.Length && decayBufferIdx < _decayBuffer.Length; c++)
                {
                    float old = _decayBuffer[decayBufferIdx];
                    float decayedS = Math.Min(maxHeight, Math.Max((spectrum[c] * decayRate) + (old * (1 - decayRate)), old * decayRate));
                    _decayBuffer[decayBufferIdx] = decayedS;

                    GL.Color4(bottomColor);
                    GL.Vertex2(x, y);
                    GL.Vertex2(x + barWidth, y);

                    float top = y - (scale * decayedS);
                    GL.Color4(topColor);
                    GL.Vertex2(x + barWidth, top);
                    GL.Vertex2(x, top);

                    x += barWidth;
                    decayBufferIdx++;
                }
            }
            GL.End();

            GL.ActiveTexture(TextureUnit.Texture0);

            // Draw hint text with drop shadow
            _drawing.ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(0.0f, InternalResolutionX, 0.0f, InternalResolutionY, -1.0f, 1.0f);
            _drawing.DrawingPrimitives.Clear();

            float sidePadding = 50;
            SizeF maxWidth = new SizeF(InternalResolutionX - (sidePadding * 2), -1f);
            _drawing.Print(_headerFont, "Identify this song", new Vector3(InternalResolutionX / 2, InternalResolutionY - sidePadding, 0), maxWidth, QFontAlignment.Centre, _renderOptions);
            
            if (_showAnswer)
            {
                _drawing.Print(_answerFont, _currentPrompt.Answer, new Vector3(InternalResolutionX / 2.0f, 150, 0), maxWidth, QFontAlignment.Centre, _shadowRenderOptions);
            }

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
