using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Durandal.Common.Logger
{
    /// <summary>
    /// Wraps a regular ILogger implementation, passing along many of its functions straight through,
    /// but turning set() and clone() operations into no-ops.
    /// </summary>
    public class ImmutableLogger : ILogger
    {
        private ILogger _impl;

        public ImmutableLogger(ILogger impl)
        {
            if (impl == null)
            {
                throw new ArgumentNullException("impl");
            }

            _impl = impl;
            _impl.LogUpdated += LogUpdated;
        }

        public string ComponentName
        {
            get
            {
                return _impl.ComponentName;
            }
        }

        public string TraceId
        {
            get
            {
                return _impl.TraceId;
            }

            set { }
        }

        public LogLevel ValidLevels
        {
            get
            {
                return _impl.ValidLevels;
            }

            set { }
        }

        public event EventHandler<LogUpdatedEventArgs> LogUpdated;

        public ILogger Clone(string newComponentName)
        {
            return this;
        }

        public ILogger Clone(string newComponentName, string traceId)
        {
            return this;
        }

        public void Dispose()
        {
            _impl.Dispose();
        }

        public void Flush(bool blocking = false)
        {
            _impl.Flush(blocking);
        }

        public LoggingHistory GetHistory()
        {
            return _impl.GetHistory();
        }

        public void Log(LogEvent value)
        {
            _impl.Log(value);
        }

        public void Log(Func<string> producer, LogLevel level = LogLevel.Std, string traceId = null)
        {
            _impl.Log(producer, level, _impl.TraceId);
        }

        public void Log(string value, LogLevel level = LogLevel.Std, string traceId = null)
        {
            _impl.Log(value, level, _impl.TraceId);
        }

        public void Log(Exception value, LogLevel level = LogLevel.Err, string traceId = null)
        {
            _impl.Log(value, level, _impl.TraceId);
        }

        public void Log(object value, LogLevel level = LogLevel.Std, string traceId = null)
        {
            _impl.Log(value, level, _impl.TraceId);
        }

        public void SuppressAllOutput()
        {
            _impl.SuppressAllOutput();
        }

        public void UnsuppressAllOutput()
        {
            _impl.UnsuppressAllOutput();
        }
    }
}
