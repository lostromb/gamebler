using Durandal.Common.Audio;
using Durandal.Common.Audio.Components;
using NativeGL.Audio;
using NativeGL.Structures;
using QuickFont;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NativeGL
{
    public class StaticResources
    {
        public AvatarCollection Avatars;
        public Dictionary<string, GLShaderProgram> Shaders = new Dictionary<string, GLShaderProgram>();
        public Dictionary<string, GLTexture> Textures = new Dictionary<string, GLTexture>();
        public Dictionary<string, AudioSample> SoundEffects = new Dictionary<string, AudioSample>();
        public Dictionary<string, QFont> Fonts = new Dictionary<string, QFont>();
        public AudioSystem AudioSubsystem;
    }
}
