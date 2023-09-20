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
    public class BigShotScreen : GameScreen
    {
        private const float TWO_PI = (float)(Math.PI * 2);
        private Matrix4 _projectionMatrix;

        private List<BigShotRouletteSlot> _rouletteSlots = new List<BigShotRouletteSlot>();
        private BigShotRouletteSlot _selectedSlot = null;

        private bool _finished = false;
        private float _wheelVelocity = 0;
        private float _wheelRotation = 0;
        private bool _wheelLocked = true;
        private const float INITIAL_SPIN_SPEED = 0.045f;
        private const float WHEEL_DRAG = 0.975f;
        private const float MIN_VELOCITY = 0.0001f;

        // Starts at 0
        // Increments to 1 at startup to ease in assets
        // Increments back down to 0 at end to ease out assets
        private float _animationTimer = 0;
        private float _animationTimerIncrement = 0; // amount to add to animationTimer each frame

        private QFont _textFont;
        private QFontDrawing _drawing;
        private QFontRenderOptions _renderOptions;
        private QFont _wheelFont;
        private QFontRenderOptions _wheelRenderOptions;
        private int _kromer = 0;
        private string _currentText = "";
        private DateTimeOffset _nextTextUpdate = DateTimeOffset.MinValue;

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

            _textFont = Resources.Fonts["default_40pt"];
            _drawing = new QFontDrawing();
            _renderOptions = new QFontRenderOptions()
            {
                UseDefaultBlendFunction = true,
                CharacterSpacing = 0.15f
            };

            _wheelFont = Resources.Fonts["wheel"];
            Resources.AudioSubsystem.PlayMusic("Bigshot");
            _animationTimerIncrement = 0.004f;
        }

        private void UpdateText()
        {
            if (_kromer == 0)
            {
                _currentText = "THIS IS YOUR CHANCE FOR A [HUGE] COMEBACK!!";
                _nextTextUpdate = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(100);
            }
            else if (_kromer < 0)
            {
                _currentText = "BUST! GUESS YOU'RE NOT [BIG TIME] AFTER ALL!!";
                _nextTextUpdate = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(100);
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                Random rand = new Random();
                double val = rand.NextDouble();
                if (val < 0.3)
                {
                    sb.Append(_kromer);
                    sb.Append(" KROMER ");
                    sb.Append('!', rand.Next(1, 10));
                    _nextTextUpdate = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(3);
                }
                else if (val < 0.5)
                {
                    sb.Append("[");
                    sb.Append(_kromer);
                    sb.Append("] KROMER");
                    sb.Append('!', rand.Next(1, 6));
                    _nextTextUpdate = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(3);
                }
                else if (val < 0.6)
                {
                    sb.Append(_kromer);
                    sb.Append(",000 KROMER ");
                    sb.Append('!', rand.Next(1, 4));
                    _nextTextUpdate = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(3);
                }
                else if (val < 0.7)
                {
                    sb.Append("[ERROR: HYPERLINK BLOCKED]");
                    _nextTextUpdate = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(1);
                }
                else if (val < 0.8)
                {
                    sb.Append("Go Large or    Go Away!");
                    _nextTextUpdate = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(2);
                }
                else if (val < 0.9)
                {
                    sb.Append("And now for a word from our sponsors!");
                    _nextTextUpdate = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(2);
                }
                else
                {
                    sb.Append("$");
                    sb.Append(_kromer);
                    sb.Append(" GUARANTEED!!");
                    _nextTextUpdate = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(4);
                }

                _currentText = sb.ToString();
            }
        }

        private int PredictOutcome(float initialVelocity)
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
            foreach (BigShotRouletteSlot slot in _rouletteSlots)
            {
                wheelSum += slot.Weight;
            }

            float theta = 0;
            float normalizer = TWO_PI / wheelSum;
            foreach (BigShotRouletteSlot slot in _rouletteSlots)
            {
                theta += slot.Weight * normalizer;

                if ((TWO_PI - theta) < rotation)
                {
                    return slot.ScoreAugment;
                }
            }

            return 0;
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

                    float hypVelocity;
                    bool outcomeOk;
                    int outcomeRetries = 0;
                    int predictedOutcome;
                    do
                    {
                        // Manipulate the wheel outcome here
                        hypVelocity = (float)((rand.NextDouble() * 1.5) + 0.5) * INITIAL_SPIN_SPEED;
                        bool bust = _kromer >= 300 && (_kromer > rand.Next(300, 1000)); // past this upper number is guaranteed bust
                        predictedOutcome = PredictOutcome(hypVelocity);
                        outcomeOk = true;

                        // Yeah it's rigged
                        if (bust && predictedOutcome >= 0)
                        {
                            outcomeOk = false;
                        }
                        else if (!bust && predictedOutcome < 0)
                        {
                            outcomeOk = false;
                        }
                    } while (!outcomeOk && outcomeRetries++ < 200);

                    Console.WriteLine("Predicted outcome: " + predictedOutcome);

                    _wheelVelocity = hypVelocity;
                    _wheelLocked = false;
                }
            }

            if (args.Key == OpenTK.Input.Key.BackSpace)
            {
                _animationTimerIncrement = -0.004f; // start easing out assets, will finish after animation finishes
            }
        }

        public override void MouseMoved(VirtualMousePosition args)
        {
        }

        public override void MouseDown(VirtualMouseClick args)
        {
        }

        #region Rendering

        protected override void RenderInternal()
        {
            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Matrix4 modelViewMatrix = Matrix4.Identity;
            // simple hermitian curve
            float easingTimer = 1.0f + (2.0f * _animationTimer * _animationTimer * _animationTimer) - (3.0f * _animationTimer * _animationTimer);
            modelViewMatrix = Matrix4.CreateTranslation(0.0f, InternalResolutionY * easingTimer, 0.0f) * modelViewMatrix;

            // Draw background
            GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);
            GL.Enable(EnableCap.Texture2D);
            GL.Disable(EnableCap.Texture1D);
            GL.UseProgram(Resources.Shaders["default"].Handle);
            GL.Uniform1(GL.GetUniformLocation(Resources.Shaders["default"].Handle, "textureImage"), 0);
            GL.UniformMatrix4(GL.GetUniformLocation(Resources.Shaders["default"].Handle, "projectionMatrix"), false, ref _projectionMatrix);
            GL.UniformMatrix4(GL.GetUniformLocation(Resources.Shaders["default"].Handle, "modelViewMatrix"), false, ref modelViewMatrix);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, Resources.Textures["spamton background"].Handle);
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

            modelViewMatrix = Matrix4.Identity;
            modelViewMatrix = Matrix4.CreateTranslation(InternalResolutionY * 0.42f, InternalResolutionY * 0.5f, 0.0f) * modelViewMatrix;
            modelViewMatrix = Matrix4.CreateTranslation(-1.0f * InternalResolutionY * easingTimer, 0.0f, 0.0f) * modelViewMatrix;
            Matrix4 wheelRotationModelView = Matrix4.CreateRotationZ(_wheelRotation) * modelViewMatrix;

            float wheelSum = 0;
            foreach (BigShotRouletteSlot slot in _rouletteSlots)
            {
                wheelSum += slot.Weight;
            }

            float wheelRadius = InternalResolutionY * 0.4f;
            float theta = 0;
            float normalizer = TWO_PI / wheelSum;
            float selectedTheta = 0;
            float wheelInnerRadius = 130f;

            Vector3 lineColor = new Vector3(0.0f, 0.0f, 0.0f);
            GL.Disable(EnableCap.Texture2D);
            foreach (BigShotRouletteSlot slot in _rouletteSlots)
            {
                float sliceSize = slot.Weight * normalizer;

                // While we're iterating, try and find the highlighted slot
                if ((TWO_PI - theta) > _wheelRotation)
                {
                    _selectedSlot = slot;
                    selectedTheta = theta;
                }

                DrawWheelSlice(slot, theta, sliceSize, lineColor, 4.0f, wheelInnerRadius, 500f, wheelRotationModelView);
                theta += sliceSize;
            }

            // Draw the highlighted slice on top of all others
            if (_selectedSlot != null)
            {
                lineColor = new Vector3(1.0f, 0.0f, 0.0f);
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
            GL.BindTexture(TextureTarget.Texture2D, Resources.Textures["spamton wheel hub"].Handle);

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

            // Draw big spamton
            GLTexture spamtonWheel = Resources.Textures["spamton wheel"];
            GL.BindTexture(TextureTarget.Texture2D, spamtonWheel.Handle);
            GL.UniformMatrix4(GL.GetUniformLocation(Resources.Shaders["default"].Handle, "modelViewMatrix"), false, ref modelViewMatrix);

            float spamtonScale = 1.611f;
            float spamtonCoverLeft = -460;
            float spamtonCoverTop = -543;
            float spamtonCoverRight = spamtonCoverLeft + (spamtonWheel.Width * spamtonScale);
            float spamtonCoverBottom = spamtonCoverTop + (spamtonWheel.Height * spamtonScale);


            GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);
            GL.Begin(PrimitiveType.Quads);
            {
                GL.TexCoord2(0.0f, 0.0f);
                GL.Vertex2(spamtonCoverLeft, spamtonCoverBottom);
                GL.TexCoord2(1.0f, 0.0f);
                GL.Vertex2(spamtonCoverRight, spamtonCoverBottom);
                GL.TexCoord2(1.0f, 1.0f);
                GL.Vertex2(spamtonCoverRight, spamtonCoverTop);
                GL.TexCoord2(0.0f, 1.0f);
                GL.Vertex2(spamtonCoverLeft, spamtonCoverTop);
            }
            GL.End();
            GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);

            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.Texture2D);

            _drawing.ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(0.0f, InternalResolutionX, 0.0f, InternalResolutionY, -1.0f, 1.0f);
            _drawing.DrawingPrimitives.Clear();

            SizeF maxSize = new SizeF(500, 500);
            SizeF measuredHeight = _textFont.Measure(_currentText, maxSize, QFontAlignment.Centre);
            _drawing.Print(_textFont, _currentText, new Vector3(1250, 250 + (measuredHeight.Height / 2) + (InternalResolutionY * easingTimer), 0), maxSize, QFontAlignment.Centre, _renderOptions);
            _drawing.RefreshBuffers();
            _drawing.Draw();
        }

        private void DrawWheelSlice(BigShotRouletteSlot slot, float theta, float thisSliceSize, Vector3 lineColor, float lineWidth, float innerR, float outerR, Matrix4 modelViewMatrix)
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
            slot.RenderedLabel.Print(_wheelFont, slot.Label, new Vector3(200, lineHeight, 0), new SizeF(outerR - 20f, -1), QFontAlignment.Left, _wheelRenderOptions);
            slot.RenderedLabel.RefreshBuffers();
            slot.RenderedLabel.Draw();
        }

        private static Vector3 FromColor(Color color)
        {
            return new Vector3((float)color.R / 256f, (float)color.G / 256f, (float)color.B / 256f);
        }

        private static List<BigShotRouletteSlot> BuildWheel()
        {
            Random rand = new Random();
            List<BigShotRouletteSlot> returnVal = new List<BigShotRouletteSlot>();

            int lastValue = 0;
            Color[] possibleColors = new Color[]
            {
                Color.FromArgb(32, 19, 174), // 50
                Color.FromArgb(224, 43, 55), // 100
                Color.FromArgb(98, 240, 235), // 150
                Color.FromArgb(240, 98, 148), // 200
                Color.FromArgb(240, 237, 98)  // 250
            };

            for (int c = 0; c < 6; c++)
            {
                returnVal.Add(new BigShotRouletteSlot()
                {
                    Weight = 2.0f,
                    Color = FromColor(Color.FromArgb(0, 0, 0)),
                    Label = "BANKRUPT",
                    RenderedLabel = new QFontDrawing(),
                    ScoreAugment = -99999
                });

                for (int good = 0; good < 3; good++)
                {
                    int score = rand.Next(1, 5) * 50;
                    while (score == lastValue)
                    {
                        score = rand.Next(1, 5) * 50;
                    }

                    lastValue = score;
                    Color thisColor = possibleColors[score / 50];
                    returnVal.Add(new BigShotRouletteSlot()
                    {
                        Weight = 3.0f,
                        Color = FromColor(thisColor),
                        Label = score.ToString(),
                        RenderedLabel = new QFontDrawing(),
                        ScoreAugment = score
                    });
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

                // Lock in selection
                float wheelSum = 0;
                foreach (BigShotRouletteSlot slot in _rouletteSlots)
                {
                    wheelSum += slot.Weight;
                }

                float theta = 0;
                float normalizer = TWO_PI / wheelSum;
                foreach (BigShotRouletteSlot slot in _rouletteSlots)
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

            if (_animationTimerIncrement != 0)
            {
                _animationTimer += _animationTimerIncrement;
            }

            if (_animationTimerIncrement > 0 && _animationTimer >= 1.0)
            {
                _animationTimer = 1.0f;
                _animationTimerIncrement = 0;
            }
            else if (_animationTimerIncrement < 0 && _animationTimer <= 0.0)
            {
                _animationTimer = 0.0f;
                _animationTimerIncrement = 0;
                Resources.AudioSubsystem.StopMusic();
                _finished = true;
            }

            if (_nextTextUpdate < DateTimeOffset.UtcNow)
            {
                UpdateText();
            }
        }

        private void WheelSettled(BigShotRouletteSlot selectedSlot)
        {
            _kromer += selectedSlot.ScoreAugment;
            UpdateText();
            if (selectedSlot.ScoreAugment > 0)
            {
                //Resources.AudioSubsystem.PlaySound(Resources.SoundEffects["kaching"]);
            }
            else
            {
                //Resources.AudioSubsystem.PlaySound(Resources.SoundEffects["loss"]);
            }
        }

        public override bool Finished
        {
            get
            {
                return _finished;
            }
        }

        public class BigShotRouletteSlot
        {
            public float Weight;
            public Vector3 Color;
            public string Label;
            public QFontDrawing RenderedLabel;
            public int ScoreAugment;
        }
    }
}
