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
using Durandal.Common.Audio.Components;
using System.Diagnostics;
using Durandal.Common.Logger;

namespace NativeGL.Screens
{
    public class MainGameScreen : GameScreen
    {
        private const float TWO_PI = (float)(Math.PI * 2);
        private Matrix4 _projectionMatrix;

        private List<RouletteSlot> _rouletteSlots = new List<RouletteSlot>();
        private RouletteSlot _selectedSlot = null;
        private Queue<RouletteSlotType> _categoryHistory = new Queue<RouletteSlotType>();
        private HashSet<RouletteSlotType> _allPlayedCategories = new HashSet<RouletteSlotType>();

        private float _wheelVelocity = 0;
        private float _wheelRotation = 0;
        private bool _wheelLocked = true;
        private const float INITIAL_SPIN_SPEED = 0.045f;
        private const float WHEEL_DRAG = 0.975f;
        private const float MIN_VELOCITY = 0.0001f;
        
        private QFont _wheelFont;
        private QFontRenderOptions _wheelRenderOptions;
        
        private QFont _scoreFont;
        private QFont _playerNameFont;
        private QFontDrawing _playerAreaText;
        private List<GLButton> _playerButtons;
        private int _currentPlayerTurnIdx = 0;
        private bool _timesAlmostUp = false;
        private int _turnIdx = 0;

        protected override void InitializeInternal()
        {
            _rouletteSlots = BuildWheel();
            _projectionMatrix = Matrix4.CreateOrthographicOffCenter(0, InternalResolutionX, 0, InternalResolutionY, -1.0f, 1.0f);

            // Load wheel font
            QFontBuilderConfiguration builderConfig = new QFontBuilderConfiguration(true)
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
            
            _wheelRenderOptions = new QFontRenderOptions()
            {
                UseDefaultBlendFunction = true,
                Colour = Color.White,
                DropShadowActive = true,
                DropShadowOffset = new Vector2(0, 0),
                CharacterSpacing = 0.15f
            };

            _wheelFont = Resources.Fonts["wheel"];
            _scoreFont = Resources.Fonts["score"];
            _playerNameFont = Resources.Fonts["playername"];
            _playerAreaText = new QFontDrawing();
            _playerButtons = new List<GLButton>();
        }

        private RouletteSlotType PredictOutcome(float initialVelocity)
        {
            float rotation = _wheelRotation;
            float velocity = initialVelocity;
            while (velocity >= MIN_VELOCITY)
            {
                rotation += velocity * 33.33f;
                velocity *= WHEEL_DRAG;

                if (rotation > TWO_PI)
                {
                    rotation -= TWO_PI;
                }
            }

            float wheelSum = 0;
            foreach (RouletteSlot slot in _rouletteSlots)
            {
                wheelSum += slot.Weight;
            }

            float theta = 0;
            float normalizer = TWO_PI / wheelSum;
            foreach (RouletteSlot slot in _rouletteSlots)
            {
                theta += slot.Weight * normalizer;

                if ((TWO_PI - theta) < rotation)
                {
                    return slot.Type;
                }
            }

            return RouletteSlotType.Unknown;
        }

        private float TryTargetWheelSegment(RouletteSlotType target)
        {
            Random rand = new Random();
            float returnVal = 0;
            for (int attempt = 0; attempt < 1000; attempt++)
            {
                returnVal = (float)((rand.NextDouble() * 1.5) + 0.5) * INITIAL_SPIN_SPEED;
                RouletteSlotType inferredTarget = PredictOutcome(returnVal);
                if (target == inferredTarget)
                {
                    return returnVal;
                }
            }

            return returnVal;
        }
         
