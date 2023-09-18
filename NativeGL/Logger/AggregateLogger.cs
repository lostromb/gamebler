namespace Durandal.Common.Logger
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// This logger abstracts the behavior of multiple loggers into one object.
    /// </summary>
    public class AggregateLogger : LoggerBase
    {
        private IList<ILogger> _loggers;
        private string _componentName;
        private string _traceId;
        private LogEventEmitter _eventEmitter;
        private LogLevel _validLevels;

        public AggregateLogger(string componentName, params ILogger[] loggers)
        {
            _loggers = new List<ILogger>(loggers);
            _componentName = componentName;
            _traceId = null;
            _eventEmitter = new LogEventEmitter();
            _validLevels = LogLevel.None;
            foreach (ILogger subLogger in _loggers)
            {
                _validLevels = _validLevels | subLogger.ValidLevels;
            }
        }

        /// <summary>
        /// Private constructor for creating inherited loggers
        /// </summary>
        /// <param name="componentName"></param>
        /// <param name="console"></param>
        /// <param name="file"></param>
        private AggregateLogger(string componentName, string traceId, IList<ILogger> loggers, LogEventEmitter emitter, LogLevel levels)
        {
            _loggers = new List<ILogger>();
            foreach (ILogger l in loggers)
            {
                _loggers.Add(l.Clone(componentName, traceId));
            }

            _componentName = componentName;
            _traceId = traceId;
            _eventEmitter = emitter;
            _validLevels = levels;
        }

        public override ILogger Clone(string newComponentName, string traceId)
        {
            return new AggregateLogger(newComponentName, traceId, _loggers, _eventEmitter, _validLevels);
        }

        public override ILogger Clone(string newComponentName)
        {
            return new AggregateLogger(newComponentName, _traceId, _loggers, _eventEmitter, _validLevels);
        }
        
        public override void SuppressAllOutput()
        {
            foreach (ILogger l in _loggers)
            {
                l.SuppressAllOutput();
            }
        }

        public override void UnsuppressAllOutput()
        {
            foreach (ILogger l in _loggers)
            {
                l.UnsuppressAllOutput();
            }
        }

        public override void Flush(bool blocking = false)
        {
            foreach (ILogger l in _loggers)
            {
                l.Flush(blocking);
            }
        }

        public new void Dispose()
        {
            foreach (ILogger l in _loggers)
            {
                l.Dispose();
            }
        }

        public override LogLevel ValidLevels
        {
            get
            {
                return _validLevels;
            }

            set
            {
                _validLevels = value;
                foreach (ILogger l in _loggers)
                {
                    l.ValidLevels = value;
                }
            }
        }

        public override string ComponentName
        {
            get
            {
                return _componentName;
            }
        }

        public override string TraceId
        {
            get
            {
                return _traceId;
            }
            set
            {
                _traceId = value;
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
            LogEvent newEvent = new LogEvent(_componentName, value, level, GetHighResolutionTime(), traceId ?? _traceId);
            Log(newEvent);
        }

        public override void Log(LogEvent e)
        {
            if ((_validLevels | e.Level) != 0)
            {
                foreach (ILogger l in _loggers)
                {
                    l.Log(e);
                }

                OnLogUpdated(e);
            }
        }

        public override void Log(Func<string> producer, LogLevel level = LogLevel.Std, string traceId = null)
        {
            if ((ValidLevels & level) != 0)
            {
                Log(producer(), level, traceId);
            }
        }

        public override event EventHandler<LogUpdatedEventArgs> LogUpdated
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

        public override LoggingHistory GetHistory()
        {
            LoggingHistory returnVal = null;
            foreach (ILogger l in _loggers)
            {
                returnVal = l.GetHistory();
                if (returnVal != null)
                    return returnVal;
            }

            return returnVal;
        }
    }
}
