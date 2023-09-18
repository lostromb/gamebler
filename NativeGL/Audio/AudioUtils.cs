using System;
using System.IO;
using System.Threading.Tasks;

namespace Durandal.Common.Audio
{
    using Durandal.Common.Audio.Interfaces;
    
    using System.Collections.Generic;

    public static class AudioUtils
    {
        public const int DURANDAL_INTERNAL_SAMPLE_RATE = 48000;
        
        public static byte[] CompressAudioUsingStream(AudioChunk audio, IAudioCodec codec, out string encodeParams, string traceId = null)
        {
            return CompressAudioUsingStream(audio, codec.CreateCompressionStream(audio.SampleRate, traceId), out encodeParams);
        }

        /// <summary>
        /// Sends an entire audio chunk through a compressor and returns the byte array output and encode params
        /// </summary>
        /// <param name="audio"></param>
        /// <param name="compressor"></param>
        /// <param name="encodeParams"></param>
        /// <returns></returns>
        public static byte[] CompressAudioUsingStream(AudioChunk audio, IAudioCompressionStream compressor, out string encodeParams)
        {
            if (compressor == null)
            {
                throw new NullReferenceException("IAudioCompressionStream");
            }

            encodeParams = compressor.GetEncodeParams();

            // Chunk the input and pass it to the stream
            const int CHUNK_SIZE = 320;
            short[] samples = new short[CHUNK_SIZE];
            IList<byte[]> outputChunks = new List<byte[]>();
            int totalOutputSize = 0;
            int input_ptr;
            for (input_ptr = 0; input_ptr < audio.DataLength - CHUNK_SIZE; input_ptr += CHUNK_SIZE)
            {
                Array.Copy(audio.Data, input_ptr, samples, 0, CHUNK_SIZE);
                AudioChunk sample = new AudioChunk(samples, audio.SampleRate);
                byte[] thisPacket = compressor.Compress(sample);
                if (thisPacket != null && thisPacket.Length > 0)
                {
                    outputChunks.Add(thisPacket);
                    totalOutputSize += thisPacket.Length;
                }
            }
            if (input_ptr < audio.DataLength)
            {
                short[] tail = new short[audio.DataLength - input_ptr];
                Array.Copy(audio.Data, input_ptr, tail, 0, tail.Length);
                AudioChunk sample = new AudioChunk(tail, audio.SampleRate);
                byte[] thisPacket = compressor.Compress(sample);
                if (thisPacket != null)
                {
                    outputChunks.Add(thisPacket);
                    totalOutputSize += thisPacket.Length;
                }
            }

            byte[] footer = compressor.Close();
            if (footer != null && footer.Length > 0)
            {
                outputChunks.Add(footer);
                totalOutputSize += footer.Length;
            }

            byte[] returnVal = new byte[totalOutputSize];
            int outCur = 0;
            foreach (byte[] chunk in outputChunks)
            {
                Array.Copy(chunk, 0, returnVal, outCur, chunk.Length);
                outCur += chunk.Length;
            }
            return returnVal;
        }

        public static AudioChunk DecompressAudioUsingStream(byte[] input, IAudioCodec codec, string encodeParams, string traceId = null)
        {
            return DecompressAudioUsingStream(input, codec.CreateDecompressionStream(encodeParams, traceId));
        }

        /// <summary>
        /// Sends an encoded audio sample through a decompressor and returns the decoded audio
        /// </summary>
        /// <param name="input"></param>
        /// <param name="decompressor"></param>
        /// <returns></returns>
        public static AudioChunk DecompressAudioUsingStream(byte[] input, IAudioDecompressionStream decompressor)
        {
            if (decompressor == null)
            {
                throw new NullReferenceException("IAudioDecompressionStream");
            }

            BucketAudioStream audioOut = new BucketAudioStream();
            int outSampleRate = AudioUtils.DURANDAL_INTERNAL_SAMPLE_RATE;
            const int CHUNK_SIZE = 120;
            byte[] chunk = new byte[CHUNK_SIZE];
            int input_ptr;
            for (input_ptr = 0; input_ptr < input.Length - CHUNK_SIZE; input_ptr += CHUNK_SIZE)
            {
                Array.Copy(input, input_ptr, chunk, 0, CHUNK_SIZE);
                AudioChunk thisPacket = decompressor.Decompress(chunk);
                if (thisPacket != null && thisPacket.DataLength > 0)
                {
                    audioOut.Write(thisPacket.Data);
                    outSampleRate = thisPacket.SampleRate;
                }
            }
            if (input_ptr < input.Length)
            {
                byte[] tail = new byte[input.Length - input_ptr];
                Array.Copy(input, input_ptr, tail, 0, tail.Length);
                AudioChunk thisPacket = decompressor.Decompress(tail);
                if (thisPacket != null && thisPacket.DataLength > 0)
                {
                    audioOut.Write(thisPacket.Data);
                    outSampleRate = thisPacket.SampleRate;
                }
            }

            AudioChunk final = decompressor.Close();
            if (final != null && final.DataLength > 0)
            {
                audioOut.Write(final.Data);
                outSampleRate = final.SampleRate;
            }

            short[] allAudio = audioOut.GetAllData();

            return new AudioChunk(allAudio, outSampleRate);
        }
    }
}
