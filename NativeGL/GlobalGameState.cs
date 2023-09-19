using NativeGL.Structures;
using System.Collections.Generic;

namespace NativeGL
{
    public class GlobalGameState
    {
        public List<Player> Players = new List<Player>();
        public List<string> MusicQuizSongs = new List<string>();
        public List<PictureQuizQuestion> PictureQuizQuestions = new List<PictureQuizQuestion>();
        public List<string> DescramblerImages = new List<string>();
        public List<string> WordDescramberWords = new List<string>();
        public List<List<string>> Shoutouts = new List<List<string>>();
    }
}
