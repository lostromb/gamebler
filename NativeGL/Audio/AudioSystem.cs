using Durandal.Common.Audio;
using Durandal.Common.Audio.Codecs;
using Durandal.Common.Audio.Codecs.Opus;
using Durandal.Common.Audio.Components;
using Durandal.Common.Logger;
using Durandal.Common.MathExt;
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
        private const int SPECTOGRAPH_WIDTH = 512;
        private readonly object _lock = new object();
        private readonly IAudioGraph _audioGraph;
        private readonly AudioSampleFormat _audioOutputFormat;
        private readonly IAudioRenderDevice _speakers;
        private readonly LinearMixerAutoConforming _mixer;
        private readonly IOpusCodecProvider _opus;
        private readonly AudioPeekBuffer _peekBuffer;
        private readonly float[] _currentWaveform = new float[SPECTOGRAPH_WIDTH];
        private readonly float[] _currentSpectrum = new float[SPECTOGRAPH_WIDTH];

        private AudioConcatenator _currentMusicConcatenator = null;
        private MusicIdentifier _currentlyPlayingAudio;

        public AudioSystem(ILogger logger)
        {
            IResamplerFactory resamplerFactory = new NativeSpeexResamplerFactory();
            _opus = new NativeOpusCodecProvider();
            _audioOutputFormat = AudioSampleFormat.Mono(48000);
            _audioGraph = new AudioGraph(AudioGraphCapabilities.Concurrent);
            _speakers = new WasapiPlayer(_audioGraph, _audioOutputFormat, "Speakers", logger.Clone("Speakers"));
            _mixer = new LinearMixerAutoConforming(_audioGraph, _audioOutputFormat, "Mixer", resamplerFactory, readForever: true);
            _peekBuffer = new AudioPeekBuffer(_audioGraph, _audioOutputFormat, "PeekBuffer", AudioMath.ConvertSamplesPerChannelToTimeSpan(_audioOutputFormat.SampleRateHz, SPECTOGRAPH_WIDTH * 2));

            _mixer.ConnectOutput(_peekBuffer);
            _peekBuffer.ConnectOutput(_speakers);
            _speakers.StartPlayback(DefaultRealTimeProvider.Singleton).Await();
        }

        public void PlayMusic(string musicName)
        {
            StopMusic();

            Task.Run(async () =>
            {
                MusicIdentifier id = new MusicIdentifier()
                {
                    Id = Guid.NewGuid(),
                    SongName = musicName
                };

                // Enqueue at least 2 inputs to the concatenator
                FileInfo musicIntroFilePath = new FileInfo(@".\Resources\Music\" + musicName + "_Intro.opus");
                FileInfo musicFilePath = new FileInfo(@".\Resources\Music\" + musicName + ".opus");
                if (!musicFilePath.Exists)
                {
                    DebugLogger.Default.Log("Couldn't find music file " + musicFilePath.FullName);
                    return;
                }

                //AudioSampleFormat HACKformat = AudioSampleFormat.Mono(48000);

                if (musicIntroFilePath.Exists)
                {
                    AudioDecoder musicIntroDecoder = new OggOpusDecoder(_audioGraph, "MusicStream", TimeSpan.FromMilliseconds(100), _opus);
                    AudioInitializationResult ir1 = await musicIntroDecoder.Initialize(new FileStream(musicIntroFilePath.FullName, FileMode.Open, FileAccess.Read), true, CancellationToken.None, DefaultRealTimeProvider.Singleton);
                    
                    lock (_lock)
                    {
                        _currentMusicConcatenator = new AudioConcatenator(_audioGraph, musicIntroDecoder.OutputFormat, "MusicConcat", readForever: false);
                        _currentMusicConcatenator.EnqueueInput(musicIntroDecoder, id, takeOwnership: true);
                        _currentMusicConcatenator.ChannelFinishedEvent.Subscribe(HandleMusicFinished);
                        _currentMusicConcatenator.TakeOwnershipOfDisposable(musicIntroDecoder);
                    }
                }
                else
                {
                    AudioDecoder musicPart1Decoder = new OggOpusDecoder(_audioGraph, "MusicStream", TimeSpan.FromMilliseconds(100), _opus);
                    AudioInitializationResult ir1 = await musicPart1Decoder.Initialize(new FileStream(musicFilePath.FullName, FileMode.Open, FileAccess.Read), true, CancellationToken.None, DefaultRealTimeProvider.Singleton);

                    lock (_lock)
                    {
                        _currentMusicConcatenator = new AudioConcatenator(_audioGraph, musicPart1Decoder.OutputFormat, "MusicConcat", readForever: false);
                        _currentMusicConcatenator.EnqueueInput(musicPart1Decoder, id, takeOwnership: true);
                        _currentMusicConcatenator.ChannelFinishedEvent.Subscribe(HandleMusicFinished);
                        _currentMusicConcatenator.TakeOwnershipOfDisposable(musicPart1Decoder);
                    }
                }

                AudioDecoder musicPart2Decoder = new OggOpusDecoder(_audioGraph, "MusicStream", TimeSpan.FromMilliseconds(100), _opus);
                AudioInitializationResult ir2 = await musicPart2Decoder.Initialize(new FileStream(musicFilePath.FullName, FileMode.Open, FileAccess.Read), true, CancellationToken.None, DefaultRealTimeProvider.Singleton);
                
                lock (_lock)
                {
                    _currentMusicConcatenator.EnqueueInput(musicPart2Decoder, id, takeOwnership: true);
                    _currentMusicConcatenator.TakeOwnershipOfDisposable(musicPart2Decoder);
                    _mixer.AddInput(_currentMusicConcatenator, takeOwnership: false);
                    _currentlyPlayingAudio = id;
                }
            });
        }

        private async Task HandleMusicFinished(object source, PlaybackFinishedEventArgs args, IRealTimeProvider realTime)
        {
            MusicIdentifier musicName = (MusicIdentifier)args.ChannelToken;

            if (_currentlyPlayingAudio.Id == musicName.Id)
            {
                FileInfo musicFilePath = new FileInfo(@".\Resources\Music\" + musicName.SongName + ".opus");
                AudioDecoder musicDecoder = new OggOpusDecoder(_audioGraph, "MusicStream", TimeSpan.FromMilliseconds(100), _opus);
                await musicDecoder.Initialize(new FileStream(musicFilePath.FullName, FileMode.Open, FileAccess.Read), true, CancellationToken.None, DefaultRealTimeProvider.Singleton);

                lock (_lock)
                {
                    _currentMusicConcatenator.EnqueueInput(musicDecoder, musicName, takeOwnership: true);
                    _currentMusicConcatenator.TakeOwnershipOfDisposable(musicDecoder);
                }
            }
        }

        public float[] GetSpectrograph()
        {
            int actualWaveformLength;
            long bufferStartTimestamp;
            _peekBuffer.PeekAtBuffer(_currentWaveform, 0, SPECTOGRAPH_WIDTH, out actualWaveformLength, out bufferStartTimestamp);

            if (actualWaveformLength < SPECTOGRAPH_WIDTH)
            {
                return _currentSpectrum;
            }

            ComplexF[] complex = new ComplexF[SPECTOGRAPH_WIDTH];
            for (int c = 0; c < SPECTOGRAPH_WIDTH; c++)
            {
                complex[c] = new ComplexF(_currentWaveform[c], 0);
            }

            Fourier.FFT(complex, SPECTOGRAPH_WIDTH, FourierDirection.Forward);
            for (int c = 0; c < _currentSpectrum.Length; c++)
            {
                _currentSpectrum[c] = complex[c].GetModulus();
            }

            return _currentSpectrum;
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

        private struct MusicIdentifier
        {
            public Guid Id;
            public string SongName;
        }
    }
}
