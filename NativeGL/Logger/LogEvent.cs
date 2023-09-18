namespace Durandal.Common.Logger
{
    using System;

    public class LogEvent : IComparable<LogEvent>
    {
        public DateTime Timestamp;
        public string Message;
        public string Component;
        public LogLevel Level;
        public string TraceId;

        public LogEvent(string component, string message, LogLevel level, DateTime utcTimestamp, string traceId = null)
        {
            this.Message = message;
            this.Component = component;
            this.Level = level;
            this.Timestamp = utcTimestamp;
            this.TraceId = traceId;
        }
        
        //public LogEvent(string component, string message, LogLevel level, string traceId = null)
        //{
        //    this.Message = message;
        //    this.Component = component;
        //    this.Level = level;
        //    this.Timestamp = DateTime.Now;
        //    this.TraceId = traceId;
        //}

        public string ToDetailedString()
        {
            return string.Format("[{0}] [{1}] [{2}:{3}]  {4}",
                Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffff"),
                string.IsNullOrEmpty(TraceId) ? "0" : TraceId,
                Level.ToChar(),
                Component,
                Message);
        }

        public string ToShortStringLocalTime()
        {
            if (!string.IsNullOrEmpty(this.TraceId) && this.TraceId.Length >= 3)
            {
                return string.Format("[{0}] [{1}] [{2}:{3}] {4}",
                this.TraceId.Substring(0, 3),
                Timestamp.ToLocalTime().ToString("HH:mm:ss"),
                Level.ToString(),
                Component,
                Message);
            }
            else
            {
                return string.Format("[{0}] [{1}:{2}] {3}",
                Timestamp.ToLocalTime().ToString("HH:mm:ss"),
                Level.ToString(),
                Component,
                Message);
            }
        }

        public string ToShortStringHighPrecisionTime()
        {
            if (!string.IsNullOrEmpty(this.TraceId) && this.TraceId.Length >= 3)
            {
                return string.Format("[{0}] [{1}] [{2}:{3}] {4}",
                this.TraceId.Substring(0, 3),
                Timestamp.ToLocalTime().ToString("HH:mm:ss.fffff"),
                Level.ToString(),
                Component,
                Message);
            }
            else
            {
                return string.Format("[{0}] [{1}:{2}] {3}",
                Timestamp.ToLocalTime().ToString("HH:mm:ss.fffff"),
                Level.ToString(),
                Component,
                Message);
            }
        }

        public override string ToString()
        {
            return ToShortStringLocalTime();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            LogEvent other = (LogEvent)obj;

            return other.Timestamp == this.Timestamp &&
                other.Level == this.Level &&
                (this.Message == null ? other.Message == null : this.Message.Equals(other.Message)) &&
                (this.Component == null ? other.Component == null : this.Component.Equals(other.Component)) &&
                (this.TraceId == null ? other.TraceId == null : this.TraceId.Equals(other.TraceId));
        }
        
        public override int GetHashCode()
        {
            return Timestamp.GetHashCode() +
                (Message != null ? Message.GetHashCode() : 0) +
                Level.GetHashCode() +
                (Component != null ? Component.GetHashCode() : 0) +
                (TraceId != null ? TraceId.GetHashCode() : 0);
        }

        public int CompareTo(LogEvent o)
        {
            if (o == null)
            {
                return -1;
            }
            
            // Sort ascending by default
            return Timestamp.CompareTo(o.Timestamp);
        }
    }
}
