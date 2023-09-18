using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Durandal.Common.Audio.Interfaces
{
    public interface IAudioPlayer : IDisposable
    {
        void PlaySound(AudioChunk chunk, object channelToken = null);

        void PlayStream(ChunkedAudioStream stream, object channelToken = null);

        bool IsPlaying();

        void StopPlaying();

        event EventHandler<ChannelFinishedEventArgs> ChannelFinished;

        void Suspend();

        void Resume();
    }
}
