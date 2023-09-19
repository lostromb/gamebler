using NativeGL.Structures;
using System.Collections.Generic;

namespace NativeGL
{
    public class GlobalGameState
    {
        public List<Player> Players = new List<Player>();
        public List<SoundTestPrompt> MusicQuizSongs = new List<SoundTestPrompt>();
        public List<PictureQuizQuestion> PictureQuizQuestions = new List<PictureQuizQuestion>();
        public List<DescramblerPrompt> DescramblerImages = new List<DescramblerPrompt>();
        public List<WordDescramblerPrompt> WordDescramberWords = new List<WordDescramblerPrompt>();
    }
}
