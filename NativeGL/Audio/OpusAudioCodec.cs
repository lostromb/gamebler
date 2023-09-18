using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Durandal.Common.Utils.IO;

namespace Durandal.Common.Audio.Codecs
{
    using System.Diagnostics;
    using System.IO;
    using System.Threading;

    using Durandal.Common.Audio;
    using Durandal.Common.Audio.Interfaces;
    using Durandal.Common.Logger;
    using Durandal.Common.Utils;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using Concentus.Structs;
    using Concentus.Enums;
    using Concentus;

    /// <summary>
    /// Wrapper for OPUS encoder/decoder (using Concentus as native backend)
    /// </summary>
    public class OpusAudioCodec : IAudioCodec
    {
        private const int FRAME_SIZE = 10;
        private int _bitrate = 32;
        private int _complexity = 0;
        private ILogger _logger;

        public OpusAudioCodec(ILogger logger, int complexity = 0)
        {
            _logger = logger;
            _complexity = complexity;
        }

        /// <summary>
        /// The bitrate to use for encoding
        /// </summary>
        public int QualityKbps
        {
            get
            {
                return _bitrate;
            }
            set
            {
                _bitrate = value;
            }
        }

        public string GetFormatCode()
        {
            return "opus";
        }

        public string GetDescription()
        {
            return "Opus audio codec 1.1.2 (via " + Concentus.CodecHelpers.GetVersionString() + ")";
        }

        public bool Initialize()
        {
            return true;
        }

        public IAudioCompressionStream CreateCompressionStream(int inputSampleRate, string traceId = null)
        {
            OpusCompressionStream returnVal = new OpusCompressionStream(inputSampleRate, _bitrate, _complexity, _logger.Clone(_logger.ComponentName, traceId ?? _logger.TraceId));
            if (!returnVal.Initialize())
            {
                return null;
            }

            return returnVal;
        }

        public IAudioDecompressionStream CreateDecompressionStream(string encodeParams, string traceId = null)
        {
            OpusDecompressionStream returnVal = new OpusDecompressionStream(encodeParams, _logger.Clone(_logger.ComponentName, traceId ?? _logger.TraceId));
            if (!returnVal.Initialize())
            {
                return null;
            }

            return returnVal;
        }

        public class OpusCompressionStream : IAudioCompressionStream
        {
            private BasicBufferShort _incomingSamples;

            /// <summary>
            /// The native pointer to the encoder state object
            /// </summary>
            private OpusEncoder concentusEncoder;

            private int _sampleRate = AudioUtils.DURANDAL_INTERNAL_SAMPLE_RATE;
            private int _qualityKbps;
            private int _complexity;
            private ILogger _logger;

            public OpusCompressionStream(int sampleRate, int qualityKbps, int complexity, ILogger logger)
            {
                _sampleRate = FindSampleRateFloor(sampleRate);
                _qualityKbps = qualityKbps;
                _logger = logger;
                _complexity = complexity;

                // Buffer for 1 second of input
                _incomingSamples = new BasicBufferShort(_sampleRate * 1);
            }

            private int FindSampleRateFloor(int desiredSampleRate)
            {
                if (desiredSampleRate >= 48000)
                {
                    return 48000;
                }
                if (desiredSampleRate >= 24000)
                {
                    return 24000;
                }
                if (desiredSampleRate >= 16000)
                {
                    return 16000;
                }
                if (desiredSampleRate >= 12000)
                {
                    return 12000;
                }

                return 8000;
            }

            public bool Initialize()
            {
                try
                {
                    concentusEncoder = OpusEncoder.Create(_sampleRate, 1, OpusApplication.OPUS_APPLICATION_AUDIO);

                    // Set the encoder bitrate and complexity
                    concentusEncoder.Bitrate = _qualityKbps * 1024;
                    concentusEncoder.Complexity = _complexity;
                    concentusEncoder.ForceMode = OpusMode.MODE_CELT_ONLY; // CELT mode is much faster than hybrid so force it
                    concentusEncoder.UseVBR = true;

                    _logger.Log("Initializing Opus compression stream with samplerate=" + _sampleRate + ", bitrate=" + _qualityKbps + ", complexity=" + _complexity);

                    return true;
                }
                catch (OpusException e)
                {
                    _logger.Log("Exception while initializing Opus encoder!", LogLevel.Err);
                    _logger.Log(e.Message);
                    return false;
                }
            }

            private int GetFrameSize()
            {
                // 10ms window is used for all packets
                return _sampleRate * FRAME_SIZE / 1000;
            }