        public override void KeyDown(KeyboardKeyEventArgs args)
        {
            base.KeyDown(args);

            if (_wheelLocked)
            {
                // Spin the wheel
                if (args.Key == OpenTK.Input.Key.Space)
                {
                    Random rand = new Random();

                    int currentPlayerPoints = GameState.Players[_currentPlayerTurnIdx].Score;
                    int highestPlayerPoints = GameState.Players.Max((s) => s.Score);
                    int lowestPlayerPoints = GameState.Players.Min((s) => s.Score);
                    float hypVelocity;
                    bool outcomeOk;
                    int outcomeRetries = 0;
                    RouletteSlotType predictedOutcome;
                    do
                    {
                        // Manipulate the wheel outcome here
                        hypVelocity = (float)((rand.NextDouble() * 1.5) + 0.5) * INITIAL_SPIN_SPEED;
                        predictedOutcome = PredictOutcome(hypVelocity);
                        outcomeOk = true;

                        // Disallow the same category multiple times in a row
                        if (_categoryHistory.Contains(predictedOutcome))
                        {
                            outcomeOk = false;
                        }

                        // Disallow more than one round of Betrayal or BigShot
                        if ((predictedOutcome == RouletteSlotType.PrisonerDilemma || predictedOutcome == RouletteSlotType.BigShot) &&
                            _allPlayedCategories.Contains(predictedOutcome))
                        {
                            outcomeOk = false;
                        }

                        // Disallow feelin' sad for the player with fewest points
                        // or if it would put them into negative
                        if (predictedOutcome == RouletteSlotType.FeelinSad &&
                            (currentPlayerPoints == lowestPlayerPoints ||
                            currentPlayerPoints < 100))
                        {
                            outcomeOk = false;
                        }

                        // Disallow bigshot for the player with most points
                        if (predictedOutcome == RouletteSlotType.BigShot &&
                            currentPlayerPoints == highestPlayerPoints)
                        {
                            outcomeOk = false;
                        }

                        // Disallow A.I. arena and prisoners dilemma when players have low score totals
                        if ((predictedOutcome == RouletteSlotType.AIArena || predictedOutcome == RouletteSlotType.PrisonerDilemma) &&
                            lowestPlayerPoints < 150)
                        {
                            outcomeOk = false;
                        }
                    } while (!outcomeOk && outcomeRetries++ < 50);

                    // Is it time to deploy the BIGSHOT?
                    if (!_allPlayedCategories.Contains(RouletteSlotType.BigShot) &&
                        currentPlayerPoints == lowestPlayerPoints && 
                        _rouletteSlots.Any((s) => s.Type == RouletteSlotType.BigShot) &&
                        rand.NextDouble() < (double)(highestPlayerPoints - currentPlayerPoints) / 1000.0)
                    {
                        Console.WriteLine("Deploying BIG SHOT");
                        hypVelocity = TryTargetWheelSegment(RouletteSlotType.BigShot);
                        predictedOutcome = RouletteSlotType.BigShot;
                    }

                    Console.WriteLine("Predicted outcome: " + predictedOutcome);

                    _wheelVelocity = hypVelocity;
                    if (!_timesAlmostUp)
                    {
                        Resources.AudioSubsystem.PlayMusic("Randomizer");
                    }

                    _wheelLocked = false;
                }

                // Enter the current game
                if (args.Key == OpenTK.Input.Key.Enter && _selectedSlot != null)
                {
                    switch (_selectedSlot.Type)
                    {
                        case RouletteSlotType.Descrambler:
                            EnqueueScreen(new DescramblerScreen());
                            break;
                        case RouletteSlotType.SoundTest:
                            EnqueueScreen(new SoundTestScreen());
                            break;
                        case RouletteSlotType.PrisonerDilemma:
                            EnqueueScreen(new BetrayalScreen());
                            break;
                        case RouletteSlotType.Quizzler:
                            EnqueueScreen(new QuizzlerScreen());
                            break;
                        case RouletteSlotType.Drawbage:
                            EnqueueScreen(new ScribblerScreen());
                            break;
                        case RouletteSlotType.AIArena:
                            EnqueueScreen(new ArenaScreen());
                            break;
                        case RouletteSlotType.WordDescrambler:
                            EnqueueScreen(new WordDescramblerScreen());
                            break;
                        case RouletteSlotType.FeelinGroovy:
                            EnqueueScreen(new FeelinGroovyScreen());
                            break;
                        case RouletteSlotType.FeelinSad:
                            EnqueueScreen(new FeelinSadScreen());
                            break;
                        case RouletteSlotType.BigShot:
                            EnqueueScreen(new BigShotScreen());
                            break;
                        default:
                            EnqueueScreen(new EmptyScreen());
                            break;
                    }

                    if (!_allPlayedCategories.Contains(_selectedSlot.Type))
                    {
                        _allPlayedCategories.Add(_selectedSlot.Type);
                    }

                    _turnIdx++;
                    _currentPlayerTurnIdx = (_currentPlayerTurnIdx + 1) % GameState.Players.Count;

                    // Sneak in BIGSHOT on turn 7
                    if (_turnIdx == 7)
                    {
                        _rouletteSlots.Add(new RouletteSlot()
                        {
                            Weight = 2.0f,
                            Color = FromColor(Color.FromArgb(250, 85, 203)),
                            Label = "BIG SHOT",
                            RenderedLabel = new QFontDrawing(),
                            Type = RouletteSlotType.BigShot
                        });
                    }
                }
            }

            if (args.Key == OpenTK.Input.Key.End)
            {
                _timesAlmostUp = true;
                Resources.AudioSubsystem.PlayMusic("pizza");
            }
            else if (args.Key == OpenTK.Input.Key.PageDown)
            {
                _currentPlayerTurnIdx = (_currentPlayerTurnIdx + 1) % GameState.Players.Count;
            }
            else if (args.Key == OpenTK.Input.Key.PageUp)
            {
                _currentPlayerTurnIdx -= 1;
                if (_currentPlayerTurnIdx < 0)
                {
                    _currentPlayerTurnIdx += GameState.Players.Count;
                }
            }
        }

