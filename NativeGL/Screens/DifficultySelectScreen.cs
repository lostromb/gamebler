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
    public class DifficultySelectScreen : GameScreen
    {
        private bool _finished = false;
        private Difficulty _availableDifficulties;
        private List<GLButton> _buttons;
        
        public DifficultySelectScreen(Difficulty availableDifficulties)
        {
            _availableDifficulties = availableDifficulties;
        }

        public Difficulty ReturnVal
        {
            get; private set;
        }

        protected override void InitializeInternal()
        {
            _buttons = new List<GLButton>();
            uint flag = (uint)Difficulty.Level1;
            float buttonHeight = 200;
            float buttonWidth = 500;
            float buttonPadding = 20;
            float buttonX = (InternalResolutionX - buttonWidth) / 2;
            float buttonY = (InternalResolutionY - ((buttonHeight * 4) + (buttonPadding * 3))) / 2;
            for (int c = 0; c < 4; c++)
            {
                GLButton newButton = new GLButton(Resources, buttonX, buttonY, buttonWidth, buttonHeight, ((c + 1) * 100).ToString() + " points", c.ToString());
                newButton.Enabled = _availableDifficulties.HasFlag((Difficulty)flag);
                flag = flag << 1;
                _buttons.Add(newButton);
                buttonY += buttonHeight + buttonPadding;
                newButton.Clicked += ButtonClicked;
            }
        }

        public override void KeyDown(KeyboardKeyEventArgs args)
        {
            //if (args.Key == OpenTK.Input.Key.BackSpace)
            //{
            //    _finished = true;
            //}
        }

        public override void KeyTyped(KeyPressEventArgs args)
        {
        }

        public override void MouseMoved(VirtualMousePosition args)
        {
            foreach (GLButton button in _buttons)
            {
                button.MouseMoved(args);
            }
        }

        public override void MouseDown(VirtualMouseClick args)
        {
            foreach (GLButton button in _buttons)
            {
                button.MouseDown(args);
            }
        }

        protected override void RenderInternal()
        {
            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            foreach (GLButton button in _buttons)
            {
                button.Render();
            }
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
            if (args.SourceButtonId == "0")
            {
                ReturnVal = Difficulty.Level1;
            }
            else if (args.SourceButtonId == "1")
            {
                ReturnVal = Difficulty.Level2;
            }
            else if (args.SourceButtonId == "2")
            {
                ReturnVal = Difficulty.Level3;
            }
            else if (args.SourceButtonId == "3")
            {
                ReturnVal = Difficulty.Level4;
            }

            _finished = true;
        }
    }
}
