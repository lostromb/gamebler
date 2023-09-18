namespace Durandal.Common.Logger
{
    using System;
    using System.Diagnostics;

    public class DebugLogger : LoggerBase
    {
        public const LogLevel DEFAULT_LOG_LEVELS = LogLevel.Std | LogLevel.Err | LogLevel.Wrn | LogLevel.Ins;

        private DebugLoggerCore _loggerImpl;

        private string _thisComponent;
        private string _thisTraceId;

        public DebugLogger(string componentName = "Main", LogLevel logLevels = DEFAULT_LOG_LEVELS, bool keepHistory = false)
        {
            _loggerImpl = new DebugLoggerCore(logLevels, keepHistory);
            _thisComponent = componentName ?? "Main";
            _thisTraceId = null;
            ValidLevels = logLevels;
            Log("Console logger initialized");
        }

        /// <summary>
        /// Private constructor for creating inherited logger objects
        /// </summary>
        /// <param name="componentName"></param>
        /// <param name="stream"></param>
        private DebugLogger(string componentName, string traceId, DebugLoggerCore core, LogLevel levels)
        {
            _thisComponent = componentName;
            _loggerImpl = core;
            _thisTraceId = traceId;
            ValidLevels = levels;
        }

        public override ILogger Clone(string newComponentName, string traceId)
        {
            if (string.Equals(_thisTraceId, traceId) &&
                string.Equals(_thisComponent, newComponentName))
            {
                return this;
            }

            return new DebugLogger(newComponentName, traceId, _loggerImpl, ValidLevels);
        }

        public override ILogger Clone(string newComponentName)
        {
            if (string.Equals(_thisComponent, newComponentName))
            {
                return this;
            }

            return new DebugLogger(newComponentName, _thisTraceId, _loggerImpl, ValidLevels);
        }

        public override void SuppressAllOutput()
        {
            _loggerImpl.SuppressAllOutput();
        }

        public override void UnsuppressAllOutput()
        {
            _loggerImpl.UnsuppressAllOutput();
        }

        public override LogLevel ValidLevels
        {
            get; set;
        }

        public override void Flush(bool blocking = false) { }

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
            if ((ValidLevels & value.Level) != 0)
            {
                _loggerImpl.Log(value);
            }
        }

        public override void Log(Func<string> producer, LogLevel level = LogLevel.Std, string traceId = null)
        {
            if ((ValidLevels & level) != 0)
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
        private class DebugLoggerCore
        {
            private LogLevel _levelsToLog = DEFAULT_LOG_LEVELS;
            private LoggingHistory _history = null;
            private volatile bool _suppressed = false;
            private LogEventEmitter _eventEmitter;
            
            public DebugLoggerCore(LogLevel levels, bool keepHistory)
            {
                _levelsToLog = levels;
                if (keepHistory)
                {
                    _history = new LoggingHistory();
                }
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
                    Debug.WriteLine(value.ToShortStringHighPrecisionTime());
                    if (_history != null)
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
