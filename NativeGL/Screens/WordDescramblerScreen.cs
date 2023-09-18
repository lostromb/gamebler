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
using Durandal.Common.Utils;

namespace NativeGL.Screens
{
    public class WordDescramblerScreen : GameScreen
    {
        private QFont _questionFont;
        private QFont _headerFont;
        private QFontDrawing _drawing;
        private QFontRenderOptions _renderOptions;
        private QFontRenderOptions _monoRenderOptions;

        private bool _finished = false;
        
        private double _msSinceLastChange = CHANGE_INTERVAL;
        private double _msElapsed = 0;
        private const double CHANGE_INTERVAL = 500;
        private const double TARGET_SOLVE_TIME_MS = 30000;

        private int _currentWordLetterCount = 0;
        private string _currentWord = string.Empty;
        private string _currentDisplayWord = string.Empty;
        private int _currentWordScrambleCount = 0;

        protected override void InitializeInternal()
        {
            if (GameState.WordDescramberWords.Count > 0)
            {
                // Select a random one and play it
                string wordToDisplay = GameState.WordDescramberWords[new Random().Next(0, GameState.WordDescramberWords.Count)];
                GameState.WordDescramberWords.Remove(wordToDisplay);
                InitializeWord(wordToDisplay);
            }
            else
            {
                _finished = true;
            }

            _headerFont = Resources.Fonts["questionheader"];
            _questionFont = Resources.Fonts["monospace"];

            _drawing = new QFontDrawing();

            _renderOptions = new QFontRenderOptions()
            {
                UseDefaultBlendFunction = true,
                CharacterSpacing = 0.15f
            };

            _monoRenderOptions = new QFontRenderOptions()
            {
                UseDefaultBlendFunction = true,
                CharacterSpacing = 0.40f,
                Monospacing = QFontMonospacing.Yes,
                LineSpacing = 1.9f
            };
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
            if (_finished)
            {
                return;
            }

            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            
            _drawing.ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(0.0f, InternalResolutionX, 0.0f, InternalResolutionY, -1.0f, 1.0f);
            _drawing.DrawingPrimitives.Clear();

            float sidePadding = 50;
            SizeF maxWidth = new SizeF(InternalResolutionX - (sidePadding * 2), -1f);
            _drawing.Print(_headerFont, "FILL IN THE BLANKS", new Vector3(InternalResolutionX / 2, InternalResolutionY - sidePadding, 0), maxWidth, QFontAlignment.Centre, _renderOptions);
            _drawing.Print(_questionFont, _currentDisplayWord, new Vector3(InternalResolutionX / 2, InternalResolutionY - 250, 0), maxWidth, QFontAlignment.Centre, _monoRenderOptions);
            _drawing.RefreshBuffers();

            _drawing.Draw();
        }

        public override void Logic(double msElapsed)
        {
            _msElapsed += msElapsed;
            _msSinceLastChange += msElapsed;
            // See if we need to change the word
            if (_msSinceLastChange > CHANGE_INTERVAL)
            {
                _msSinceLastChange -= CHANGE_INTERVAL;
                Random rand = new Random();
                double percentScramble = Math.Max(0, (TARGET_SOLVE_TIME_MS - _msElapsed) / TARGET_SOLVE_TIME_MS);
                int charsToScramble = (int)Math.Round(_currentWordLetterCount * percentScramble);

                while (_currentWordScrambleCount > charsToScramble)
                {
                    int idx;
                    do
                    {
                        idx = rand.Next(0, _currentDisplayWord.Length);
                    } while (_currentDisplayWord[idx] != '_');
                    _currentDisplayWord = _currentDisplayWord.Substring(0, idx) + _currentWord[idx] + _currentDisplayWord.Substring(idx + 1);
                    _currentWordScrambleCount--;
                }
            }
        }

        public override bool Finished
        {
            get
            {
                return _finished;
            }
        }

        private void InitializeWord(string word)
        {
            _currentWord = word;
            _currentWordLetterCount = LetterCount(_currentWord);
            _currentWordScrambleCount = _currentWordLetterCount;
            StringBuilder b = new StringBuilder(_currentWord.Length);
            for (int c = 0; c < _currentWord.Length; c++)
            {
                if (char.IsWhiteSpace(word[c]))
                {
                    b.Append(' ');
                }
                else
                {
                    b.Append('_');
                }
            }

            _currentDisplayWord = b.ToString();
        }

        private int LetterCount(string word)
        {
            int returnVal = 0;
            for (int c = 0; c < word.Length; c++)
            {
                if (!char.IsWhiteSpace(word[c]))
                {
                    returnVal++;
                }
            }

            return returnVal;
        }
    }
}
