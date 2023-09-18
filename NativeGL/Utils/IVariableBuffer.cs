using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Durandal.Common.Utils.IO
{
    public interface IVariableBuffer
    {
        int Read(byte[] targetBuffer, int offset, int count);

        void Write(byte[] chunk, int offset, int count);

        void CloseWrite();

        bool EndOfStream();

        int Available();
    }
}
