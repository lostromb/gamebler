using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Durandal.Common.Audio
{
    public class ChannelFinishedEventArgs : EventArgs
    {
        public object ChannelToken;
        public bool WasStream;
    }
}
