using System;

namespace SolutionManager.Logic.Logging
{
    public enum LogLevel
    {
        /// <summary>
        /// No logging output.
        /// </summary>
        None,
        /// <summary>
        /// Informational messages.
        /// </summary>
        Info,
        /// <summary>
        /// Warning messages.
        /// </summary>
        Warning,
        /// <summary>
        /// Error messages.
        /// </summary>
        Error,
        /// <summary>
        /// Fatal messages.
        /// </summary>
        Fatal,
        /// <summary>
        /// For development purposes. Don't pass this into the Logger.Log()
        /// function, use DebugLog() instead.
        /// </summary>
        Debug
    };
}
