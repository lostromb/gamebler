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
        private Matrix4 _projectionMatrix;
        private float _time = 0;
        private float _scramble = 0.75f;
        // Number of seconds before scramble is fully zero
        private const float TIME_TO_DESCRAMBLE = 25f;
        private bool _finished = false;
        private GLTexture _imageTexture;
        private GLTexture _displacementTexture;

        protected override void InitializeInternal()
        {
            _displacementTexture = Resources.Textures["displacement"];
            _projectionMatrix = Matrix4.CreateOrthographicOffCenter(0, InternalResolutionX, InternalResolutionY, 0, -1.0f, 1.0f);

            // Is there an image to show?
            if (GameState.DescramblerImages.Count > 0)
            {
                // Select a random one and display it
                string textureImage = GameState.DescramblerImages[new Random().Next(0, GameState.DescramblerImages.Count)];
                GameState.DescramblerImages.Remove(textureImage);
                _imageTexture = Resources.Textures[textureImage];
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
        }

        public override void Logic(double msElapsed)
        {
            _time += ((float)msElapsed * 0.001f);
            _scramble = 0.75f * Math.Max(0, 1.0f - (_time / TIME_TO_DESCRAMBLE));
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
