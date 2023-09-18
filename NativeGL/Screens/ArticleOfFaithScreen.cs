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
    public class ArticleOfFaithScreen : GameScreen
    {
        private QFont _questionFont;
        private QFont _headerFont;
        private QFontDrawing _drawing;
        private QFontRenderOptions _renderOptions;

        private bool _configured = false;
        private ArticleOfFaith _currentQuestion;

        private bool _finished = false;

        protected override void InitializeInternal()
        {
            _headerFont = Resources.Fonts["questionheader"];
            _questionFont = Resources.Fonts["default"];

            _drawing = new QFontDrawing();

            _renderOptions = new QFontRenderOptions()
            {
                UseDefaultBlendFunction = true,
                CharacterSpacing = 0.15f
            };

            // Determine the difficulty of questions that are left
            Difficulty availableDifficulties = Difficulty.NotSet;
            foreach (ArticleOfFaith question in GameState.ArticlesOfFaith)
            {
                availableDifficulties = availableDifficulties | question.Difficulty;
            }

            if (availableDifficulties == Difficulty.NotSet)
            {
                // No questions to ask. Finish this screen immediately.
                _finished = true;
            }
            else
            {
                // Then push a difficulty selector on the view stack
                EnqueueScreen(new DifficultySelectScreen(availableDifficulties));
            }
        }

        public override void KeyDown(KeyboardKeyEventArgs args)
        {
            if (args.Key == OpenTK.Input.Key.BackSpace)
            {
                Resources.AudioSubsystem.StopMusic();
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

            if (!_configured)
            {
                return;
            }

            _drawing.ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(0.0f, InternalResolutionX, 0.0f, InternalResolutionY, -1.0f, 1.0f);
            _drawing.DrawingPrimitives.Clear();

            StringBuilder builder = new StringBuilder();
            builder.Append(_currentQuestion.QuestionText);

            int difficultyScore = 0;
            if (_currentQuestion.Difficulty == Difficulty.Level1)
            {
                difficultyScore = 100;
            }
            else if (_currentQuestion.Difficulty == Difficulty.Level2)
            {
                difficultyScore = 200;
            }
            else if (_currentQuestion.Difficulty == Difficulty.Level3)
            {
                difficultyScore = 300;
            }
            else if (_currentQuestion.Difficulty == Difficulty.Level4)
            {
                difficultyScore = 400;
            }

            float sidePadding = 50;
            SizeF maxWidth = new SizeF(InternalResolutionX - (sidePadding * 2), -1f);
            _drawing.Print(_headerFont, difficultyScore + " POINT QUESTION", new Vector3(InternalResolutionX / 2, InternalResolutionY - sidePadding, 0), maxWidth, QFontAlignment.Centre, _renderOptions);
            _drawing.Print(_questionFont, builder.ToString(), new Vector3(sidePadding, InternalResolutionY - 250, 0), maxWidth, QFontAlignment.Justify, _renderOptions);
            _drawing.RefreshBuffers();

            _drawing.Draw();
        }

        public override void Logic(double msElapsed)
        {
            if (!_configured)
            {
                return;
            }
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
            if (aboveScreen is DifficultySelectScreen)
            {
                Difficulty desiredDifficulty = ((DifficultySelectScreen)aboveScreen).ReturnVal;

                // Select a random question with the configured difficulty
                List<ArticleOfFaith> candidates = new List<ArticleOfFaith>();
                foreach (ArticleOfFaith question in GameState.ArticlesOfFaith)
                {
                    if (question.Difficulty == desiredDifficulty)
                    {
                        candidates.Add(question);
                    }
                }

                _currentQuestion = candidates[new Random().Next(0, candidates.Count)];
                GameState.ArticlesOfFaith.Remove(_currentQuestion);
                Resources.AudioSubsystem.PlayMusic(_currentQuestion.MusicName);

                _configured = true;
            }
        }
    }
}
