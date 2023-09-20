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
    public class QuizzlerScreen : GameScreen
    {
        private QFont _headerFont;
        private QFontDrawing _drawing;
        private QFontRenderOptions _renderOptions;

        private bool _finished = false;
        private List<QuizzlerButton> _buttons;
        private Matrix4 _projectionMatrix;

        private class QuizzlerButton
        {
            public GLButton Button;
            public int QuestionId;
            public float X;
            public float Y;
            public float Width;
            public float Height;
            public ButtonState State;
        }

        private enum ButtonState
        {
            Neutral,
            Correct,
            Incorrect
        }

        public QuizzlerScreen()
        {
        }

        protected override void InitializeInternal()
        {
            _projectionMatrix = Matrix4.CreateOrthographicOffCenter(0, InternalResolutionX, 0, InternalResolutionY, -1.0f, 1.0f);
            _buttons = new List<QuizzlerButton>();

            // Select some questions
            QuizzlerQuestion[] allQuestions = new List<QuizzlerQuestion>(GameState.QuizQuestions).ToArray();
            Random rand = new Random();

            // Bubble shuffle the list
            for (int c = 0; c < 1000; c++)
            {
                int src = rand.Next(0, allQuestions.Length);
                int dst = rand.Next(0, allQuestions.Length);
                QuizzlerQuestion tmp = allQuestions[src];
                allQuestions[src] = allQuestions[dst];
                allQuestions[dst] = tmp;
            }

            // Present some to select in three rows

            const int rows = 3;
            const int columns = 4;
            float buttonHeight = 200;
            float buttonWidth = 350;
            float buttonPadding = 20;
            float totalColumnWidth = (buttonWidth * columns) + (buttonPadding * columns) - buttonPadding;
            int questionIndex = 0;

            for (int row = 0; row < rows; row++)
            {
                float buttonY = 300 + ((buttonHeight + buttonPadding) * row);
                float buttonX = (InternalResolutionX - totalColumnWidth) / 2;
                for (int column = 0; column < columns; column++)
                {
                    if (questionIndex >= allQuestions.Length)
                    {
                        continue;
                    }

                    QuizzlerQuestion q = allQuestions[questionIndex];
                    GLButton rawButton = new GLButton(
                        Resources, buttonX, buttonY, buttonWidth, buttonHeight, q.Category, q.Id.ToString(), Resources.Fonts["default_20pt"]);
                    QuizzlerButton button = new QuizzlerButton()
                    {
                        Button = rawButton,
                        QuestionId = q.Id,
                        X = buttonX,
                        Y = buttonY,
                        Width = buttonWidth,
                        Height = buttonHeight,
                    };

                    _buttons.Add(button);
                    buttonX += buttonWidth + buttonPadding;
                    rawButton.Clicked += ButtonClicked;
                    questionIndex++;
                }
            }

            _headerFont = Resources.Fonts["questionheader"];
            _drawing = new QFontDrawing();
            _renderOptions = new QFontRenderOptions()
            {
                UseDefaultBlendFunction = true,
                CharacterSpacing = 0.15f
            };

            _drawing.ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(0.0f, InternalResolutionX, 0.0f, InternalResolutionY, -1.0f, 1.0f);
            _drawing.DrawingPrimitives.Clear();

            float sidePadding = 50;
            SizeF maxWidth = new SizeF(InternalResolutionX - (sidePadding * 2), -1f);
            _drawing.Print(_headerFont, "QUIZZLER", new Vector3(InternalResolutionX / 2, InternalResolutionY - sidePadding, 0), maxWidth, QFontAlignment.Centre, _renderOptions);

            _drawing.RefreshBuffers();
        }

        public override void MouseMoved(VirtualMousePosition args)
        {
            foreach (QuizzlerButton button in _buttons)
            {
                button.Button.MouseMoved(args);
            }
        }

        public override void MouseDown(VirtualMouseClick args)
        {
            foreach (QuizzlerButton button in _buttons)
            {
                button.Button.MouseDown(args);
            }
        }

        protected override void RenderInternal()
        {
            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);
            GL.UseProgram(Resources.Shaders["solidcolor"].Handle);
            Matrix4 modelViewMatrix;

            foreach (QuizzlerButton button in _buttons)
            {
                modelViewMatrix = Matrix4.Identity;
                modelViewMatrix = Matrix4.CreateTranslation(button.X, InternalResolutionY - button.Y - button.Height, 0.0f) * modelViewMatrix;

                GL.UniformMatrix4(GL.GetUniformLocation(Resources.Shaders["solidcolor"].Handle, "projectionMatrix"), false, ref _projectionMatrix);
                GL.UniformMatrix4(GL.GetUniformLocation(Resources.Shaders["solidcolor"].Handle, "modelViewMatrix"), false, ref modelViewMatrix);

                switch (button.State)
                {
                    case ButtonState.Neutral:
                        GL.Color4(0.0f, 0.0, 0.0f, 0.1f);
                        break;
                    case ButtonState.Correct:
                        GL.Color4(0.0f, 1.0, 0.0f, 1.0f);
                        break;
                    case ButtonState.Incorrect:
                        GL.Color4(1.0f, 0.0, 0.0f, 1.0f);
                        break;
                }

                GL.Begin(PrimitiveType.Quads);
                {
                    GL.Vertex2(button.Width, 0);
                    GL.Vertex2(button.Width, button.Height);
                    GL.Vertex2(0, button.Height);
                    GL.Vertex2(0, 0);
                }
                GL.End();
            }

            foreach (QuizzlerButton button in _buttons)
            {
                button.Button.Render();
            }

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

        public override void ScreenAboveFinished(GameScreen aboveScreen)
        {
            QuizzlerQuestionScreen screen = aboveScreen as QuizzlerQuestionScreen;

            int questionId = screen.QuestionId;
            QuizzlerButton button = _buttons.Single((s) => s.QuestionId == questionId);

            // Update the state of the button
            if (screen.WasCorrect)
            {
                button.State = ButtonState.Correct;
            }
            else
            {
                button.State = ButtonState.Incorrect;
            }

            // Remove that question from the rotation
            GameState.QuizQuestions = GameState.QuizQuestions.Where((s) => s.Id != questionId).ToList();
        }

        public override void KeyDown(KeyboardKeyEventArgs args)
        {
            base.KeyDown(args);

            if (args.Key == OpenTK.Input.Key.BackSpace)
            {
                _finished = true;
            }
        }

        private void ButtonClicked(object source, ButtonPressedEventArgs args)
        {
            // Make sure the button is enabled
            int questionId = int.Parse(args.SourceButtonId);
            QuizzlerButton button = _buttons.Single((s) => s.QuestionId == questionId);
            if (button.State != ButtonState.Neutral)
            {
                return;
            }
            else
            {
                QuizzlerQuestion question = GameState.QuizQuestions.Single((s) => s.Id == questionId);
                EnqueueScreen(new QuizzlerQuestionScreen(question));
            }
        }
    }
}
