using NativeGL;
using NativeGL.Screens;
using OpenTK;
using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using QuickFont;
using QuickFont.Configuration;
using System.Drawing.Text;
using NativeGL.Structures;
using OpenTK.Input;
using Newtonsoft.Json;
using Durandal.Common.Utils;
using Durandal.Common.Logger;
using Durandal.Common.File;
using Durandal.Common.Speech.Triggers;
using Durandal.Common.Audio;
using Durandal.Common.Audio.Components;
using Durandal.Extensions.NAudio.Devices;
using Durandal.Common.Utils.NativePlatform;
using Durandal.Common.Time;
using Durandal.Common.Tasks;
using Durandal.Common.Audio.Codecs;
using Durandal.Common.Audio.Codecs.Opus;
using NativeGL.Audio;

namespace NativeGL
{
    public class MainWindow : GameWindow
    {
        private const int INTERNAL_RESOLUTION_X = 1920;
        private const int INTERNAL_RESOLUTION_Y = 1080;

        private ReaderWriterLockSlim _renderLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private List<GameScreen> _gameScreenStack;
        private StaticResources _staticResources;
        private GlobalGameState _gameState;
        private RateCounter _framerate = new RateCounter(TimeSpan.FromSeconds(2));
        private HighPrecisionTimer _frameTimer;
        private double _lastFrameTime;
        private Matrix4 _orthoProjectionMatrix = Matrix4.CreateOrthographicOffCenter(0, 1, 1, 0, -1.0f, 1.0f);
        private readonly ILogger _logger;

        private readonly Queue<GameScreen> _newScreens = new Queue<GameScreen>();
        private DateTime _lastFpsUpdate = DateTime.Now;

        public MainWindow() : base(960, 540, GraphicsMode.Default, "Gamebler", GameWindowFlags.Default, DisplayDevice.Default, 3, 0, GraphicsContextFlags.Default)
        {
            this.TargetRenderFrequency = 60;
            this.TargetUpdateFrequency = 60;
            _logger = new ConsoleLogger();

            _frameTimer = new HighPrecisionTimer();
            _frameTimer.Start();
            _lastFrameTime = _frameTimer.ElapsedMs;

            _staticResources = LoadStaticResources();

            _gameState = new GlobalGameState();

            _gameState.PictureQuizQuestions = JsonConvert.DeserializeObject<List<PictureQuizQuestion>>(File.ReadAllText(@".\Resources\Questions\PictureQuiz.json"));
            _gameState.MusicQuizSongs = JsonConvert.DeserializeObject<List<SoundTestPrompt>>(File.ReadAllText(@".\Resources\Questions\Songs.json"));
            _gameState.DescramblerImages = JsonConvert.DeserializeObject<List<DescramblerPrompt>>(File.ReadAllText(@".\Resources\Questions\Descrambler.json"));
            _gameState.WordDescramberWords = JsonConvert.DeserializeObject<List<WordDescramblerPrompt>>(File.ReadAllText(@".\Resources\Questions\WordDescrambler.json"));
           
            _staticResources.AudioSubsystem = new AudioSystem(_logger.Clone("AudioSystem"));

            _gameScreenStack = new List<GameScreen>();

            EnqueueNewScreen(new MainGameScreen());
            EnqueueNewScreen(new PlayerEntryScreen());

            MakeCurrent();

            GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
        }

