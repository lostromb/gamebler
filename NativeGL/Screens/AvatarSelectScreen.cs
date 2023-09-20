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
    public class AvatarSelectScreen : GameScreen
    {
        private QFont _headerFont;
        private QFontDrawing _drawing;
        private QFontRenderOptions _renderOptions;

        private bool _finished = false;
        private List<AvatarButton> _buttons;
        private string _chosenAvatar;
        private Matrix4 _projectionMatrix;

        private class AvatarButton
        {
            public GLButton Button;
            public GLTexture Texture;
            public string AvatarKey;
            public float X;
            public float Y;
            public float Width;
            public float Height;
        }

        public AvatarSelectScreen()
        {
        }

        protected override void InitializeInternal()
        {
            _projectionMatrix = Matrix4.CreateOrthographicOffCenter(0, InternalResolutionX, 0, InternalResolutionY, -1.0f, 1.0f);
            _buttons = new List<AvatarButton>();
            KeyValuePair<string, GLTexture>[] avatarNames = new List<KeyValuePair<string, GLTexture>>(Resources.Avatars.AvailableAvatars).ToArray();
            Random rand = new Random();

            // Bubble shuffle the list
            for (int c = 0; c < 1000; c++)
            {
                int src = rand.Next(0, avatarNames.Length);
                int dst = rand.Next(0, avatarNames.Length);
                KeyValuePair<string, GLTexture> tmp = avatarNames[src];
                avatarNames[src] = avatarNames[dst];
                avatarNames[dst] = tmp;
            }

            // Present some to select in two rows

            const int rows = 2;
            const int columns = 8;
            float buttonHeight = 200;
            float buttonWidth = 200;
            float buttonPadding = 20;
            float totalColumnWidth = (buttonWidth * columns) + (buttonPadding * columns) - buttonPadding;
            int avatarIndex = 0;

            for (int row = 0; row < rows; row++)
            {
                float buttonY = 400 + ((buttonHeight + buttonPadding) * row);
                float buttonX = (InternalResolutionX - totalColumnWidth) / 2;
                for (int column = 0; column < columns; column++)
                {
                    if (avatarIndex >= avatarNames.Length)
                    {
                        continue;
                    }

                    GLButton rawButton = new GLButton(Resources, buttonX, buttonY, buttonWidth, buttonHeight, string.Empty, avatarNames[avatarIndex].Key);
                    AvatarButton button = new AvatarButton()
                    {
                        Button = rawButton,
                        AvatarKey = avatarNames[avatarIndex].Key,
                        Texture = avatarNames[avatarIndex].Value,
                        X = buttonX,
                        Y = buttonY,
                        Width = buttonWidth,
                        Height = buttonHeight,
                    };

                    _buttons.Add(button);
                    buttonX += buttonWidth + buttonPadding;
                    rawButton.Clicked += ButtonClicked;
                    avatarIndex++;
                }
            }

            _headerFont = Resources.Fonts["questionheader"];
            _drawing = new QFontDrawing();
            _renderOptions = new QFontRenderOptions()
            {
                UseDefaultBlendFunction = true,
                CharacterSpacing = 0.15f
            };

            // Draw hint text with drop shadow
            _drawing.ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(0.0f, InternalResolutionX, 0.0f, InternalResolutionY, -1.0f, 1.0f);
            _drawing.DrawingPrimitives.Clear();

            float sidePadding = 50;
            SizeF maxWidth = new SizeF(InternalResolutionX - (sidePadding * 2), -1f);
            _drawing.Print(_headerFont, "CHOOSE YOUR FIGHTER", new Vector3(InternalResolutionX / 2, InternalResolutionY - sidePadding, 0), maxWidth, QFontAlignment.Centre, _renderOptions);

            _drawing.RefreshBuffers();
        }

        public override void MouseMoved(VirtualMousePosition args)
        {
            foreach (AvatarButton button in _buttons)
            {
                button.Button.MouseMoved(args);
            }
        }

        public override void MouseDown(VirtualMouseClick args)
        {
            foreach (AvatarButton button in _buttons)
            {
                button.Button.MouseDown(args);
            }
        }

        protected override void RenderInternal()
        {
            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            foreach (AvatarButton button in _buttons)
            {
                button.Button.Render();
            }

            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);
            GL.UseProgram(Resources.Shaders["default"].Handle);
            Matrix4 modelViewMatrix;

            foreach (AvatarButton button in _buttons)
            {
                modelViewMatrix = Matrix4.Identity;
                modelViewMatrix = Matrix4.CreateTranslation(button.X, InternalResolutionY - button.Y - button.Height, 0.0f) * modelViewMatrix;

                GL.Uniform1(GL.GetUniformLocation(Resources.Shaders["default"].Handle, "textureImage"), 0);
                GL.UniformMatrix4(GL.GetUniformLocation(Resources.Shaders["default"].Handle, "projectionMatrix"), false, ref _projectionMatrix);
                GL.UniformMatrix4(GL.GetUniformLocation(Resources.Shaders["default"].Handle, "modelViewMatrix"), false, ref modelViewMatrix);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, button.Texture.Handle);

                GL.Begin(PrimitiveType.Quads);
                {
                    GL.TexCoord2(1.0f, 1.0f);
                    GL.Vertex2(button.Width, 0);
                    GL.TexCoord2(1.0f, 0.0f);
                    GL.Vertex2(button.Width, button.Height);
                    GL.TexCoord2(0.0f, 0.0f);
                    GL.Vertex2(0, button.Height);
                    GL.TexCoord2(0.0f, 1.0f);
                    GL.Vertex2(0, 0);
                }
                GL.End();
            }

            GL.Disable(EnableCap.Texture2D);

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

        private void ButtonClicked(object source, ButtonPressedEventArgs args)
        {
            _chosenAvatar = args.SourceButtonId;
            _finished = true;
        }

        public string ReturnVal => _chosenAvatar;
    }
}
