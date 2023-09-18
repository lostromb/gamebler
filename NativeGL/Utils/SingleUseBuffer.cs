using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Durandal.Common.Utils.IO
{
    /// <summary>
    /// This buffer is intended to support asynchronous reading and writing by separate producer and consumer threads.
    /// All it really is is a slightly different implementation of MemoryStream with some thread-safety and the all-powerful "EndOfStream" property.
    /// TODO: Perhaps then I should reimplement this with a RecyclableMemoryStream instead of a Queue<byte[]>
    /// </summary>
    public class SingleUseBuffer : IVariableBuffer
    {
        private Queue<byte[]> _chunks = new Queue<byte[]>();
        private byte[] _currentChunk = null;
        private int _index = 0;
        private bool _closed = false;
        private volatile int _available = 0;
        private volatile bool _endOfStream = false;
        
        public int Read(byte[] targetBuffer, int offset, int count)
        {
            lock (this)
            {
                int dataWritten = 0;

                while (_currentChunk != null && dataWritten < count)
                {
                    int amountToWrite = System.Math.Min(count - dataWritten, _currentChunk.Length - _index);
                    Array.Copy(_currentChunk, _index, targetBuffer, dataWritten + offset, amountToWrite);
                    dataWritten += amountToWrite;
                    _available -= amountToWrite;
                    _index += amountToWrite;

                    // Queue up a new chunk if we can
                    if (_index >= _currentChunk.Length)
                    {
                        if (_chunks.Count > 0)
                        {
                            _currentChunk = _chunks.Dequeue();
                            _index = 0;
                        }
                        else
                        {
                            _currentChunk = null;
                        }
                    }
                }

                _endOfStream = _closed && _currentChunk == null;

                return dataWritten;
            }
        }

        public void Write(byte[] chunk, int offset, int count)
        {
            byte[] copiedData = new byte[count];
            Array.Copy(chunk, offset, copiedData, 0, count);

            lock (this)
            {
                if (_currentChunk == null)
                {
                    // Enqueue immediately
                    _currentChunk = copiedData;
                    _index = 0;
                }
                else
                {
                    // Enqueue for later
                    _chunks.Enqueue(copiedData);
                }

                _available += count;
            }
        }

        /// <summary>
        /// Closes the write end of this pipe. The data will still sit in the buffer and be readable,
        /// and EndOfStream will not be signalled until the buffer is entirely emptied.
        /// </summary>
        public void CloseWrite()
        {
            lock (this)
            {
                _closed = true;
            }
        }

        public bool EndOfStream()
        {
            return _endOfStream;
        }

        /// <summary>
        /// Returns a MINIMUM estimate for how much buffer is available for consumption at the current time.
        /// </summary>
        /// <returns></returns>
        public int Available()
        {
            return _available;
        }
    }
}