        private static StaticResources LoadStaticResources()
        {
            StaticResources returnVal = new StaticResources();

            // Textures
            DirectoryInfo textureDirectory = new DirectoryInfo(@".\Resources\Textures");
            if (textureDirectory.Exists)
            {
                foreach (FileInfo textureFile in textureDirectory.EnumerateFiles())
                {
                    string name = textureFile.Name.Substring(0, textureFile.Name.Length - textureFile.Extension.Length);
                    returnVal.Textures[name] = GLTexture.Load(textureFile);
                }
            }

            // Shaders
            returnVal.Shaders["default"] = GLShaderProgram.Compile(Shaders.Vert_Std, Shaders.Frag_Texture);
            returnVal.Shaders["solidcolor"] = GLShaderProgram.Compile(Shaders.Vert_Std, Shaders.Frag_Std);
            returnVal.Shaders["wheel"] = GLShaderProgram.Compile(Shaders.Vert_Std, Shaders.Frag_Texture1D);
            returnVal.Shaders["blur"] = GLShaderProgram.Compile(Shaders.Vert_Std, Shaders.Frag_PointBlur);
            returnVal.Shaders["descrambler"] = GLShaderProgram.Compile(Shaders.Vert_Std, Shaders.Frag_Scrambler);

            // Sound effects
            returnVal.SoundEffects["locked_in"] = CreateSampleFromOpusFileStream(new FileStream(@".\Resources\Sounds\Locked in.opus", FileMode.Open));
            returnVal.SoundEffects["bonus_major"] = CreateSampleFromOpusFileStream(new FileStream(@".\Resources\Sounds\Bonus major.opus", FileMode.Open));
            returnVal.SoundEffects["bonus_minor"] = CreateSampleFromOpusFileStream(new FileStream(@".\Resources\Sounds\Bonus minor.opus", FileMode.Open));
            returnVal.SoundEffects["fanfare_1"] = CreateSampleFromOpusFileStream(new FileStream(@".\Resources\Sounds\Lap fanfare.opus", FileMode.Open));
            returnVal.SoundEffects["loss"] = CreateSampleFromOpusFileStream(new FileStream(@".\Resources\Sounds\Loss fanfare.opus", FileMode.Open));
            returnVal.SoundEffects["victory"] = CreateSampleFromOpusFileStream(new FileStream(@".\Resources\Sounds\Victory fanfare.opus", FileMode.Open));
            returnVal.SoundEffects["starting"] = CreateSampleFromOpusFileStream(new FileStream(@".\Resources\Sounds\Starting fanfare.opus", FileMode.Open));

            // Fonts
            QFontBuilderConfiguration fontBuilderConfig = new QFontBuilderConfiguration(true)
            {
                TextGenerationRenderHint = TextGenerationRenderHint.AntiAlias | TextGenerationRenderHint.AntiAliasGridFit,
                Characters = CharacterSet.BasicSet | CharacterSet.ExtendedLatin,
                ShadowConfig = new QFontShadowConfiguration()
                {
                    Type = ShadowType.Expanded,
                    BlurRadius = 1,
                    BlurPasses = 1,
                    Scale = 1.1f
                }
            };

            QFontBuilderConfiguration monospaceFontBuilderConfig = new QFontBuilderConfiguration(true)
            {
                TextGenerationRenderHint = TextGenerationRenderHint.AntiAlias | TextGenerationRenderHint.AntiAliasGridFit,
                Characters = CharacterSet.BasicSet | CharacterSet.ExtendedLatin,
                ShadowConfig = new QFontShadowConfiguration()
                {
                    Type = ShadowType.Expanded,
                    BlurRadius = 1,
                    BlurPasses = 1,
                    Scale = 1.1f
                },
                KerningConfig = new QFontKerningConfiguration()
                {
                    AlphaEmptyPixelTolerance = 0
                }
            };

            returnVal.Fonts["questionheader"] = new QFont(@".\Resources\Generation Two.ttf", 100, fontBuilderConfig);
            returnVal.Fonts["default_80pt"] = new QFont(@".\Resources\segueui.ttf", 80, fontBuilderConfig);
            returnVal.Fonts["default_60pt"] = new QFont(@".\Resources\segueui.ttf", 60, fontBuilderConfig);
            returnVal.Fonts["default_40pt"] = new QFont(@".\Resources\segueui.ttf", 40, fontBuilderConfig);
            returnVal.Fonts["default_20pt"] = new QFont(@".\Resources\segueui.ttf", 20, fontBuilderConfig);
            returnVal.Fonts["wheel"] = new QFont(@".\Resources\segueui.ttf", 24, fontBuilderConfig);
            returnVal.Fonts["score"] = new QFont(@".\Resources\SimplerGr.ttf", 60, fontBuilderConfig);
            returnVal.Fonts["playername"] = new QFont(@".\Resources\segueui.ttf", 40, fontBuilderConfig);
            returnVal.Fonts["monospace"] = new QFont(@".\Resources\UbuntuMono-R.ttf", 140, monospaceFontBuilderConfig);
            
            // Avatars
            returnVal.Avatars = AvatarCollection.Load(new DirectoryInfo(@".\Resources\Avatars"));

            return returnVal;
        }