        public override void MouseMoved(VirtualMousePosition args)
        {
            foreach (GLButton playerButton in _playerButtons)
            {
                playerButton.MouseMoved(args);
            }
        }

        public override void MouseDown(VirtualMouseClick args)
        {
            foreach (GLButton playerButton in _playerButtons)
            {
                playerButton.MouseDown(args);
            }
        }

        #region Rendering

        protected override void RenderInternal()
        {
            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Matrix4 modelViewMatrix = Matrix4.Identity;

            // Draw background
            GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);
            GL.Enable(EnableCap.Texture2D);
            GL.Disable(EnableCap.Texture1D);
            GL.UseProgram(Resources.Shaders["default"].Handle);
            GL.Uniform1(GL.GetUniformLocation(Resources.Shaders["default"].Handle, "textureImage"), 0);
            GL.UniformMatrix4(GL.GetUniformLocation(Resources.Shaders["default"].Handle, "projectionMatrix"), false, ref _projectionMatrix);
            GL.UniformMatrix4(GL.GetUniformLocation(Resources.Shaders["default"].Handle, "modelViewMatrix"), false, ref modelViewMatrix);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, Resources.Textures["background"].Handle);
            //GL.Enable(EnableCap.CullFace);
            //GL.CullFace(CullFaceMode.Back);

            GL.Begin(PrimitiveType.Quads);
            {
                GL.TexCoord2(0.0f, 0.0f);
                GL.Vertex2(0.0f, InternalResolutionY);
                GL.TexCoord2(1.0f, 0.0f);
                GL.Vertex2(InternalResolutionX, InternalResolutionY);
                GL.TexCoord2(1.0f, 1.0f);
                GL.Vertex2(InternalResolutionX, 0.0f);
                GL.TexCoord2(0.0f, 1.0f);
                GL.Vertex2(0.0f, 0.0f);
            }
            GL.End();

            // Begin drawing the wheel
            modelViewMatrix = Matrix4.CreateTranslation(InternalResolutionY * 0.42f, InternalResolutionY * 0.5f, 0.0f) * modelViewMatrix;
            Matrix4 wheelRotationModelView = Matrix4.CreateRotationZ(_wheelRotation) * modelViewMatrix;
            
