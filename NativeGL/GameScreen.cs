using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using OpenTK;

namespace NativeGL
{
    public abstract class GameScreen
    {
        public delegate void EnqueueScreenDelegate(GameScreen screen);

        // The OpenGL framebuffer object that this screen gets rendered to
        public int TargetFramebuffer
        {
            get
            {
                return _primaryFrameBuffer;
            }
        }

        public int TargetColorbuffer
        {
            get
            {
                return _primaryColorTex;
            }
        }

        private int _primaryFrameBuffer;
        private int _primaryColorTex;
        private int _primaryDepthTex;
        private int _internalResolutionX;
        private int _internalResolutionY;
        private EnqueueScreenDelegate _enqueueScreen;
        private StaticResources _staticResources;
        private GlobalGameState _gameState;

        public void Initialize(int internalResolutionX, int internalResolutionY, StaticResources resources, GlobalGameState gameState, EnqueueScreenDelegate enqueueScreen)
        {
            _internalResolutionX = internalResolutionX;
            _internalResolutionY = internalResolutionY;
            _staticResources = resources;
            _gameState = gameState;
            _enqueueScreen = enqueueScreen;
            GLUtils.CreateFramebuffer(out _primaryColorTex, out _primaryDepthTex, out _primaryFrameBuffer, internalResolutionX, internalResolutionY);
            InitializeInternal();
        }

        protected abstract void InitializeInternal();

        public virtual void KeyDown(KeyboardKeyEventArgs args) { }
        public virtual void KeyUp(KeyboardKeyEventArgs args) { }
        public virtual void KeyTyped(KeyPressEventArgs args) { }

        public virtual void MouseUp(VirtualMouseClick args) { }
        public virtual void MouseDown(VirtualMouseClick args) { }
        public virtual void MouseMoved(VirtualMousePosition newPosition) { }
        public virtual void MouseExited() { }
        public virtual void MouseEntered() { }

        protected abstract void RenderInternal();

        public void Render()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _primaryFrameBuffer);
            GL.Viewport(0, 0, _internalResolutionX, _internalResolutionY);
            if (Finished)
            {
                return;
            }

            RenderInternal();
        }

        public abstract void Logic(double msElapsed);

        /// <summary>
        /// Signals to the game screen processor that this screen has finished and should be popped from the stack
        /// </summary>
        public abstract bool Finished { get; }

        protected int InternalResolutionX
        {
            get
            {
                return _internalResolutionX;
            }
        }

        protected int InternalResolutionY
        {
            get
            {
                return _internalResolutionY;
            }
        }

        protected void EnqueueScreen(GameScreen newScreen)
        {
            _enqueueScreen(newScreen);
        }

        protected StaticResources Resources
        {
            get
            {
                return _staticResources;
            }
        }

        protected GlobalGameState GameState
        {
            get
            {
                return _gameState;
            }
        }

        public virtual void ScreenAboveFinished(GameScreen aboveScreen) { }
    }
}
