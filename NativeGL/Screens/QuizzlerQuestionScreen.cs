using NativeGL.Structures;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using QuickFont;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace NativeGL.Screens
{
    public class QuizzlerQuestionScreen : GameScreen
    {
        private Matrix4 _projectionMatrix;
        private readonly QuizzlerQuestion _question;
        private List<QuizzlerButton> _buttons;

        private class QuizzlerButton
        {
            public GLButton Button;
            public bool IsCorrectAnswer;
            public float X;
            public float Y;
            public float Width;
            public float Height;
        }

        private static readonly TimeSpan QUESTION_TIME = TimeSpan.FromSeconds(21);
        private DateTimeOffset _screenStartTime;
        private bool _showingAnswers = false;
        private bool _finished = false;
        private QFont _headerFont;
        private QFont _buttonFont;
        private QFont _questionFont;
        private QFontDrawing _drawing;
        private QFontRenderOptions _renderOptions;

        public QuizzlerQuestionScreen(QuizzlerQuestion question)
        {
            _question = question;
            QuestionId = question.Id;
            _finished = false;
        }

        protected override void InitializeInternal()
        {
            _projectionMatrix = Matrix4.CreateOrthographicOffCenter(0, InternalResolutionX, 0, InternalResolutionY, -1.0f, 1.0f);
            _buttons = new List<QuizzlerButton>();

            _headerFont = Resources.Fonts["questionheader"];
            _drawing = new QFontDrawing();
            _renderOptions = new QFontRenderOptions()
            {
                UseDefaultBlendFunction = true,
                CharacterSpacing = 0.15f
            };

            _buttonFont = Resources.Fonts["default_20pt"];
            _questionFont = Resources.Fonts["default_40pt"];
            _drawing = new QFontDrawing();
            _renderOptions = new QFontRenderOptions()
            {
                UseDefaultBlendFunction = true,
                CharacterSpacing = 0.15f
            };
            _drawing.ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(0.0f, InternalResolutionX, 0.0f, InternalResolutionY, -1.0f, 1.0f);

            List<Tuple<string, bool>> responsesList = new List<Tuple<string, bool>>();
            responsesList.Add(new Tuple<string, bool>(_question.Answer, true));
            foreach (var incorrect in _question.Incorrect)
            {
                responsesList.Add(new Tuple<string, bool>(incorrect, false));
            }

            Tuple<string, bool>[] allResponses = responsesList.ToArray();
            Random rand = new Random();

            // Bubble shuffle the list
            for (int c = 0; c < 100; c++)
            {
                int src = rand.Next(0, allResponses.Length);
                int dst = rand.Next(0, allResponses.Length);
                Tuple<string, bool> tmp = allResponses[src];
                allResponses[src] = allResponses[dst];
                allResponses[dst] = tmp;
            }

            // Create buttons for all the answers

            float buttonHeight = 100;
            float buttonWidth = 800;
            float buttonPadding = 20;

            for (int row = 0; row < allResponses.Length; row++)
            {
                float buttonY = 300 + ((buttonHeight + buttonPadding) * row);
                float buttonX = 50;

                string currentPrompt = allResponses[row].Item1;
                bool isCorrectPrompt = allResponses[row].Item2;
                GLButton rawButton = new GLButton(
                    Resources, buttonX, buttonY, buttonWidth, buttonHeight, currentPrompt, isCorrectPrompt.ToString(), _buttonFont);
                QuizzlerButton button = new QuizzlerButton()
                {
                    Button = rawButton,
                    IsCorrectAnswer = isCorrectPrompt,
                    X = buttonX,
                    Y = buttonY,
                    Width = buttonWidth,
                    Height = buttonHeight,
                };

                _buttons.Add(button);
                rawButton.Clicked += ButtonClicked;
            }

            _screenStartTime = DateTimeOffset.UtcNow;
        }

        public override void KeyDown(KeyboardKeyEventArgs args)
        {
            if (args.Key == OpenTK.Input.Key.BackSpace)
            {
                _finished = true;
            }
        }

        private void ButtonClicked(object source, ButtonPressedEventArgs args)
        {
            WasCorrect = bool.Parse(args.SourceButtonId);
            _showingAnswers = true;

            Resources.AudioSubsystem.PlaySound(Resources.SoundEffects["question_correct"]);
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

            if (_showingAnswers)
            {
                foreach (QuizzlerButton button in _buttons)
                {
                    modelViewMatrix = Matrix4.Identity;
                    modelViewMatrix = Matrix4.CreateTranslation(button.X, InternalResolutionY - button.Y - button.Height, 0.0f) * modelViewMatrix;

                    // Highlight correct and wrong answers
                    GL.UniformMatrix4(GL.GetUniformLocation(Resources.Shaders["solidcolor"].Handle, "projectionMatrix"), false, ref _projectionMatrix);
                    GL.UniformMatrix4(GL.GetUniformLocation(Resources.Shaders["solidcolor"].Handle, "modelViewMatrix"), false, ref modelViewMatrix);

                    switch (button.IsCorrectAnswer)
                    {
                        case true:
                            GL.Color4(0.0f, 1.0, 0.0f, 1.0f);
                            break;
                        case false:
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
            }

            foreach (QuizzlerButton button in _buttons)
            {
                button.Button.Render();
            }

            _drawing.DrawingPrimitives.Clear();

            float sidePadding = 50;
            SizeF maxWidth = new SizeF((InternalResolutionX / 2) - (sidePadding * 2), -1f);
            _drawing.Print(_headerFont, "QUIZZLER", new Vector3(InternalResolutionX / 2, InternalResolutionY - sidePadding, 0), maxWidth, QFontAlignment.Centre, _renderOptions);
            _drawing.Print(_questionFont, _question.QuestionText,
                new Vector3((InternalResolutionX + sidePadding) / 2, 800, 0), maxWidth, QFontAlignment.Left, _renderOptions);

            TimeSpan currentTime = QUESTION_TIME - (DateTimeOffset.UtcNow - _screenStartTime);
            int secondsRemaining = Math.Max(0, (int)Math.Ceiling(currentTime.TotalSeconds));
            _drawing.Print(_questionFont, "TIME: " + secondsRemaining,
                new Vector3(InternalResolutionX / 2, 100, 0), maxWidth, QFontAlignment.Centre, _renderOptions);

            _drawing.RefreshBuffers();
            _drawing.Draw();
        }

        public override void Logic(double msElapsed)
        {
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

        public override bool Finished
        {
            get
            {
                return _finished;
            }
        }

        public bool WasCorrect { get; private set; }
        public int QuestionId { get; private set; }
    }
}
