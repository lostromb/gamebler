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

//namespace NativeGL.Screens
//{
//    public class StorytimeScreen : GameScreen
//    {
//        private QFont _questionFont;
//        private QFont _headerFont;
//        private QFontDrawing _drawing;
//        private QFontRenderOptions _renderOptions;

//        private bool _configured = false;
//        private bool _finished = false;

//        protected override void InitializeInternal()
//        {
//            _headerFont = Resources.Fonts["questionheader"];
//            _questionFont = Resources.Fonts["default"];

//            _drawing = new QFontDrawing();

//            _renderOptions = new QFontRenderOptions()
//            {
//                UseDefaultBlendFunction = true,
//                CharacterSpacing = 0.15f
//            };
            
//            EnqueueScreen(new DifficultySelectScreen(Difficulty.All));
//        }

//        public override void KeyDown(KeyboardKeyEventArgs args)
//        {
//            if (args.Key == OpenTK.Input.Key.BackSpace)
//            {
//                _finished = true;
//            }
//        }

//        public override void KeyTyped(KeyPressEventArgs args)
//        {
//        }

//        protected override void RenderInternal()
//        {
//            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
//            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

//            if (!_configured)
//            {
//                return;
//            }

//            _drawing.ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(0.0f, InternalResolutionX, 0.0f, InternalResolutionY, -1.0f, 1.0f);
//            _drawing.DrawingPrimitives.Clear();
            
//            float sidePadding = 50;
//            SizeF maxWidth = new SizeF(InternalResolutionX - (sidePadding * 2), -1f);
//            _drawing.Print(_headerFont, "STORYTIME", new Vector3(InternalResolutionX / 2, InternalResolutionY - sidePadding, 0), maxWidth, QFontAlignment.Centre, _renderOptions);
//            _drawing.Print(_questionFont, "Tell a gospel story in your own words without naming any characters. Points are awarded if the class can correctly guess which story it is. Teacher will provide the prompt", new Vector3(sidePadding, InternalResolutionY - 250, 0), maxWidth, QFontAlignment.Justify, _renderOptions);
//            _drawing.RefreshBuffers();

//            _drawing.Draw();
//        }

//        public override void Logic(double msElapsed)
//        {
//            if (!_configured)
//            {
//                return;
//            }
//        }

//        public override bool Finished
//        {
//            get
//            {
//                return _finished;
//            }
//        }

//        public override void ScreenAboveFinished(GameScreen aboveScreen)
//        {
//            if (aboveScreen is DifficultySelectScreen)
//            {
//                Difficulty desiredDifficulty = ((DifficultySelectScreen)aboveScreen).ReturnVal;

//                // Select a random question with the configured difficulty
//                List<PrincipleQuestion> candidates = new List<PrincipleQuestion>();
//                foreach (PrincipleQuestion question in GameState.GospelPrincipleQuestions)
//                {
//                    if (question.Difficulty == desiredDifficulty)
//                    {
//                        candidates.Add(question);
//                    }
//                }

//                _configured = true;
//            }
//        }
//    }
//}
