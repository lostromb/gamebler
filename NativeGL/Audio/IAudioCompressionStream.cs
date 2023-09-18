﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Durandal.Common.Audio.Interfaces
{
    public interface IAudioCompressionStream
    {
        byte[] Compress(AudioChunk input);
        byte[] Close();
        string GetEncodeParams();
    }
}