        private static AudioSample CreateSampleFromOpusFileStream(Stream inputStream)
        {
            IAudioGraph captureGraph = new AudioGraph(AudioGraphCapabilities.None);
            using (AudioDecoder decoder = new OggOpusDecoder(captureGraph, "Opus", null, new ManagedOpusCodecProvider()))
            {
                decoder.Initialize(inputStream, true, CancellationToken.None, DefaultRealTimeProvider.Singleton).Await();
                using (BucketAudioSampleTarget bucket = new BucketAudioSampleTarget(captureGraph, decoder.OutputFormat, "OpusBucket"))
                {
                    decoder.ConnectOutput(bucket);
                    bucket.ReadFully(CancellationToken.None, DefaultRealTimeProvider.Singleton).Await();
                    return bucket.GetAllAudio();
                }
            }
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.F11)
            {
                if (WindowState == WindowState.Fullscreen)
                {
                    WindowState = WindowState.Normal;
                }
                else
                {
                    WindowState = WindowState.Fullscreen;
                }
            }
            else if (_gameScreenStack.Count > 0)
            {
                _gameScreenStack[_gameScreenStack.Count - 1].KeyDown(e);
            }
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (_gameScreenStack.Count > 0)
            {
                _gameScreenStack[_gameScreenStack.Count - 1].KeyUp(e);
            }
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);

