namespace Durandal.Common.Logger
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A class which specifies a set of filters to be applied to a set of log events
    /// </summary>
    public class FilterCriteria
    {
        public string ExactComponentName;
        public LogLevel Level;
        public string SearchTerm;
        public ISet<string> AllowedComponentNames; 
        public DateTime StartTime;
        public DateTime EndTime;
        public string TraceId;

        public bool PassesFilter(LogEvent e)
        {
            if (this.ExactComponentName != null && !e.Component.Equals(this.ExactComponentName))
            {
                return false;
            }
            if (this.Level != default(LogLevel) && !this.Level.HasFlag(e.Level))
            {
                return false;
            }
            if (this.StartTime != default(DateTime) && e.Timestamp < this.StartTime)
            {
                return false;
            }
            if (this.EndTime != default(DateTime) && e.Timestamp > this.EndTime)
            {
                return false;
            }
            if (this.AllowedComponentNames != null && !this.AllowedComponentNames.Contains(e.Component))
            {
                return false;
            }
            if (!string.IsNullOrEmpty(this.SearchTerm) && !e.Message.Contains(this.SearchTerm) && !e.Component.Contains(this.SearchTerm))
            {
                return false;
            }
            if (!string.IsNullOrEmpty(this.TraceId) && (string.IsNullOrEmpty(e.TraceId) || !e.TraceId.Equals(this.TraceId)))
            {
                 return false;
            }
            return true;
        }

        public static FilterCriteria ByTraceId(string traceId)
        {
            return new FilterCriteria()
            {
                TraceId = traceId
            };
        }
    }
}
