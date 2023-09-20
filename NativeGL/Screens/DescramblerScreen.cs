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
    public class DescramblerScreen : GameScreen
    {
        private QFont _questionFont;
        private QFontDrawing _drawing;
        private QFontRenderOptions _renderOptions;

        private Matrix4 _projectionMatrix;
        private float _time = 0;
        private float _scramble = 0.85f;
        // Number of seconds before scramble is fully zero
        private const float TIME_TO_DESCRAMBLE = 25f;
        private bool _finished = false;
        private bool _showingAnswer = false;
        private GLTexture _imageTexture;
        private GLTexture _displacementTexture;
        private DescramblerPrompt _currentPrompt;

        protected override void InitializeInternal()
        {
            _displacementTexture = Resources.Textures["displacement"];
            _questionFont = Resources.Fonts["default_40pt"];
            _projectionMatrix = Matrix4.CreateOrthographicOffCenter(0, InternalResolutionX, InternalResolutionY, 0, -1.0f, 1.0f);

            _drawing = new QFontDrawing();
            _renderOptions = new QFontRenderOptions()
            {
                UseDefaultBlendFunction = true,
                CharacterSpacing = 0.15f,
                DropShadowColour = Color.Black,
                DropShadowActive = true,
                DropShadowOpacity = 1.0f,
            };

            // Is there an image to show?
            if (GameState.DescramblerImages.Count > 0)
            {
                // Select a random one and display it
                _currentPrompt = GameState.DescramblerImages[new Random().Next(0, GameState.DescramblerImages.Count)];
                GameState.DescramblerImages.Remove(_currentPrompt);
                _imageTexture = Resources.Textures[_currentPrompt.ImageName];
            }
            else
            {
                _finished = true;
            }
        }

        public override void KeyDown(KeyboardKeyEventArgs args)
        {
            if (args.Key == OpenTK.Input.Key.BackSpace)
            {
                _finished = true;
            }
            else if (args.Key == OpenTK.Input.Key.End)
            {
                _showingAnswer = true;
            }
        }

        protected override void RenderInternal()
        {
            Matrix4 modelViewMatrix = Matrix4.CreateTranslation((float)InternalResolutionX / 2, (float)InternalResolutionY / 2, 0.0f);

            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            if (_finished)
            {
                return;
            }

            GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);
            GL.Enable(EnableCap.Texture2D);
            int program = Resources.Shaders["descrambler"].Handle;
            GL.UseProgram(program);
            GL.Uniform1(GL.GetUniformLocation(program, "textureImage"), 0);
            GL.Uniform1(GL.GetUniformLocation(program, "distortionMap"), 1);
            GL.Uniform1(GL.GetUniformLocation(program, "distortion"), _scramble);
            GL.Uniform1(GL.GetUniformLocation(program, "time"), _time);
            GL.UniformMatrix4(GL.GetUniformLocation(program, "projectionMatrix"), false, ref _projectionMatrix);
            GL.UniformMatrix4(GL.GetUniformLocation(program, "modelViewMatrix"), false, ref modelViewMatrix);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _imageTexture.Handle);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, _displacementTexture.Handle);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            //GL.Enable(EnableCap.CullFace);
            //GL.CullFace(CullFaceMode.Back);
            // Stretch to either the sides or top based on the calculated aspect ratio
            float frameAspectRatio = (float)InternalResolutionX / (float)InternalResolutionY;
            float height;
            float width;

            if (_imageTexture.AspectRatio > frameAspectRatio)
            {
                // Wide image
                width = (float)InternalResolutionX / 2;
                height = (float)InternalResolutionX / 2 / _imageTexture.AspectRatio;
            }
            else
            {
                // Tall image
                width = (float)InternalResolutionY / 2 * _imageTexture.AspectRatio;
                height = (float)InternalResolutionY / 2;
            }

            GL.Begin(PrimitiveType.Quads);
            {
                GL.TexCoord2(1.0f, 0.0f);
                GL.Vertex2(width, 0 - height);
                GL.TexCoord2(1.0f, 1.0f);
                GL.Vertex2(width, height);
                GL.TexCoord2(0.0f, 1.0f);
                GL.Vertex2(0 - width, height);
                GL.TexCoord2(0.0f, 0.0f);
                GL.Vertex2(0 - width, 0 - height);
            }
            GL.End();

            GL.ActiveTexture(TextureUnit.Texture0);

            // Draw hint text with drop shadow
            _drawing.ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(0.0f, InternalResolutionX, 0.0f, InternalResolutionY, -1.0f, 1.0f);
            _drawing.DrawingPrimitives.Clear();

            float sidePadding = 50;
            SizeF maxWidth = new SizeF(InternalResolutionX - (sidePadding * 2), -1f);
            _drawing.Print(_questionFont, _showingAnswer ? _currentPrompt.Answer : _currentPrompt.Hint, new Vector3(InternalResolutionX / 2.0f, 150, 0), maxWidth, QFontAlignment.Centre, _renderOptions);
            _drawing.RefreshBuffers();

            _drawing.Draw();
        }

        public override void Logic(double msElapsed)
        {
            _time += ((float)msElapsed * 0.001f);
            _scramble = 0.85f * Math.Max(0, 1.0f - (_time / TIME_TO_DESCRAMBLE));
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
