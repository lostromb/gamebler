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
    public class NameEntryScreen : GameScreen
    {
        private QFont _entryFont;
        private QFont _titleFont;
        private QFontDrawing _drawing;
        private QFontRenderOptions _renderOptions;

        private StringBuilder _currentString = new StringBuilder();
        private bool _entryFinished = false;
        private string _prompt;

        public NameEntryScreen(string prompt)
        {
            _prompt = prompt;
        }

        protected override void InitializeInternal()
        {
            QFontBuilderConfiguration builderConfig = new QFontBuilderConfiguration(true)
            {
                TextGenerationRenderHint = TextGenerationRenderHint.AntiAlias | TextGenerationRenderHint.AntiAliasGridFit,
                Characters = CharacterSet.BasicSet
            };

            _entryFont = Resources.Fonts["default"];
            _titleFont = Resources.Fonts["questionheader"];

            _drawing = new QFontDrawing();
            
            _renderOptions = new QFontRenderOptions()
                {
                    UseDefaultBlendFunction = true
                };
        }

        public override void KeyDown(KeyboardKeyEventArgs args)
        {
            if (args.Key == OpenTK.Input.Key.BackSpace)
            {
                if (_currentString.Length > 0)
                {
                    _currentString.Remove(_currentString.Length - 1, 1);
                }
            }
            if (args.Key == OpenTK.Input.Key.Enter)
            {
                _entryFinished = true;
            }
        }
        
        public override void KeyTyped(KeyPressEventArgs args)
        {
            _currentString.Append(args.KeyChar);
        }

        protected override void RenderInternal()
        {
            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _drawing.ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(0.0f, InternalResolutionX, 0.0f, InternalResolutionY, -1.0f, 1.0f);
            _drawing.DrawingPrimitives.Clear();

            _drawing.Print(_entryFont, _currentString.ToString() + "|", new Vector3(50, 650, 0), QFontAlignment.Left, _renderOptions);
            _drawing.Print(_titleFont, _prompt, new Vector3(960, 800, 0), new SizeF(1820, -1), QFontAlignment.Centre, _renderOptions);
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
                return _entryFinished;
            }
        }

        public void ClickedEnter(object source, EventArgs args)
        {
            _entryFinished = true;
        }

        public string ReturnVal
        {
            get
            {
                return _currentString.ToString();
            }
        }
    }
}
