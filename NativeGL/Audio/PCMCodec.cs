﻿using Durandal.Common.Audio.Interfaces;
using Durandal.Common.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Durandal.Common.Audio.Codecs
{
    public class PCMCodec : IAudioCodec
    {
        private ILogger _logger;

        public PCMCodec(ILogger logger = null)
        {
            if (logger != null)
                _logger = logger;
            else
                _logger = NullLogger.Singleton;
        }

        public IAudioCompressionStream CreateCompressionStream(int inputSampleRate, string traceId = null)
        {
            return new PCMCompressor(inputSampleRate);
        }

        public IAudioDecompressionStream CreateDecompressionStream(string encodeParams, string traceId = null)
        {
            return new PCMDecompressor(encodeParams, _logger.Clone("PCMDecompressor", traceId));
        }

        public string GetDescription()
        {
            return "Uncompressed PCM s16le";
        }

        public string GetFormatCode()
        {
            return "pcm";
        }

        public bool Initialize()
        {
            return true;
        }

        private class PCMCompressor : IAudioCompressionStream
        {
            private int _sampleRate;

            public PCMCompressor(int sampleRate)
            {
                _sampleRate = sampleRate;
            }

            public byte[] Compress(AudioChunk input)
            {
                return input.GetDataAsBytes();
            }

            public byte[] Close()
            {
                return null;
            }

            public string GetEncodeParams()
            {
                return "samplerate=" + _sampleRate;
            }
        }

        public class PCMDecompressor : IAudioDecompressionStream
        {
            // This value is used to keep the block alignment to 2 bytes.
            // Otherwise the audio will get scrambled if we read an odd number of bytes.
            private int blockAlign = 0;

            private byte _oddByte;

            private readonly Regex _sampleRateParser = new Regex("samplerate=([0-9]+)");

            /// <summary>
            /// The sample rate, parsed from the encode params
            /// </summary>
            private int _sampleRate;

            public PCMDecompressor(int sampleRate)
            {
                _sampleRate = sampleRate;
            }

            public PCMDecompressor(string encodeParams, ILogger logger)
            {
                Match m = _sampleRateParser.Match(encodeParams);
                if (m.Success)
                {
                    if (!int.TryParse(m.Groups[1].Value, out _sampleRate))
                    {
                        logger.Log("Could not parse sample rate from wave stream parameters! Expecting \"samplerate=16000\". Params are \"" + encodeParams + "\"", LogLevel.Wrn);
                        _sampleRate = AudioUtils.DURANDAL_INTERNAL_SAMPLE_RATE;
                    }
                }
                else
                {
                    logger.Log("Could not find sample rate info wave stream parameters! Expecting \"samplerate=16000\". Params are \"" + encodeParams + "\"", LogLevel.Wrn);
                    _sampleRate = AudioUtils.DURANDAL_INTERNAL_SAMPLE_RATE;
                }
            }

            public AudioChunk Decompress(byte[] input)
            {
                // Note that input may be an odd number of bytes. If so, keep the odd byte cached
                // That's what these variables are for
                int oldBlockAlign = blockAlign;
                byte oldOddByte = _oddByte;

                // Calculate the block size factoring in 2-byte alignment
                int blockSize = input.Length + oldBlockAlign;

                blockAlign = blockSize % 2;

                if (blockAlign == 1)
                {
                    blockSize -= 1;
                    _oddByte = input[input.Length - 1];
                }

                byte[] actualBuffer = new byte[blockSize];

                if (oldBlockAlign == 1)
                {
                    // There is an odd byte carried over from the last block
                    actualBuffer[0] = oldOddByte;
                    Array.Copy(input, 0, actualBuffer, 1, blockSize - 1);
                }
                else
                {
                    Array.Copy(input, 0, actualBuffer, 0, blockSize);
                }

                return new AudioChunk(actualBuffer, _sampleRate);
            }

            public AudioChunk Close()
            {
                return null;
            }
        }
    }
}
