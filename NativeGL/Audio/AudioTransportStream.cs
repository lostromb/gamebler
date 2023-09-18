using Durandal.Common.Utils.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Durandal.Common.Utils
{
    public class AudioTransportStream : IVariableBuffer
    {
        private readonly SingleUseBuffer _buffer;
        private readonly int _sampleRate;

        // Enforces byte alignment of the input data
        private readonly int _writeChunkSize;
        private readonly int _byteAlignment;
        private readonly byte[] _orphanBytes;
        private int _orphanCount = 0;
        
        public AudioTransportStream(int sampleRate, int byteAlignment = 2)
        {
            _buffer = new SingleUseBuffer();
            _sampleRate = sampleRate;
            _byteAlignment = byteAlignment;
            _orphanBytes = new byte[_byteAlignment - 1];
            _writeChunkSize = _byteAlignment * 512;
        }

        public virtual string GetCodec()
        {
            return string.Empty;
        }

        public virtual string GetCodecParams()
        {
            return string.Empty;
        }

        public virtual int GetSampleRate()
        {
            return _sampleRate;
        }

        public void Write(byte[] inputData, int offset, int count)
        {
            // Chunk the input and pass it to the transformer implementation (to prevent buffer overruns if the transformer is an audio codec or something)
            byte[] inputChunk = null;
            int input_ptr;
            for (input_ptr = 0; input_ptr < count;)
            {
                int thisChunkSize = Math.Min(_writeChunkSize, count - input_ptr + _orphanCount);
                if (thisChunkSize != _writeChunkSize)
                {
                    // this path only happens on the last iteration
                    int newOrphanCount = thisChunkSize % _byteAlignment;

                    int thisChunkSizeByteAligned = thisChunkSize - newOrphanCount;

                    // Is this piece enough to make a complete byte-aligned chunk?
                    // If not, we append to the current list of orphans
                    if (thisChunkSizeByteAligned < _orphanCount)
                    {
                        Array.Copy(inputData, offset + input_ptr, _orphanBytes, _orphanCount, newOrphanCount - _orphanCount);
                        input_ptr += newOrphanCount - _orphanCount;
                        _orphanCount = newOrphanCount;
                        continue;
                    }

                    // create new byte-aligned array
                    inputChunk = new byte[thisChunkSizeByteAligned];

                    if (_orphanCount > 0)
                    {
                        // Copy old orphan data from side buffer
                        Array.Copy(_orphanBytes, 0, inputChunk, 0, _orphanCount);
                    }

                    // Copy byte-aligned data
                    if (thisChunkSizeByteAligned > _orphanCount)
                    {
                        Array.Copy(inputData, offset + input_ptr, inputChunk, _orphanCount, thisChunkSizeByteAligned - _orphanCount);
                    }

                    // Copy new orphans to side buffer
                    if (newOrphanCount > 0)
                    {
                        Array.Copy(inputData, offset + input_ptr + thisChunkSizeByteAligned - _orphanCount, _orphanBytes, 0, newOrphanCount);
                    }

                    input_ptr += thisChunkSizeByteAligned + newOrphanCount;
                    _orphanCount = newOrphanCount;
                }
                else
                {
                    if (inputChunk == null)
                    {
                        inputChunk = new byte[_writeChunkSize];
                    }

                    if (_orphanCount > 0)
                    {
                        Array.Copy(_orphanBytes, 0, inputChunk, 0, _orphanCount);
                    }

                    Array.Copy(inputData, offset + input_ptr, inputChunk, _orphanCount, thisChunkSize - _orphanCount);
                    input_ptr += thisChunkSize - _orphanCount;
                    _orphanCount = 0; // always set to zero since the write buffer is byte-aligned
                }

                if (inputChunk.Length > 0)
                {
                    byte[] transformedData = TransformOutput(inputChunk);
                    if (transformedData != null && transformedData.Length > 0)
                    {
                        _buffer.Write(transformedData, 0, transformedData.Length);
                    }
                }
            }
        }

        public int Read(byte[] targetBuffer, int offset, int length)
        {
            return _buffer.Read(targetBuffer, offset, length);
        }

        public void CloseWrite()
        {
            // Allow subclasses such as audio compressor streams to flush their output before we finally close the stream
            byte[] finalizedOutput = FinalizeOutput();
            if (finalizedOutput != null && finalizedOutput.Length > 0)
            {
                _buffer.Write(finalizedOutput, 0, finalizedOutput.Length);
            }

            _buffer.CloseWrite();
        }

        public bool EndOfStream()
        {
            return _buffer.EndOfStream();
        }

        public int Available()
        {
            return _buffer.Available();
        }

        /// <summary>
        /// This method allows subclasses to transform raw data that is written to the buffer
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        protected virtual byte[] TransformOutput(byte[] input)
        {
            return input;
        }

        /// <summary>
        /// This method allows subclasses to flush internal buffers before closing the write stream.
        /// Invoking this method signals that no further data will be written to this stream.
        /// </summary>
        /// <returns></returns>
        protected virtual byte[] FinalizeOutput()
        {
            return null;
        }
    }
}
