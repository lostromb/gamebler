namespace NativeGL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements a simple realtime rate or throughput counter
    /// </summary>
    public class RateCounter
    {
        private readonly Queue<long> _events = new Queue<long>();
        private readonly object _mutex = new object();
        private readonly long _window;

        /// <summary>
        /// Builds a new rate counter
        /// </summary>
        /// <param name="windowSize">The amount of time to use for a window function. Smaller window = more granular, but more noisy results</param>
        /// <param name="customTimeProvider">A custom time provider, if your scenario is not being run at real time. If null, the regular wallclock time is used.</param>
        public RateCounter(TimeSpan windowSize)
        {
            if (windowSize.Ticks == 0)
            {
                throw new ArgumentOutOfRangeException("Window size must be non-zero");
            }

            _window = (long)windowSize.TotalMilliseconds;
        }

        /// <summary>
        /// Signals that an event happened at the current time, which increments the rate counter.
        /// The definition of "current time" depends on the IRealTimeProvider passed into the constructor, which
        /// is wallclock time by default
        /// </summary>
        public void Increment()
        {
            long curTime = DateTime.Now.Ticks / 10000;

            lock (_mutex)
            {
                // Add the new event
                _events.Enqueue(curTime);

                // Remove all outdated events
                long cutoffTime = curTime - _window;
                while (_events.Count > 0 && _events.Peek() < cutoffTime)
                {
                    _events.Dequeue();
                }
            }
        }

        /// <summary>
        /// Retrieves the current rate, in events per second.
        /// </summary>
        public double Rate
        {
            get
            {
                long curTime = DateTime.Now.Ticks / 10000;

                lock (_mutex)
                {
                    // Remove all outdated events first (otherwise the rate would never decay properly if events stopped)
                    long cutoffTime = curTime - _window;
                    while (_events.Count > 0 && _events.Peek() < cutoffTime)
                    {
                        _events.Dequeue();
                    }

                    return (double)_events.Count * 1000.0 / (double)_window;
                }
            }
        }
    }
}