            if (_gameScreenStack.Count > 0)
            {
                _gameScreenStack[_gameScreenStack.Count - 1].KeyTyped(e);
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (_gameScreenStack.Count > 0)
            {
                VirtualMouseClick mouseEvent = new VirtualMouseClick()
                {
                    Position = new VirtualMousePosition()
                    {
                        RealWidth = Width,
                        RealHeight = Height,
                        RealMouseX = e.X,
                        RealMouseY = e.Y,
                        VirtualWidth = INTERNAL_RESOLUTION_X,
                        VirtualHeight = INTERNAL_RESOLUTION_Y,
                        VirtualMouseX = e.X / (float)Width * INTERNAL_RESOLUTION_X,
                        VirtualMouseY = e.Y / (float)Height * INTERNAL_RESOLUTION_Y,
                    },
                    Button = e.Button
                };

                _gameScreenStack[_gameScreenStack.Count - 1].MouseDown(mouseEvent);
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (_gameScreenStack.Count > 0)
            {
                VirtualMouseClick mouseEvent = new VirtualMouseClick()
                {
                    Position = new VirtualMousePosition()
                    {
                        RealWidth = Width,
                        RealHeight = Height,
                        RealMouseX = e.X,
                        RealMouseY = e.Y,
                        VirtualWidth = INTERNAL_RESOLUTION_X,
                        VirtualHeight = INTERNAL_RESOLUTION_Y,
                        VirtualMouseX = e.X / (float)Width * INTERNAL_RESOLUTION_X,
                        VirtualMouseY = e.Y / (float)Height * INTERNAL_RESOLUTION_Y,
                    },
                    Button = e.Button
                };

                _gameScreenStack[_gameScreenStack.Count - 1].MouseUp(mouseEvent);
            }
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);

            if (_gameScreenStack.Count > 0)
            {
                VirtualMousePosition mousePos = new VirtualMousePosition()
                {
                    RealWidth = Width,
                    RealHeight = Height,
                    RealMouseX = e.X,
                    RealMouseY = e.Y,
                    VirtualWidth = INTERNAL_RESOLUTION_X,
                    VirtualHeight = INTERNAL_RESOLUTION_Y,
                    VirtualMouseX = e.X / (float)Width * INTERNAL_RESOLUTION_X,
                    VirtualMouseY = e.Y / (float)Height * INTERNAL_RESOLUTION_Y,
                };

                _gameScreenStack[_gameScreenStack.Count - 1].MouseMoved(mousePos);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);

            if (_gameScreenStack.Count > 0)
            {
                _gameScreenStack[_gameScreenStack.Count - 1].MouseEntered();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            if (_gameScreenStack.Count > 0)
            {
                _gameScreenStack[_gameScreenStack.Count - 1].MouseExited();
            }
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            // Drive game logic
            double msElapsed = _frameTimer.ElapsedMs - _lastFrameTime;
            _lastFrameTime = _frameTimer.ElapsedMs;
            _renderLock.EnterWriteLock();
            try
            {
                List<GameScreen> finishedScreens = null;
                foreach (GameScreen screen in _gameScreenStack)
                {
                    screen.Logic(msElapsed);

                    // Pop finished screens off the stack
                    if (screen.Finished)
                    {
                        if (finishedScreens == null)
                        {
                            finishedScreens = new List<GameScreen>();
                        }

                        finishedScreens.Add(screen);
                    }
                }

                if (finishedScreens != null)
                {
                    foreach (GameScreen finishedScreen in finishedScreens)
                    {
                        // Tell the screen below the finished ones that the finished ones finished
                        int signalTarget = _gameScreenStack.IndexOf(finishedScreen) - 1;
                        while (signalTarget >= 0 && _gameScreenStack[signalTarget].Finished)
                        {
                            signalTarget--;
                        }
                        if (signalTarget >= 0)
                        {
                            _gameScreenStack[signalTarget].ScreenAboveFinished(finishedScreen);
                        }

                        // And remove them from the render stack
                        _gameScreenStack.Remove(finishedScreen);
                    }
                }
            }
            finally
            {
                _renderLock.ExitWriteLock();
            }
        }

        private void EnqueueNewScreen(GameScreen screen)
        {
            _newScreens.Enqueue(screen);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            _framerate.Increment();
            if (DateTime.Now > _lastFpsUpdate + TimeSpan.FromSeconds(1))
            {
                Title = string.Format("Gamebler - {0:F2} FPS", _framerate.Rate);
                _lastFpsUpdate = DateTime.Now;
            }

            // Enqueue new screens
            while (_newScreens.Count > 0)
            {
                GameScreen newScreen = _newScreens.Dequeue();
                newScreen.Initialize(INTERNAL_RESOLUTION_X, INTERNAL_RESOLUTION_Y, _staticResources, _gameState, EnqueueNewScreen);
                // Call Render so there is at least something in its framebuffer
                newScreen.Render();
                _gameScreenStack.Add(newScreen);
            }

            GameScreen topGameScreen = _gameScreenStack[_gameScreenStack.Count - 1];
            // Tell the top screen to render to its target buffer
            _renderLock.EnterReadLock();
            try
            {
                // Actually just render all screens
                //foreach (GameScreen screen in _gameScreenStack)
                //{
                //    screen.Render();
                //}

                topGameScreen.Render();
            }
            finally
            {
                _renderLock.ExitReadLock();
            }

            // Clear global view
            int screenWidth = this.Width;
            int screenHeight = this.Height;
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, screenWidth, screenHeight);
            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Matrix4 modelViewMatrix = Matrix4.Identity;

            GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.Enable(EnableCap.Texture2D);
            GL.Disable(EnableCap.DepthTest);

            for (int screenId = 0; screenId < _gameScreenStack.Count; screenId++)
            {
                // Render the background framebuffers in order, blurring all but the top one
                GameScreen currentScreen = _gameScreenStack[screenId];
                bool isTop = screenId == _gameScreenStack.Count - 1;
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, currentScreen.TargetColorbuffer);

                if (isTop)
                {
                    GL.UseProgram(_staticResources.Shaders["default"].Handle);
                    GL.UniformMatrix4(GL.GetUniformLocation(_staticResources.Shaders["default"].Handle, "projectionMatrix"), false, ref _orthoProjectionMatrix);
                    GL.UniformMatrix4(GL.GetUniformLocation(_staticResources.Shaders["default"].Handle, "modelViewMatrix"), false, ref modelViewMatrix);
                }
                else
                {
                    GL.UseProgram(_staticResources.Shaders["blur"].Handle);
                    GL.Uniform1(GL.GetUniformLocation(_staticResources.Shaders["blur"].Handle, "dist"), 0.25f);
                    GL.Uniform1(GL.GetUniformLocation(_staticResources.Shaders["blur"].Handle, "textureImage"), 0);
                    GL.UniformMatrix4(GL.GetUniformLocation(_staticResources.Shaders["blur"].Handle, "projectionMatrix"), false, ref _orthoProjectionMatrix);
                    GL.UniformMatrix4(GL.GetUniformLocation(_staticResources.Shaders["blur"].Handle, "modelViewMatrix"), false, ref modelViewMatrix);
                }

                GL.Begin(PrimitiveType.Quads);
                {
                    GL.TexCoord2(0.0f, 1.0f);
                    GL.Vertex3(0.0f, 0.0f, 0);
                    GL.TexCoord2(0.0f, 0.0f);
                    GL.Vertex3(0.0f, 1.0f, 0);
                    GL.TexCoord2(1.0f, 0.0f);
                    GL.Vertex3(1.0f, 1.0f, 0);
                    GL.TexCoord2(1.0f, 1.0f);
                    GL.Vertex3(1.0f, 0.0f, 0);
                }
                GL.End();
            }

            // Then finalize our frame
            GL.Flush();
            SwapBuffers();
        }
    }
}
