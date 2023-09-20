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
    public class PlayerEntryScreen : GameScreen
    {
        private bool _finished = false;
        private List<GLButton> _buttons;
        private int _numPlayers = 0;

        public PlayerEntryScreen()
        {
        }

        protected override void InitializeInternal()
        {
            _buttons = new List<GLButton>();
            float buttonHeight = 100;
            float buttonWidth = 500;
            float buttonPadding = 20;
            float buttonX = (InternalResolutionX - buttonWidth) / 2;
            float buttonY = (InternalResolutionY - ((buttonHeight * 6) + (buttonPadding * 3))) / 2;
            for (int c = 2; c < 8; c++)
            {
                GLButton newButton = new GLButton(Resources, buttonX, buttonY, buttonWidth, buttonHeight, c + " players", c.ToString());
                _buttons.Add(newButton);
                buttonY += buttonHeight + buttonPadding;
                newButton.Clicked += ButtonClicked;
            }

            Resources.AudioSubsystem.PlayMusic("Data Select");
        }

        public override void MouseMoved(VirtualMousePosition args)
        {
            foreach (GLButton button in _buttons)
            {
                button.MouseMoved(args);
            }
        }

        public override void MouseDown(VirtualMouseClick args)
        {
            foreach (GLButton button in _buttons)
            {
                button.MouseDown(args);
            }
        }

        protected override void RenderInternal()
        {
            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            foreach (GLButton button in _buttons)
            {
                button.Render();
            }
        }

        public override void Logic(double msElapsed)
        {

        }

        public override bool Finished
        {
            get
            {
                return _finished;
            }
        }

        private void ButtonClicked(object source, ButtonPressedEventArgs args)
        {
            _numPlayers = int.Parse(args.SourceButtonId);
            EnqueueScreen(new NameEntryScreen("Player 1 name:"));
        }

        public override void ScreenAboveFinished(GameScreen aboveScreen)
        {
            if (aboveScreen is NameEntryScreen)
            {
                // Add player to the roster
                Tuple<string, string> playerNameAndAvatar = ((NameEntryScreen)aboveScreen).ReturnVal;
                string avKey = playerNameAndAvatar.Item2;
                GameState.Players.Add(new Player()
                {
                    Name = playerNameAndAvatar.Item1,
                    Score = 0,
                    Avatar = Resources.Avatars.AvailableAvatars[avKey]
                });

                Resources.Avatars.AvailableAvatars.Remove(avKey);

                // Do we need to prompt for more players?
                if (GameState.Players.Count < _numPlayers)
                {
                    int nextPlayer = GameState.Players.Count + 1;
                    EnqueueScreen(new NameEntryScreen("Player " + nextPlayer + " name:"));
                }
                else
                {
                    _finished = true;

                    // Shuffle the player list
                    List<Player> shuffledList = new List<Player>();
                    Random rand = new Random();
                    while (GameState.Players.Count > 0)
                    {
                        Player next = GameState.Players[rand.Next(0, GameState.Players.Count)];
                        shuffledList.Add(next);
                        GameState.Players.Remove(next);
                    }

                    GameState.Players.AddRange(shuffledList);

                    // Play the game start fanfare
                    Resources.AudioSubsystem.StopMusic();
                    Resources.AudioSubsystem.PlaySound(Resources.SoundEffects["starting"]);
                }
            }
        }
    }
}
