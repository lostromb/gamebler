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

namespace NativeGL.Screens
{
    public class QuizzlerQuestionScreen : GameScreen
    {
        private Matrix4 projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(1.5f, 1.0f, 1.0f, 1000.0f);
        private float _rotation = 0;
        private readonly QuizzlerQuestion _question;

        public QuizzlerQuestionScreen(QuizzlerQuestion question)
        {
            _question = question;
            QuestionId = question.Id;
            WasCorrect = new Random().NextDouble() < 0.5;
        }

        protected override void InitializeInternal()
        {
        }

        protected override void RenderInternal()
        {
            Matrix4 modelViewMatrix = Matrix4.Identity;
            modelViewMatrix = Matrix4.CreateTranslation(0.0f, 0.0f, -3.0f) * modelViewMatrix;
            modelViewMatrix = Matrix4.CreateRotationY(_rotation * 3.141592f / 180) * modelViewMatrix;

            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);
            GL.Enable(EnableCap.Texture2D);
            GL.UseProgram(Resources.Shaders["default"].Handle);
            GL.Uniform1(GL.GetUniformLocation(Resources.Shaders["default"].Handle, "textureImage"), 0);
            GL.UniformMatrix4(GL.GetUniformLocation(Resources.Shaders["default"].Handle, "projectionMatrix"), false, ref projectionMatrix);
            GL.UniformMatrix4(GL.GetUniformLocation(Resources.Shaders["default"].Handle, "modelViewMatrix"), false, ref modelViewMatrix);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, Resources.Textures["background"].Handle);
            //GL.Enable(EnableCap.CullFace);
            //GL.CullFace(CullFaceMode.Back);

            GL.Begin(PrimitiveType.Quads);
            {
                GL.TexCoord2(1.0f, 1.0f);
                GL.Vertex3(1.0f, -1.0f, 0.0f);
                GL.TexCoord2(1.0f, 0.0f);
                GL.Vertex3(1.0f, 1.0f, 0.0f);
                GL.TexCoord2(0.0f, 0.0f);
                GL.Vertex3(-1.0f, 1.0f, 0.0f);
                GL.TexCoord2(0.0f, 1.0f);
                GL.Vertex3(-1.0f, -1.0f, 0.0f);
            }
            GL.End();
        }

        public override void Logic(double msElapsed)
        {
            _rotation += ((float)msElapsed * 0.1f);
        }

        public override bool Finished
        {
            get
            {
                return _rotation > 200;
            }
        }

        public bool WasCorrect { get; private set; }
        public int QuestionId { get; private set; }
    }
}
