using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NativeGL
{
    public class HighPrecisionTimer
    {
        private Stopwatch _watch = new Stopwatch();

        public void Start()
        {
            _watch.Start();
        }

        public void Stop()
        {
            _watch.Stop();
        }

        public double ElapsedMs
        {
            get
            {
                return _watch.Elapsed.TotalMilliseconds;
            }
        }
    }
}
