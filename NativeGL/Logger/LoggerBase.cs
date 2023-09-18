using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Durandal.Common.Logger
{
    /// <summary>
    /// Provides common helper infrastructure for system loggers
    /// </summary>
    public abstract class LoggerBase : ILogger
    {
        public abstract string ComponentName { get; }
        public abstract string TraceId { get; set; }
        public abstract LogLevel ValidLevels { get; set; }
        public abstract event EventHandler<LogUpdatedEventArgs> LogUpdated;
        public abstract ILogger Clone(string newComponentName);
        public abstract ILogger Clone(string newComponentName, string traceId);
        public abstract void Flush(bool blocking = false);
        public abstract LoggingHistory GetHistory();
        public abstract void Log(LogEvent value);
        public abstract void Log(Func<string> producer, LogLevel level = LogLevel.Std, string traceId = null);
        public abstract void Log(string value, LogLevel level = LogLevel.Std, string traceId = null);
        public abstract void Log(object value, LogLevel level = LogLevel.Std, string traceId = null);
        public abstract void SuppressAllOutput();
        public abstract void UnsuppressAllOutput();
        public void Dispose() { }

        private long _stopwatchOffset = 0;
        private double _stopwatchMultiplier;
        private int nextSyncCounter;

        public LoggerBase()
        {
            _stopwatchMultiplier = (10000000d / (double)Stopwatch.Frequency);
            Sync();
        }

        /// <summary>
        /// Standard implementation of exception logger
        /// </summary>
        /// <param name="value"></param>
        /// <param name="level"></param>
        /// <param name="traceId"></param>
        public void Log(Exception value, LogLevel level = LogLevel.Err, string traceId = null)
        {
            Log(value.GetType().Name + ": " + value.Message, level, traceId);
            Exception inner = value.InnerException;
            int nestCount = 0;
            while (inner != null && nestCount++ < 3)
            {
                Log("Inner exception: " + inner.GetType().Name + ": " + inner.Message, level, traceId);
                inner = inner.InnerException;
            }

            if (value.StackTrace != null)
            {
                Log(value.StackTrace, level, traceId);
            }
        }

        private void Sync()
        {
            // Recalculate the stopwatch offset, and if it has varied to a large degree (25ms), adjust it
            long newOffset = DateTime.UtcNow.Ticks - (long)(Stopwatch.GetTimestamp() * _stopwatchMultiplier);
            if (Math.Abs(newOffset - _stopwatchOffset) > 250000L)
            {
                _stopwatchOffset = newOffset;
            }
            nextSyncCounter = 10000;
        }

        /// <summary>
        /// Returns the current UTC time according to the most accurate timer available to the system.
        /// </summary>
        /// <returns></returns>
        protected DateTime GetHighResolutionTime()
        {
            if (nextSyncCounter-- <= 0)
                Sync();
            return new DateTime((long)((double)Stopwatch.GetTimestamp() * _stopwatchMultiplier) + _stopwatchOffset, DateTimeKind.Utc);
        }
    }
}
