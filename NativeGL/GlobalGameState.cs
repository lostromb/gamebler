using NativeGL.Structures;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NativeGL
{
    public class GlobalGameState
    {
        public List<Player> Players = new List<Player>();
        public List<PrincipleQuestion> GospelPrincipleQuestions = new List<PrincipleQuestion>();
        public List<string> MusicQuizSongs = new List<string>();
        public List<PictureQuizQuestion> PictureQuizQuestions = new List<PictureQuizQuestion>();
        public List<string> DescramblerImages = new List<string>();
        public List<ArticleOfFaith> ArticlesOfFaith = new List<ArticleOfFaith>();
        public List<string> WordDescramberWords = new List<string>();
        public List<List<string>> Shoutouts = new List<List<string>>();
    }
}
