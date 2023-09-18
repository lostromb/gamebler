using System;
using System.IO;

namespace Durandal.Common.Utils.IO
{
    public class CircularBufferStream : Stream
    {
        private byte[] _buf;
        private int _idx;
        private int _capacity;
        private int _available;

        public CircularBufferStream(int capacity)
        {
            _capacity = capacity;
            _buf = new byte[_capacity];
            _idx = 0;
            _available = 0;
        }

        public int Capacity
        {
            get
            {
                return _capacity;
            }
        }

        public int Available
        {
            get
            {
                return _available;
            }
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

        public override long Length
        {
            get
            {
                return _capacity;
            }
        }

        public override long Position
        {
            get
            {
                return 0;
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int amountToActuallyRead = System.Math.Min(count, _available);
            int part1Size = System.Math.Min(amountToActuallyRead, _capacity - _idx);
            int part2Size = System.Math.Max(0, amountToActuallyRead - part1Size);

            Buffer.BlockCopy(_buf, _idx, buffer, offset, part1Size);
            if (part2Size > 0)
            {
                Buffer.BlockCopy(_buf, 0, buffer, offset + part2Size, part2Size);
            }

            _idx = (_idx + amountToActuallyRead) % _capacity;
            _available -= amountToActuallyRead;
            return amountToActuallyRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            int amountToActuallyWrite = System.Math.Min(count, _capacity);
            int inputOffset = offset + System.Math.Max(0, count - amountToActuallyWrite);
            
            int part1Size = System.Math.Min(amountToActuallyWrite, _capacity - _idx);
            int part2Size = System.Math.Max(0, amountToActuallyWrite - part1Size);

            Buffer.BlockCopy(buffer, inputOffset, _buf, _idx, part1Size);
            if (part2Size > 0)
            {
                Buffer.BlockCopy(buffer, inputOffset + part1Size, _buf, 0, part2Size);
            }
            
            _available += amountToActuallyWrite;
        }
    }
}
