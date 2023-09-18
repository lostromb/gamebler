using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Durandal.Common.Audio
{
    using Client;
    using System.ComponentModel;
    using System.Threading;

    public class ChunkedAudioStream
    {
        // fixme the wait operations in this class should use a realtimeprovider

        private bool _closed;
        private Mutex _lock; // fixme mutex is probably not the best design here
        private Queue<AudioChunk> _buffer;
        private EventWaitHandle _writeSignal;

        public ChunkedAudioStream()
        {
            _buffer = new Queue<AudioChunk>();
            _lock = new Mutex();
            _closed = false;
            _writeSignal = new EventWaitHandle(false, EventResetMode.AutoReset);
        }

        public bool EndOfStream
        {
            get
            {
                _lock.WaitOne();
                try
                {
                    return _closed && _buffer.Count == 0;
                }
                finally
                {
                    _lock.ReleaseMutex();
                }
            }
        }

        public bool Write(AudioChunk data, bool closeStream = false)
        {
            _lock.WaitOne();
            try
            {
                if (_closed)
                {
                    // Can't write to a closed stream
                    throw new ObjectDisposedException("Cannot write to a closed audio stream");
                }

                if (closeStream)
                {
                    _closed = true;
                }

                _buffer.Enqueue(data);
                _writeSignal.Set();
            }
            finally
            {
                _lock.ReleaseMutex();
            }

            OnDataWritten(data);
            if (closeStream)
            {
                OnFinished();
            }

            return true;
        }

        /// <summary>
        /// Attempts to read audio from the stream
        /// </summary>
        /// <param name="msToWait"></param>
        /// <returns></returns>
        public AudioChunk Read(int msToWait = 100)
        {
            if (!_lock.WaitOne(msToWait))
            {
                return null;
            }

            try
            {
                if (_closed && _buffer.Count == 0)
                {
                    // Stream is closed, return null
                    // And also set the write signal so any other waiting processes will stop blocking
                    _writeSignal.Set();
                    return null;
                }
            }
            finally
            {
                _lock.ReleaseMutex();
            }

            // Block on write if we need to do so (but unlock the mutex first so another thread can write)
            if (!_writeSignal.WaitOne(msToWait))
            {
                return null;
            }

            try
            {
                // Now actually read the data
                _lock.WaitOne();

                // Race condition check - make sure the stream has not been closed in between the last read + write
                if (_closed && _buffer.Count == 0)
                {
                    _writeSignal.Set();
                    return null;
                }

                AudioChunk returnVal = _buffer.Dequeue();
                if (_buffer.Count > 0)
                {
                    // Set the write flag which tells other readers that more data is available
                    _writeSignal.Set();
                }
                return returnVal;
            }
            finally
            {
                _lock.ReleaseMutex();
            }
        }

        public event EventHandler<AudioEventArgs> DataWritten;
        public event EventHandler<EventArgs> Finished;

        protected virtual void OnDataWritten(AudioChunk audio)
        {
            EventHandler<AudioEventArgs> handler = this.DataWritten;
            if (handler != null)
            {
                handler(this, new AudioEventArgs(audio));
            }
        }

        protected virtual void OnFinished()
        {
            EventHandler<EventArgs> handler = this.Finished;
            if (handler != null)
            {
                handler(this, null);
            }
        }
    }
}
