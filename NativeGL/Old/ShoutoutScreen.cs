//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows.Input;
//using OpenTK.Graphics.OpenGL;
//using QuickFont;
//using QuickFont.Configuration;
//using OpenTK;
//using System.Drawing;
//using NativeGL.Structures;
//using OpenTK.Input;
//using Durandal.Common.Speech.Triggers;
//using Durandal.Common.Audio;

//namespace NativeGL.Screens
//{
//    public class ShoutoutScreen : GameScreen
//    {

//        private QFont _questionFont;
//        private QFontDrawing _drawing;
//        private QFontRenderOptions _renderOptions;

//        private Matrix4 projectionMatrix;
//        private bool _finished = false;

//        private Queue<string> _words;
//        private string _currentWord;
//        private int _currentPoints;
//        private double _timeLeftMs = 0;
//        private bool _gameOn = false;

//        protected override void InitializeInternal()
//        {
//            projectionMatrix = Matrix4.CreateOrthographicOffCenter(0, InternalResolutionX, InternalResolutionY, 0, -1.0f, 1.0f);
//            _currentPoints = 0;
//            _timeLeftMs = 15000;
//            _gameOn = false;

//            // Pick a random word list
//            _words = new Queue<string>();
//            if (GameState.Shoutouts.Count > 0)
//            {
//                // Select a random one and play it
//                int idx = new Random().Next(0, GameState.Shoutouts.Count);
//                List<string> wordList = GameState.Shoutouts[idx];
//                GameState.Shoutouts.RemoveAt(idx);
//                foreach (string c in wordList)
//                {
//                    _words.Enqueue(c);
//                }

//                NextWord();
//                Resources.Microphone.ClearBuffers();
//            }
//            else
//            {
//                _finished = true;
//            }

//            _gameOn = true;
//            _questionFont = Resources.Fonts["default"];
//            _drawing = new QFontDrawing();
//            _renderOptions = new QFontRenderOptions()
//            {
//                UseDefaultBlendFunction = true,
//                CharacterSpacing = 0.15f
//            };
//            _drawing.ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(0.0f, InternalResolutionX, 0.0f, InternalResolutionY, -1.0f, 1.0f);
//            _drawing.DrawingPrimitives.Clear();

//            float sidePadding = 50;
//            SizeF maxWidth = new SizeF(InternalResolutionX - (sidePadding * 2), -1f);
//            //_drawing.Print(_questionFont, "Say " + _currentWord, new Vector3(InternalResolutionX / 2, InternalResolutionY - sidePadding, 0), maxWidth, QFontAlignment.Centre, _renderOptions);
//            _drawing.RefreshBuffers();
//        }

//        public override void KeyDown(KeyboardKeyEventArgs args)
//        {
//            if (args.Key == OpenTK.Input.Key.BackSpace)
//            {
//                _finished = true;
//            }
//        }

//        protected override void RenderInternal()
//        {
//            Matrix4 modelViewMatrix = Matrix4.Identity;

//            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
//            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

//            _drawing.DrawingPrimitives.Clear();
//            int secondsLeft = (int)Math.Ceiling(_timeLeftMs / 1000);
//            float sidePadding = 50;
//            SizeF maxWidth = new SizeF(InternalResolutionX - (sidePadding * 2), -1f);
//            if (_gameOn)
//            {
//                if (string.IsNullOrEmpty(_currentWord))
//                {
//                    _drawing.Print(_questionFont, "Awesome!\n\nTime left: " + secondsLeft + "\nYour points: " + _currentPoints, new Vector3(InternalResolutionX / 2, InternalResolutionY / 2, 0), maxWidth, QFontAlignment.Centre, _renderOptions);
//                }
//                else
//                {
//                    _drawing.Print(_questionFont, "Say " + _currentWord + "\n\nTime left: " + secondsLeft + "\nYour points: " + _currentPoints, new Vector3(InternalResolutionX / 2, InternalResolutionY / 2, 0), maxWidth, QFontAlignment.Centre, _renderOptions);
//                }
//            }
//            else
//            {
//                _drawing.Print(_questionFont, "Time's up!\n\nTime left: " + secondsLeft + "\nYour points: " + _currentPoints, new Vector3(InternalResolutionX / 2, InternalResolutionY / 2, 0), maxWidth, QFontAlignment.Centre, _renderOptions);
//            }
//            _drawing.RefreshBuffers();
//            _drawing.Draw();
//        }

//        public override void Logic(double msElapsed)
//        {
//            AudioChunk audio = Resources.Microphone.ReadMicrophone(TimeSpan.FromMilliseconds(msElapsed));
//            AudioTriggerResult triggerResult = Resources.KeywordSpotter.SendAudio(audio);
//            if (_gameOn && triggerResult.Triggered)
//            {
//                _currentPoints += 50;
//                NextWord();
//            }

//            if (_gameOn)
//            {
//                _timeLeftMs -= msElapsed;
//                if (_timeLeftMs <= 0)
//                {
//                    _gameOn = false;
//                    _timeLeftMs = 0;
//                }
//            }
//        }

//        private void NextWord()
//        {
//            if (_words.Count == 0)
//            {
//                _currentWord = string.Empty;
//                Resources.KeywordSpotter.Configure(new KeywordSpottingConfiguration()
//                {
//                    PrimaryKeyword = "FULMINATE",
//                    PrimaryKeywordSensitivity = 1
//                });
//            }
//            else
//            {
//                _currentWord = _words.Dequeue();
//                Resources.KeywordSpotter.Configure(new KeywordSpottingConfiguration()
//                {
//                    PrimaryKeyword = _currentWord,
//                    PrimaryKeywordSensitivity = 5
//                });
//            }
//        }

//        public override bool Finished
//        {
//            get
//            {
//                return _finished;
//            }
//        }
//    }
//}
