namespace Durandal.Common.Logger
{
    using System;

    [Flags]
    public enum LogLevel
    {
        /// <summary>
        /// None (only makes sense for filters and uninitialized values)
        /// </summary>
        None = 0x00,
        
        /// <summary>
        /// Standard
        /// </summary>
        Std = 0x01,

        /// <summary>
        /// Warning
        /// </summary>
        Wrn = 0x02,

        /// <summary>
        /// Error
        /// </summary>
        Err = 0x04,

        /// <summary>
        /// Verbose
        /// </summary>
        Vrb = 0x08,

        /// <summary>
        /// Instrumentation
        /// </summary>
        Ins = 0x10,

        /// <summary>
        /// All (only makes sense for filters and initializers)
        /// </summary>
        All = 0x1F
    }

    public static class LoggingLevelManipulators
    {
        public static string ToChar(this LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Std:
                    return "S";
                case LogLevel.Wrn:
                    return "W";
                case LogLevel.Err:
                    return "E";
                case LogLevel.Vrb:
                    return "V";
                case LogLevel.Ins:
                    return "I";
            }
            return "N";
        }

        public static LogLevel ParseLevelChar(string input)
        {
            switch (input)
            {
                case "S":
                    return LogLevel.Std;
                case "W":
                    return LogLevel.Wrn;
                case "E":
                    return LogLevel.Err;
                case "V":
                    return LogLevel.Vrb;
                case "I":
                    return LogLevel.Ins;
            }

            return LogLevel.None;
        }
    }
}