            public byte[] Compress(AudioChunk input)
            {
                int frameSize = GetFrameSize();

                if (input != null)
                {
                    short[] newData = input/*.ResampleTo(_sampleRate)*/.Data;
                    if (_incomingSamples.Capacity() - _incomingSamples.Available() < newData.Length)
                    {
                        _logger.Log("Buffer overrun! Too much input audio was piped into Opus compression stream at once", LogLevel.Wrn);
                    }

                    _incomingSamples.Write(newData);
                }
                else
                {
                    // If input is null, assume we are at end of stream and pad the output with zeroes
                    int paddingNeeded = _incomingSamples.Available() % frameSize;
                    if (paddingNeeded > 0)
                    {
                        _incomingSamples.Write(new short[paddingNeeded]);
                    }
                }

                try
                {
                    // Calculate the approximate amount of bits required to compress the incoming audio using the current opus bitrate
                    float timeSpanMs = (float)(_incomingSamples.Available() * 1000L / AudioUtils.DURANDAL_INTERNAL_SAMPLE_RATE);
                    float bytesPerMs = _qualityKbps * 128 / 1000;
                    int approxOpusFrameSize = Math.Min(1250, Math.Max(10, (int)(FRAME_SIZE * bytesPerMs * 1.10f)));
                    int approxOutBufferSize = (int)(timeSpanMs * bytesPerMs * 1.10f);
                    byte[] outputBuffer = new byte[approxOutBufferSize];
                    int outCursor = 0;

                    while (outCursor <= approxOutBufferSize - approxOpusFrameSize && _incomingSamples.Available() >= frameSize)
                    {
                        short[] nextFrameData = _incomingSamples.Read(frameSize);
                        short thisPacketSize = (short)concentusEncoder.Encode(nextFrameData, 0, frameSize, outputBuffer, outCursor + 2, approxOpusFrameSize - 2);
                        byte[] packetSize = BitConverter.GetBytes(thisPacketSize);
                        outputBuffer[outCursor++] = packetSize[0];
                        outputBuffer[outCursor++] = packetSize[1];
                        outCursor += thisPacketSize;
                    }

                    byte[] finalOutput = new byte[outCursor];
                    Array.Copy(outputBuffer, 0, finalOutput, 0, outCursor);
                    return finalOutput;
                }
                catch (OpusException e)
                {
                    _logger.Log("Opus encoder threw an exception: " + e.Message);
                    return new byte[0];
                }
            }

            public byte[] Close()
            {
                byte[] trailer = Compress(null);
                return trailer;
            }

            public string GetEncodeParams()
            {
                return string.Format("samplerate={0} q=0 framesize={1}", _sampleRate, FRAME_SIZE);
            }
        }

        public class OpusDecompressionStream : IAudioDecompressionStream
        {
            private OpusDecoder _hDecoder;

            private BasicBufferByte _incomingBytes;

            private int _nextPacketSize = 0;
            private float _outputFrameLengthMs;
            private int _outputSampleRate;
            private ILogger _logger;

            public OpusDecompressionStream(string encodeParams, ILogger logger)
            {
                _incomingBytes = new BasicBufferByte(10000);
                _logger = logger;

                Match sampleRateParse = Regex.Match(encodeParams, "samplerate=([0-9]+)");
                if (sampleRateParse.Success)
                {
                    _outputSampleRate = int.Parse(sampleRateParse.Groups[1].Value);
                }
                else
                {
                    _outputSampleRate = AudioUtils.DURANDAL_INTERNAL_SAMPLE_RATE;
                }

                Match frameSizeParse = Regex.Match(encodeParams, "framesize=([0-9]+)");
                if (frameSizeParse.Success)
                {
                    _outputFrameLengthMs = float.Parse(frameSizeParse.Groups[1].Value);
                }
                else
                {
                    _outputFrameLengthMs = FRAME_SIZE;
                }

                _logger.Log("Initializing Opus decompression stream with samplerate=" + _outputSampleRate + " and framesize=" + _outputFrameLengthMs);
            }

            public bool Initialize()
            {
                try
                {
                    _hDecoder = OpusDecoder.Create(_outputSampleRate, 1);
                    return true;
                }
                catch (OpusException e)
                {
                    _logger.Log("Exception while initializing Opus decoder!", LogLevel.Err);
                    _logger.Log(e.Message);
                    return false;
                }
            }

            public AudioChunk Decompress(byte[] input)
            {
                int frameSize = GetFrameSize();

                if (input != null)
                {
                    _incomingBytes.Write(input);
                }

                IList<short[]> outputFrames = new List<short[]>();
                int outputLength = 0;
                int outCursor = 0;
                if (_nextPacketSize <= 0 && _incomingBytes.Available() >= 2)
                {
                    byte[] packetSize = _incomingBytes.Read(2);
                    _nextPacketSize = BitConverter.ToInt16(packetSize, 0);
                }

                try
                {
                    while (_nextPacketSize > 0 && _incomingBytes.Available() >= _nextPacketSize)
                    {
                        short[] outputBuffer = new short[frameSize];
                        byte[] nextPacketData = _incomingBytes.Read(_nextPacketSize);
                        int thisFrameSize = _hDecoder.Decode(nextPacketData, 0, _nextPacketSize, outputBuffer, 0, frameSize, false);
                        outCursor += thisFrameSize * 2;
                        outputFrames.Add(outputBuffer);
                        outputLength += outputBuffer.Length;

                        if (_incomingBytes.Available() >= 2)
                        {
                            byte[] packetSize = _incomingBytes.Read(2);
                            _nextPacketSize = BitConverter.ToInt16(packetSize, 0);
                        }
                        else
                        {
                            _nextPacketSize = 0;
                        }
                    }

                    if (outputLength == 0)
                        return null;

                    short[] finalOutput = new short[outputLength];
                    int outCur = 0;
                    foreach (short[] frame in outputFrames)
                    {
                        Array.Copy(frame, 0, finalOutput, outCur, frame.Length);
                        outCur += frame.Length;
                    }

                    return new AudioChunk(finalOutput, _outputSampleRate);
                }
                catch (OpusException e)
                {
                    _logger.Log("Opus decoder threw an exception: " + e.Message);
                    return null;
                }
            }

            private int GetFrameSize()
            {
                // 20ms window is the default used for all packets in this encoder
                return (int)(_outputSampleRate * _outputFrameLengthMs / 1000);
            }

            public AudioChunk Close()
            {
                AudioChunk trailer = Decompress(null);
                return trailer;
            }
        }
    }
}