            float wheelSum = 0;
            foreach (RouletteSlot slot in _rouletteSlots)
            {
                wheelSum += slot.Weight;
            }

            float wheelRadius = InternalResolutionY * 0.4f;
            float theta = 0;
            float normalizer = TWO_PI / wheelSum;
            float selectedTheta = 0;
            float wheelInnerRadius = 120f;
            
            Vector3 lineColor = new Vector3(0.0f, 0.0f, 0.0f);
            GL.Disable(EnableCap.Texture2D);
            foreach (RouletteSlot slot in _rouletteSlots)
            {
                float sliceSize = slot.Weight * normalizer;

                // While we're iterating, try and find the highlighted slot
                if ((TWO_PI - theta) > _wheelRotation)
                {
                    _selectedSlot = slot;
                    selectedTheta = theta;
                }

                DrawWheelSlice(slot, theta, sliceSize, lineColor, 4.0f, wheelInnerRadius, 450f, wheelRotationModelView);
                theta += sliceSize;
            }

            // Draw the highlighted slice on top of all others
            if (_selectedSlot != null)
            {
                //lineColor = new Vector3(1.0f, 0.0f, 0.0f);
                DrawWheelSlice(_selectedSlot, selectedTheta, _selectedSlot.Weight * normalizer, lineColor, 6.0f, wheelInnerRadius, 500f, wheelRotationModelView);
            }

            GL.Disable(EnableCap.Texture1D);
            GL.Disable(EnableCap.Texture2D);

