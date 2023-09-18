namespace Durandal.Common.Logger
{
    using System;

    /// <summary>
    /// Represents a generic logging system
    /// </summary>
    public interface ILogger : IDisposable
    {
        /// <summary>
        /// Creates a clone of this logger with a new componentname & possibly a new traceID,
        /// but which is directed to the same underlying stream. Each program component will then
        /// only have to manage its own local clone of the logger.
        /// </summary>
        /// <param name="newComponentName">The new component ID to use for the clone (such as the classname that uses it)</param>
        /// <param name="traceId">The trace ID to set for the new instance. May be NULL to clear tracing</param>
        /// <returns>A new logger instance</returns>
        ILogger Clone(string newComponentName, string traceId);

        /// <summary>
        /// Creates a clone of this logger with a new component name but preserving the existing traceid.
        /// </summary>
        /// <param name="newComponentName">The new component ID to use for the clone (such as the classname that uses it)</param>
        /// <returns>A new logger instance</returns>
        ILogger Clone(string newComponentName);

        /// <summary>
        /// Logs an object
        /// </summary>
        /// <param name="value">The object to log</param>
        /// <param name="level">The importance level</param>
        void Log(object value, LogLevel level = LogLevel.Std, string traceId = null);

        /// <summary>
        /// Logs a basic string
        /// </summary>
        /// <param name="value">The string to log</param>
        /// <param name="level">The importance level</param>
        void Log(string value, LogLevel level = LogLevel.Std, string traceId = null);

        /// <summary>
        /// Logs an exception
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="level"></param>
        /// <param name="traceId"></param>
        void Log(Exception exception, LogLevel level = LogLevel.Err, string traceId = null);

        /// <summary>
        /// Logs the result of a conditional function (useful for logging large messages without incurring much overhead)
        /// </summary>
        /// <param name="producer">A function that produces a string</param>
        /// <param name="level"></param>
        /// <param name="traceId"></param>
        void Log(Func<string> producer, LogLevel level = LogLevel.Std, string traceId = null);

        /// <summary>
        /// Logs a low-level event
        /// </summary>
        /// <param name="value"></param>
        void Log(LogEvent value);

        /// <summary>
        /// An event which is fired whenever a new message is written to the log
        /// </summary>
        event EventHandler<LogUpdatedEventArgs> LogUpdated;

        /// <summary>
        /// Returns a history structure which allows access to the stream of log messages and events
        /// </summary>
        /// <returns></returns>
        LoggingHistory GetHistory();

        /// <summary>
        /// Returns the component name which owns this instance of the logger
        /// </summary>
        /// <returns></returns>
        string ComponentName { get; }

        /// <summary>
        /// Gets or sets the current TraceId
        /// </summary>
        /// <returns></returns>
        string TraceId { get; set; }

        /// <summary>
        /// Mutes all output from this logger.
        /// </summary>
        void SuppressAllOutput();

        /// <summary>
        /// Unmutes all output from this logger
        /// </summary>
        void UnsuppressAllOutput();

        LogLevel ValidLevels { get; set; }

        /// <summary>
        /// Attempts to flush all output from this logger
        /// </summary>
        void Flush(bool blocking = false);
    }
}