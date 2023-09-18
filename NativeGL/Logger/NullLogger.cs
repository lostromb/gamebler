namespace Durandal.Common.Logger
{
    using System;

    /// <summary>
    /// A black hole logger implementation
    /// </summary>
    public class NullLogger : ILogger
    {
        public ILogger Clone(string newComponentName, string traceId) { return this; }
        public ILogger Clone(string newComponentName) { return this; }
        public void SuppressAllOutput() { }
        public void UnsuppressAllOutput() { }
        public void Log(object value, LogLevel level = LogLevel.Std, string traceId = null) { }
        public void Log(string value, LogLevel level = LogLevel.Std, string traceId = null) { }
        public void Log(Exception value, LogLevel level = LogLevel.Err, string traceId = null) { }

        public void Log(Func<string> producer, LogLevel level = LogLevel.Std, string traceId = null) { }
        public void Log(LogEvent e)
        {
            if (LogUpdated != null)
                LogUpdated(this, new LogUpdatedEventArgs(e));
        }

        public event EventHandler<LogUpdatedEventArgs> LogUpdated;
        public LoggingHistory GetHistory() { return null; }
        public void Dispose() { }
        public string ComponentName
        {
            get { return null; }
        }

        public string TraceId
        {
            get { return null; }
            set { }
        }

        public void Flush(bool blocking) { }

        public LogLevel ValidLevels
        {
            get
            {
                return LogLevel.None;
            }

            set { }
        }
    }
}
