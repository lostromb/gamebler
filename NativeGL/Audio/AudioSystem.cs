using Durandal.Common.Audio;
using Durandal.Common.Audio.Codecs;
using Durandal.Common.Audio.Codecs.Opus;
using Durandal.Common.Audio.Components;
using Durandal.Common.Logger;
using Durandal.Common.Tasks;
using Durandal.Common.Time;
using Durandal.Extensions.NativeAudio;
using Durandal.Extensions.NativeAudio.Codecs;
using Durandal.Extensions.NAudio.Devices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NativeGL.Audio
{
    public class AudioSystem
    {
        private readonly object _lock = new object();
        private readonly IAudioGraph _audioGraph;
        private readonly AudioSampleFormat _audioOutputFormat;
        private readonly IAudioRenderDevice _speakers;
        private readonly LinearMixerAutoConforming _mixer;
        private readonly IOpusCodecProvider _opus;

        private AudioConcatenator _currentMusicConcatenator = null;
        private AudioDecoder _nextMusicPlayer = null;

        public AudioSystem(ILogger logger)
        {
            IResamplerFactory resamplerFactory = new NativeSpeexResamplerFactory();
            _opus = new NativeOpusCodecProvider();
            _audioOutputFormat = AudioSampleFormat.Mono(48000);
            _audioGraph = new AudioGraph(AudioGraphCapabilities.Concurrent);
            _speakers = new WasapiPlayer(_audioGraph, _audioOutputFormat, "Speakers", logger.Clone("Speakers"));
            _mixer = new LinearMixerAutoConforming(_audioGraph, _audioOutputFormat, "Mixer", resamplerFactory, readForever: true);

            _mixer.ConnectOutput(_speakers);
            _speakers.StartPlayback(DefaultRealTimeProvider.Singleton).Await();
        }

        public void PlayMusic(string musicName)
        {
                StopMusic();

            Task.Run(async () =>
            {
                FileInfo musicIntroFilePath = new FileInfo(@".\Resources\Music\" + musicName + "_Intro.opus");
                FileInfo musicFilePath = new FileInfo(@".\Resources\Music\" + musicName + ".opus");
                if (musicIntroFilePath.Exists)
                {
                    AudioDecoder musicIntroDecoder = new OggOpusDecoder(_audioGraph, "MusicStream", TimeSpan.FromMilliseconds(100), _opus);
                    await musicIntroDecoder.Initialize(new FileStream(musicFilePath.FullName, FileMode.Open, FileAccess.Read), true, CancellationToken.None, DefaultRealTimeProvider.Singleton);
                    
                    lock (_lock)
                    {
                        _currentMusicConcatenator = new AudioConcatenator(_audioGraph, musicIntroDecoder.OutputFormat, "MusicConcat", readForever: false);
                        _currentMusicConcatenator.EnqueueInput(musicIntroDecoder, musicName, takeOwnership: true);
                        _currentMusicConcatenator.ChannelFinishedEvent.Subscribe(HandleMusicFinished);
                        _currentMusicConcatenator.TakeOwnershipOfDisposable(musicIntroDecoder);
                    }
                }

                AudioDecoder musicDecoder = new OggOpusDecoder(_audioGraph, "MusicStream", TimeSpan.FromMilliseconds(100), _opus);
                await musicDecoder.Initialize(new FileStream(musicFilePath.FullName, FileMode.Open, FileAccess.Read), true, CancellationToken.None, DefaultRealTimeProvider.Singleton);
                
                lock (_lock)
                {
                    _currentMusicConcatenator = new AudioConcatenator(_audioGraph, musicDecoder.OutputFormat, "MusicConcat", readForever: false);
                    _currentMusicConcatenator.EnqueueInput(musicDecoder, musicName, takeOwnership: true);
                    _currentMusicConcatenator.ChannelFinishedEvent.Subscribe(HandleMusicFinished);
                    _currentMusicConcatenator.TakeOwnershipOfDisposable(musicDecoder);
                    _mixer.AddInput(_currentMusicConcatenator, takeOwnership: false);
                }
            });
        }

        private async Task HandleMusicFinished(object source, PlaybackFinishedEventArgs args, IRealTimeProvider realTime)
        {
            lock (_lock)
            {
                string musicName = args.ChannelToken as string;

                // Loop the music
            }
        }

        public float[] GetSpectrograph()
        {
            return new float[100];
        }

        public void StopMusic()
        {
            lock (_lock)
            {
                if (_currentMusicConcatenator != null)
                {
                    _currentMusicConcatenator.DisconnectOutput();
                    _currentMusicConcatenator.ChannelFinishedEvent.TryUnsubscribe(HandleMusicFinished);
                    _currentMusicConcatenator.Dispose();
                    _currentMusicConcatenator = null;
                }
            }
        }

        public void PlaySound(AudioSample sample)
        {
            _mixer.AddInput(new FixedAudioSampleSource(_audioGraph, sample, null), takeOwnership: true);
        }
    }
}
