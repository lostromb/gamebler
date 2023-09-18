using Durandal.Common.Audio.Interfaces;
using Durandal.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Durandal.Common.Audio
{
    public class AudioDecompressorTransport : AudioTransportStream
    {
        private IAudioCodec _codec;
        private IAudioDecompressionStream _decompressor;

        public AudioDecompressorTransport(IAudioCodec codec, string encodeParams, int outputSampleRate, string traceId = null)
            : base(outputSampleRate, 1)
        {
            _codec = codec;
            _decompressor = _codec.CreateDecompressionStream(encodeParams, traceId);
        }

        protected override byte[] TransformOutput(byte[] input)
        {
            AudioChunk audio = _decompressor.Decompress(input);

            if (audio == null)
            {
                return new byte[0];
            }

            return audio.GetDataAsBytes();
        }

        protected override byte[] FinalizeOutput()
        {
            AudioChunk audio = _decompressor.Close();

            if (audio == null)
            {
                return new byte[0];
            }

            return audio.GetDataAsBytes();
        }
    }
}
