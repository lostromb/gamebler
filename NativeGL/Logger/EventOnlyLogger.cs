namespace Durandal.Common.Logger
{
    using System;

    /// <summary>
    /// A logger which doesn't output its logs anywhere, but only makes them available as events and as a logging history
    /// </summary>
    public class EventOnlyLogger : LoggerBase
    {
        private EventLoggerCore _loggerImpl;

        private string _thisComponent;
        private string _thisTraceId;

        public EventOnlyLogger(string componentName = "Main")
        {
            _loggerImpl = new EventLoggerCore();
            MaxLevels = LogLevel.All;
            ValidLevels = LogLevel.All;
            _thisComponent = componentName ?? "Main";
            _thisTraceId = null;
        }

        /// <summary>
        /// Private constructor for creating inherited logger objects
        /// </summary>
        /// <param name="componentName"></param>
        /// <param name="stream"></param>
        private EventOnlyLogger(string componentName, string traceId, EventLoggerCore core, LogLevel levels, LogLevel maxLevels)
        {
            _thisComponent = componentName;
            _loggerImpl = core;
            _thisTraceId = traceId;
            ValidLevels = levels;
            MaxLevels = maxLevels;
        }

        public override ILogger Clone(string newComponentName, string traceId)
        {
            if (newComponentName != null && !newComponentName.Equals(_thisComponent) ||
                (_thisTraceId == null && traceId != null) || (_thisTraceId != null && traceId == null) ||
                (_thisTraceId != null && traceId != null && _thisTraceId.Equals(traceId)))
            {
                return new EventOnlyLogger(newComponentName, traceId, _loggerImpl, ValidLevels, MaxLevels);
            }

            return this;
        }

        public override ILogger Clone(string newComponentName)
        {
            if (newComponentName != null && !newComponentName.Equals(_thisComponent))
            {
                return new EventOnlyLogger(newComponentName, _thisTraceId, _loggerImpl, ValidLevels, MaxLevels);
            }

            return this;
        }

        public override void SuppressAllOutput()
        {
            _loggerImpl.SuppressAllOutput();
        }

        public override void UnsuppressAllOutput()
        {
            _loggerImpl.UnsuppressAllOutput();
        }

        public override void Flush(bool blocking = false) { }

        public override LogLevel ValidLevels
        {
            get; set;
        }

        public LogLevel MaxLevels
        {
            get; set;
        }

        public override string ComponentName
        {
            get
            {
                return _thisComponent;
            }
        }

        public override string TraceId
        {
            get
            {
                return _thisTraceId;
            }
            set
            {
                _thisTraceId = value;
            }
        }

        public override void Log(object value, LogLevel level = LogLevel.Std, string traceId = null)
        {
            if (value == null)
                Log("null", level, traceId);
            else
                Log(value.ToString(), level, traceId);
        }

        public override void Log(string value, LogLevel level = LogLevel.Std, string traceId = null)
        {
            // Convert it into an event
            LogEvent newEvent = new LogEvent(_thisComponent, value ?? "null", level, GetHighResolutionTime(), traceId ?? _thisTraceId);
            Log(newEvent);
        }

        public override void Log(LogEvent value)
        {
            if ((MaxLevels & ValidLevels & value.Level) != 0)
            {
                _loggerImpl.Log(value);
            }
        }

        public override void Log(Func<string> producer, LogLevel level = LogLevel.Std, string traceId = null)
        {
            if ((MaxLevels & ValidLevels & level) != 0)
            {
                Log(producer(), level, traceId);
            }
        }

        public override LoggingHistory GetHistory()
        {
            return _loggerImpl.GetHistory();
        }

        public override event EventHandler<LogUpdatedEventArgs> LogUpdated
        {
            add { _loggerImpl.LogUpdated += value; }
            remove { _loggerImpl.LogUpdated -= value; }
        }

        /// <summary>
        /// This is the context object shared between all clones of the console logger
        /// </summary>
        private class EventLoggerCore
        {
            private LoggingHistory _history = null;
            private volatile bool _suppressed = false;
            private LogEventEmitter _eventEmitter;

            public EventLoggerCore()
            {
                _history = new LoggingHistory();
                _eventEmitter = new LogEventEmitter();
            }

            public void SuppressAllOutput()
            {
                _suppressed = true;
            }

            public void UnsuppressAllOutput()
            {
                _suppressed = false;
            }

            public LoggingHistory GetHistory()
            {
                return _history;
            }

            public void Log(LogEvent value)
            {
                if (!_suppressed)
                {
                    _history.Add(value);
                    OnLogUpdated(value);
                }
            }

            public event EventHandler<LogUpdatedEventArgs> LogUpdated
            {
                add { _eventEmitter.Add(value); }
                remove { _eventEmitter.Remove(value); }
            }

            private void OnLogUpdated(LogEvent e)
            {
                LogUpdatedEventArgs args = new LogUpdatedEventArgs(e);
                if (_eventEmitter != null)
                {
                    _eventEmitter.Fire(this, args);
                }
            }
        }
    }
}
