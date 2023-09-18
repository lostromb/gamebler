namespace Durandal.Common.Client
{
    using System;

    using Durandal.Common.Audio;

    public class AudioEventArgs : EventArgs
    {
        public AudioEventArgs(AudioChunk audio)
        {
            this.Audio = audio;
        }

        public AudioChunk Audio { get; set; }
    }

    public class StreamingAudioEventArgs : EventArgs
    {
        public StreamingAudioEventArgs(ChunkedAudioStream stream)
        {
            this.Stream = stream;
        }

        public ChunkedAudioStream Stream { get; set; }
    }
}
