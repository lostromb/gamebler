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
//    public class PictureQuizScreen : GameScreen
//    {
//        private Matrix4 _projectionMatrix;
//        private bool _finished = false;
//        private GLTexture _imageTexture;

//        private bool _configured = false;
//        private PictureQuizQuestion _currentQuestion;
        
//        private QFont _questionFont;
//        private QFontDrawing _drawing;
//        private QFontRenderOptions _renderOptions;

//        protected override void InitializeInternal()
//        {
//            _projectionMatrix = Matrix4.CreateOrthographicOffCenter(0, InternalResolutionX, InternalResolutionY, 0, -1.0f, 1.0f);

//            // Determine the difficulty of questions that are left
//            Difficulty availableDifficulties = Difficulty.NotSet;
//            foreach (PictureQuizQuestion question in GameState.PictureQuizQuestions)
//            {
//                availableDifficulties = availableDifficulties | question.Difficulty;
//            }

//            if (availableDifficulties == Difficulty.NotSet)
//            {
//                // No questions to ask. Finish this screen immediately.
//                _finished = true;
//            }
//            else
//            {
//                // Then push a difficulty selector on the view stack
//                EnqueueScreen(new DifficultySelectScreen(availableDifficulties));
//            }

//            _questionFont = Resources.Fonts["default_80pt"];
//            _drawing = new QFontDrawing();
//            _renderOptions = new QFontRenderOptions()
//            {
//                UseDefaultBlendFunction = true,
//                CharacterSpacing = 0.15f
//            };
//            _drawing.ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(0.0f, InternalResolutionX, 0.0f, InternalResolutionY, -1.0f, 1.0f);
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
//            Matrix4 modelViewMatrix = Matrix4.CreateTranslation((float)InternalResolutionX * 3 / 4, (float)InternalResolutionY / 2, 0.0f);

//            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
//            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

//            if (!_configured)
//            {
//                return;
//            }

//            GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);
//            GL.Enable(EnableCap.Texture2D);
//            int program = Resources.Shaders["default"].Handle;
//            GL.UseProgram(program);
//            GL.Uniform1(GL.GetUniformLocation(program, "textureImage"), 0);
//            GL.UniformMatrix4(GL.GetUniformLocation(program, "projectionMatrix"), false, ref _projectionMatrix);
//            GL.UniformMatrix4(GL.GetUniformLocation(program, "modelViewMatrix"), false, ref modelViewMatrix);
//            GL.ActiveTexture(TextureUnit.Texture0);
//            GL.BindTexture(TextureTarget.Texture2D, _imageTexture.Handle);

//            //GL.Enable(EnableCap.CullFace);
//            //GL.CullFace(CullFaceMode.Back);

//            // Stretch to either the sides or top based on the calculated aspect ratio
//            float frameAspectRatio = ((float)InternalResolutionX / 2) / (float)InternalResolutionY;
//            float height;
//            float width;

//            if (_imageTexture.AspectRatio > frameAspectRatio)
//            {
//                // Wide image
//                width = (float)InternalResolutionX / 4;
//                height = (float)InternalResolutionX / 4 / _imageTexture.AspectRatio;
//            }
//            else
//            {
//                // Tall image
//                width = (float)InternalResolutionY / 2 * _imageTexture.AspectRatio;
//                height = (float)InternalResolutionY / 2;
//            }

//            GL.Begin(PrimitiveType.Quads);
//            {
//                GL.TexCoord2(1.0f, 0.0f);
//                GL.Vertex2(width, 0 - height);
//                GL.TexCoord2(1.0f, 1.0f);
//                GL.Vertex2(width, height);
//                GL.TexCoord2(0.0f, 1.0f);
//                GL.Vertex2(0 - width, height);
//                GL.TexCoord2(0.0f, 0.0f);
//                GL.Vertex2(0 - width, 0 - height);
//            }
//            GL.End();

//            GL.ActiveTexture(TextureUnit.Texture0);

//            _drawing.DrawingPrimitives.Clear();

//            float sidePadding = 50;
//            SizeF maxWidth = new SizeF((InternalResolutionX / 2) - (sidePadding * 2), -1f);
//            _drawing.Print(_questionFont, _currentQuestion.QuestionText, new Vector3(sidePadding, InternalResolutionY - sidePadding, 0), maxWidth, QFontAlignment.Left, _renderOptions);
//            _drawing.RefreshBuffers();
//            _drawing.Draw();
//        }

//        public override void Logic(double msElapsed)
//        {
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
//                List<PictureQuizQuestion> candidates = new List<PictureQuizQuestion>();
//                foreach (PictureQuizQuestion question in GameState.PictureQuizQuestions)
//                {
//                    if (question.Difficulty == desiredDifficulty)
//                    {
//                        candidates.Add(question);
//                    }
//                }

//                _currentQuestion = candidates[new Random().Next(0, candidates.Count)];
//                GameState.PictureQuizQuestions.Remove(_currentQuestion);
//                _imageTexture = Resources.Textures[_currentQuestion.ImageName];

//                _configured = true;
//            }
//        }
//    }
//}
