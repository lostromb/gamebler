using NativeGL.Structures;
using System.Collections.Generic;

namespace NativeGL
{
    public class GlobalGameState
    {
        public List<Player> Players = new List<Player>();
        public List<string> MusicQuizSongs = new List<string>();
        public List<PictureQuizQuestion> PictureQuizQuestions = new List<PictureQuizQuestion>();
        public List<DescramblerPrompt> DescramblerImages = new List<DescramblerPrompt>();
        public List<WordDescramblerPrompt> WordDescramberWords = new List<WordDescramblerPrompt>();
    }
}