            // Draw the wheel hub
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);
            GL.UseProgram(Resources.Shaders["default"].Handle);
            GL.Uniform1(GL.GetUniformLocation(Resources.Shaders["default"].Handle, "textureImage"), 0);
            GL.UniformMatrix4(GL.GetUniformLocation(Resources.Shaders["default"].Handle, "projectionMatrix"), false, ref _projectionMatrix);
            GL.UniformMatrix4(GL.GetUniformLocation(Resources.Shaders["default"].Handle, "modelViewMatrix"), false, ref wheelRotationModelView);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, Resources.Textures["wheel hub"].Handle);

            GL.Begin(PrimitiveType.Quads);
            {
                GL.TexCoord2(0.0f, 0.0f);
                GL.Vertex2(0 - wheelInnerRadius, wheelInnerRadius);
                GL.TexCoord2(1.0f, 0.0f);
                GL.Vertex2(wheelInnerRadius, wheelInnerRadius);
                GL.TexCoord2(1.0f, 1.0f);
                GL.Vertex2(wheelInnerRadius, 0 - wheelInnerRadius);
                GL.TexCoord2(0.0f, 1.0f);
                GL.Vertex2(0 - wheelInnerRadius, 0 - wheelInnerRadius);
            }
            GL.End();
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.Texture2D);

            // Draw the ticker
            GL.UseProgram(Resources.Shaders["solidcolor"].Handle);
            GL.UniformMatrix4(GL.GetUniformLocation(Resources.Shaders["solidcolor"].Handle, "projectionMatrix"), false, ref _projectionMatrix);
            GL.UniformMatrix4(GL.GetUniformLocation(Resources.Shaders["solidcolor"].Handle, "modelViewMatrix"), false, ref modelViewMatrix);
            GL.Color3(1.0f, 0.0, 0.0f);
            GL.Begin(PrimitiveType.Triangles);
            {
                GL.Vertex2(500f, 0f);
                GL.Vertex2(530f, 10f);
                GL.Vertex2(530f, -10f);
            }
            GL.End();

            // Draw all the players now
            float avatarPadding = 20;
            float avatarAreaWidth = 800;
            float avSize = Math.Min(200, (InternalResolutionY / (float)GameState.Players.Count) - avatarPadding);
            float totalPlayerAreaHeight = (GameState.Players.Count * avSize) + ((GameState.Players.Count - 1) * avatarPadding);
            float playerOffsetY = InternalResolutionY - ((InternalResolutionY - totalPlayerAreaHeight) / 2) - avSize;

            _playerAreaText.DrawingPrimitives.Clear();
            _playerAreaText.ProjectionMatrix = _projectionMatrix;

            float avatarAreaLeft = InternalResolutionX - avatarAreaWidth - avatarPadding;

            for (int playerIdx = 0; playerIdx < GameState.Players.Count; playerIdx++)
            {
                Player player = GameState.Players[playerIdx];
                modelViewMatrix = Matrix4.Identity;
                modelViewMatrix = Matrix4.CreateTranslation(avatarAreaLeft, playerOffsetY, 0.0f) * modelViewMatrix;
                
                if (_playerButtons.Count > playerIdx)
                {
                    _playerButtons[playerIdx].Render();
                }

                if (playerIdx == _currentPlayerTurnIdx)
                {
                    // Current player selection indicator
                    GL.UseProgram(Resources.Shaders["solidcolor"].Handle);
                    GL.UniformMatrix4(GL.GetUniformLocation(Resources.Shaders["solidcolor"].Handle, "projectionMatrix"), false, ref _projectionMatrix);
                    GL.UniformMatrix4(GL.GetUniformLocation(Resources.Shaders["solidcolor"].Handle, "modelViewMatrix"), false, ref modelViewMatrix);

                    GL.Begin(PrimitiveType.Quads);
                    {
                        GL.Color4(0.0f, 1.0f, 0.0f, 1.0f);
                        GL.Vertex2(0, avSize);
                        GL.Vertex2(0, 0);
                        GL.Color4(0.0f, 1.0f, 0.0f, 0.0f);
                        GL.Vertex2(avatarAreaWidth, 0);
                        GL.Vertex2(avatarAreaWidth, avSize);
                    }
                    GL.End();

                    // Offset current player to the left
                    //modelViewMatrix = Matrix4.CreateTranslation(-50, 0, 0.0f) * modelViewMatrix;
                }

                GL.Enable(EnableCap.Texture2D);
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);
                GL.UseProgram(Resources.Shaders["default"].Handle);
                GL.Uniform1(GL.GetUniformLocation(Resources.Shaders["default"].Handle, "textureImage"), 0);
                GL.UniformMatrix4(GL.GetUniformLocation(Resources.Shaders["default"].Handle, "projectionMatrix"), false, ref _projectionMatrix);
                GL.UniformMatrix4(GL.GetUniformLocation(Resources.Shaders["default"].Handle, "modelViewMatrix"), false, ref modelViewMatrix);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, player.Avatar.Handle);

                GL.Begin(PrimitiveType.Quads);
                {
                    GL.TexCoord2(1.0f, 1.0f);
                    GL.Vertex2(avSize, 0);
                    GL.TexCoord2(1.0f, 0.0f);
                    GL.Vertex2(avSize, avSize);
                    GL.TexCoord2(0.0f, 0.0f);
                    GL.Vertex2(0, avSize);
                    GL.TexCoord2(0.0f, 1.0f);
                    GL.Vertex2(0, 0);
                }
                GL.End();

                GL.Disable(EnableCap.Texture2D);
                _playerAreaText.Print(_playerNameFont, player.Name, new Vector3(avatarAreaLeft + avSize + avatarPadding, playerOffsetY + avSize, 0), QFontAlignment.Left, Color.White);
                _playerAreaText.Print(_scoreFont, player.Score.ToString() + " PTS", new Vector3(avatarAreaLeft + avSize + avatarPadding, playerOffsetY + avSize - _playerNameFont.MaxLineHeight, 0), QFontAlignment.Left, Color.White);
                playerOffsetY -= avSize + avatarPadding;

            }

            _playerAreaText.RefreshBuffers();
            _playerAreaText.Draw();
        }

        private void DrawWheelSlice(RouletteSlot slot, float theta, float thisSliceSize, Vector3 lineColor, float lineWidth, float innerR, float outerR, Matrix4 modelViewMatrix)
        {
            GL.Enable(EnableCap.Texture1D);
            GL.UseProgram(Resources.Shaders["wheel"].Handle);
            GL.UniformMatrix4(GL.GetUniformLocation(Resources.Shaders["wheel"].Handle, "projectionMatrix"), false, ref _projectionMatrix);
            GL.UniformMatrix4(GL.GetUniformLocation(Resources.Shaders["wheel"].Handle, "modelViewMatrix"), false, ref modelViewMatrix);
            GL.Uniform1(GL.GetUniformLocation(Resources.Shaders["wheel"].Handle, "textureImage"), 0);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture1D, Resources.Textures["wheel tex"].Handle);

            int segments = Math.Max(1, (int)(thisSliceSize * 12));
            float wheelResolution = thisSliceSize / (float)segments;
            GL.Color3(slot.Color);
            GL.Begin(PrimitiveType.TriangleStrip);
            {
                for (int segment = 0; segment <= segments; segment++)
                {
                    float angle = theta + (wheelResolution * segment);
                    float x = (float)(Math.Cos(angle));
                    float y = (float)(Math.Sin(angle));
                    GL.TexCoord1(0.0f);
                    GL.Vertex2(x * innerR, y * innerR);
                    GL.TexCoord1(1.0f);
                    GL.Vertex2(x * outerR, y * outerR);
                }
            }
            GL.End();

            GL.Disable(EnableCap.Texture1D);
            GL.Color3(lineColor);
            GL.LineWidth(lineWidth);

            GL.Begin(PrimitiveType.LineLoop);
            {
                for (int segment = 0; segment <= segments; segment++)
                {
                    float angle = theta + (wheelResolution * segment);
                    float x = (float)(Math.Cos(angle));
                    float y = (float)(Math.Sin(angle));
                    GL.Vertex2(x * outerR, y * outerR);
                }
                for (int segment = segments; segment >= 0; segment--)
                {
                    float angle = theta + (wheelResolution * segment);
                    float x = (float)(Math.Cos(angle));
                    float y = (float)(Math.Sin(angle));
                    GL.Vertex2(x * innerR, y * innerR);
                }
            }
            GL.End();

            slot.RenderedLabel.DrawingPrimitives.Clear();
            Matrix4 fontMatrix = Matrix4.CreateRotationZ(theta + (thisSliceSize / 2)) * modelViewMatrix;
            slot.RenderedLabel.ProjectionMatrix = fontMatrix * _projectionMatrix;
            float lineHeight = _wheelFont.MaxLineHeight / 2f;
            slot.RenderedLabel.Print(_wheelFont, slot.Label, new Vector3(0, lineHeight, 0), new SizeF(outerR - 20f, -1), QFontAlignment.Right, _wheelRenderOptions);
            slot.RenderedLabel.RefreshBuffers();
            slot.RenderedLabel.Draw();
        }

        private static Vector3 FromColor(Color color)
        {
            return new Vector3((float)color.R / 256f, (float)color.G / 256f, (float)color.B / 256f);
        }

        private static List<RouletteSlot> BuildWheel()
        {
            Random rand = new Random();
            List<RouletteSlot> returnVal = new List<RouletteSlot>();
            for (int c = 0; c < 2; c++)
            {
                returnVal.Add(new RouletteSlot()
                {
                    Weight = 3.0f,
                    Color = FromColor(Color.FromArgb(32, 19, 174)),
                    Label = "Drawbage",
                    RenderedLabel = new QFontDrawing(),
                    Type = RouletteSlotType.Drawbage
                });
            }
            for (int c = 0; c < 3; c++)
            {
                returnVal.Add(new RouletteSlot()
                {
                    Weight = 3.0f,
                    Color = FromColor(Color.FromArgb(52, 230, 45)),
                    Label = "Quizzler",
                    RenderedLabel = new QFontDrawing(),
                    Type = RouletteSlotType.Quizzler
                });
            }
            for (int c = 0; c < 2; c++)
            {
                returnVal.Add(new RouletteSlot()
                {
                    Weight = 2.0f,
                    Color = FromColor(Color.FromArgb(217, 21, 39)),
                    Label = "The Arena",
                    RenderedLabel = new QFontDrawing(),
                    Type = RouletteSlotType.AIArena
                });
            }
            for (int c = 0; c < 2; c++)
            {
                returnVal.Add(new RouletteSlot()
                {
                    Weight = 3.0f,
                    Color = FromColor(Color.FromArgb(107, 31, 151)),
                    Label = "Sound Test",
                    RenderedLabel = new QFontDrawing(),
                    Type = RouletteSlotType.SoundTest
                });
            }
            for (int c = 0; c < 2; c++)
            {
                returnVal.Add(new RouletteSlot()
                {
                    Weight = 2.5f,
                    Color = FromColor(Color.FromArgb(188, 0, 122)),
                    Label = "Descrambler",
                    RenderedLabel = new QFontDrawing(),
                    Type = RouletteSlotType.Descrambler
                });
            }
            for (int c = 0; c < 1; c++)
            {
                returnVal.Add(new RouletteSlot()
                {
                    Weight = 4.0f,
                    Color = FromColor(Color.FromArgb(239, 194, 0)),
                    Label = "Betrayal",
                    RenderedLabel = new QFontDrawing(),
                    Type = RouletteSlotType.PrisonerDilemma
                });
            }
            //}
            for (int c = 0; c < 1; c++)
            {
                returnVal.Add(new RouletteSlot()
                {
                    Weight = 1.0f,
                    Color = FromColor(Color.White),
                    Label = "Feelin' Groovy",
                    RenderedLabel = new QFontDrawing(),
                    Type = RouletteSlotType.FeelinGroovy
                });
            }
            for (int c = 0; c < 1; c++)
            {
                returnVal.Add(new RouletteSlot()
                {
                    Weight = 1.0f,
                    Color = FromColor(Color.Black),
                    Label = "Feelin' Sad",
                    RenderedLabel = new QFontDrawing(),
                    Type = RouletteSlotType.FeelinSad
                });
            }
            for (int c = 0; c < 1; c++)
            {
                returnVal.Add(new RouletteSlot()
                {
                    Weight = 3.0f,
                    Color = FromColor(Color.FromArgb(113, 226, 255)),
                    Label = "Missing Letters",
                    RenderedLabel = new QFontDrawing(),
                    Type = RouletteSlotType.WordDescrambler
                });
            }

            //returnVal.Clear();
            //returnVal.Add(new RouletteSlot()
            //{
            //    Weight = 3.0f,
            //    Color = FromColor(Color.FromArgb(32, 19, 174)),
            //    Label = "Debug",
            //    RenderedLabel = new QFontDrawing(),
            //    Type = RouletteSlotType.Quizzler
            //});

            // While the list has adjacent elements, bubble shuffle the list
            if (returnVal.Count > 2)
            {
                bool properlyShuffled = false;
                int shuffleAttempts = 50;
                while (!properlyShuffled && shuffleAttempts-- > 0)
                {
                    for (int swap = 0; swap < returnVal.Count * 3; swap++)
                    {
                        int sourceIdx = rand.Next(0, returnVal.Count);
                        int destIdx = rand.Next(0, returnVal.Count);
                        RouletteSlot source = returnVal[sourceIdx];
                        RouletteSlot dest = returnVal[destIdx];
                        returnVal.RemoveAt(sourceIdx);
                        returnVal.Insert(sourceIdx, dest);
                        returnVal.RemoveAt(destIdx);
                        returnVal.Insert(destIdx, source);
                    }

                    properlyShuffled = returnVal[0].Type != returnVal[returnVal.Count - 1].Type;
                    for (int c = 0; c < returnVal.Count - 1; c++)
                    {
                        properlyShuffled &= returnVal[c].Type != returnVal[c + 1].Type;
                    }
                }
            }

            return returnVal;
        }

        #endregion

        public override void Logic(double msElapsed)
        {
            msElapsed = 33.33f; // hack for locked 30fps
            if (!_wheelLocked && _wheelVelocity < MIN_VELOCITY)
            {
                _wheelVelocity = 0;
                _wheelLocked = true;

                _categoryHistory.Enqueue(_selectedSlot.Type);
                if (_categoryHistory.Count > 2)
                {
                    _categoryHistory.Dequeue();
                }

                if (!_timesAlmostUp)
                {
                    Resources.AudioSubsystem.StopMusic();
                }

                // Lock in selection
                float wheelSum = 0;
                foreach (RouletteSlot slot in _rouletteSlots)
                {
                    wheelSum += slot.Weight;
                }

                float theta = 0;
                float normalizer = TWO_PI / wheelSum;
                foreach (RouletteSlot slot in _rouletteSlots)
                {
                    theta += slot.Weight * normalizer;

                    if ((TWO_PI - theta) < _wheelRotation)
                    {
                        _selectedSlot = slot;
                        break;
                    }
                }

                WheelSettled(_selectedSlot);
            }
            else
            {
                _wheelRotation += _wheelVelocity * (float)msElapsed;
                _wheelVelocity *= WHEEL_DRAG;
                if (_wheelRotation > TWO_PI)
                {
                    _wheelRotation -= TWO_PI;
                }
            }

            // Create buttons for each player if the current buttons are inconsistent
            if (GameState.Players.Count != _playerButtons.Count)
            {
                foreach (GLButton oldButton in _playerButtons)
                {
                    oldButton.Clicked -= PlayerButtonClicked;
                }

                _playerButtons.Clear();

                float avatarPadding = 20;
                float avatarAreaWidth = 800;
                float avSize = Math.Min(200, (InternalResolutionY / (float)GameState.Players.Count) - avatarPadding);
                float totalPlayerAreaHeight = (GameState.Players.Count * avSize) + ((GameState.Players.Count - 1) * avatarPadding);
                float playerOffsetY = (InternalResolutionY - totalPlayerAreaHeight) / 2;
                float avatarAreaLeft = InternalResolutionX - avatarAreaWidth - avatarPadding;
                for (int playerIndex = 0; playerIndex < GameState.Players.Count; playerIndex++)
                {
                    GLButton thisPlayerButton = new GLButton(Resources, avatarAreaLeft, playerOffsetY, avatarAreaWidth, avSize, string.Empty, playerIndex.ToString());
                    _playerButtons.Add(thisPlayerButton);
                    thisPlayerButton.Clicked += PlayerButtonClicked;
                    playerOffsetY += avSize + avatarPadding;
                }
            }
        }

        private void WheelSettled(RouletteSlot selectedSlot)
        {
            if (_timesAlmostUp)
            {
                return;
            }

            if (selectedSlot.Type == RouletteSlotType.FeelinGroovy)
            {
                Resources.AudioSubsystem.PlaySound(Resources.SoundEffects["victory"]);
            }
            else if (selectedSlot.Type == RouletteSlotType.FeelinSad)
            {
                Resources.AudioSubsystem.PlaySound(Resources.SoundEffects["loss"]);
            }
            else
            {
                Resources.AudioSubsystem.PlaySound(Resources.SoundEffects["locked_in"]);
            }
        }

        public override bool Finished
        {
            get
            {
                return false;
            }
        }

        public override void ScreenAboveFinished(GameScreen aboveScreen)
        {
        }

        private void PlayerButtonClicked(object source, ButtonPressedEventArgs args)
        {
            // Augment player's score
            int playerIdx = int.Parse(args.SourceButtonId);
            if (args.MouseButton == OpenTK.Input.MouseButton.Left)
            {
                GameState.Players[playerIdx].Score += 50;
            }
            else
            {
                GameState.Players[playerIdx].Score -= 50;
            }
        }
    }
}
