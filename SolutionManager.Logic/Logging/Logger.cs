using System;
using System.Diagnostics;
using log4net;
using log4net.Core;
using log4net.Appender;
using log4net.Layout;
using log4net.Config;

namespace SolutionManager.Logic.Logging
{
    public static class Logger
    {
        /// <summary>
        /// This callback delegate can be used by applications who want to receive log
        /// messages from this library.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        /// <param name="level">The severity of the log entry from <seealso cref="LogLevel"/>.</param>
        public delegate void LogCallback(object message, LogLevel level);

        /// <summary>
        /// Triggered whenever a message is logged. If this is left
        /// null, log messages will go to the console.
        /// </summary>
        public static event LogCallback OnLogMessage;

        private static ILog LogInstance { get; set; }

        /// <summary>
        /// The minimum log level which should be outputted on the console. If
        /// no logger configuration for log4net exists, nothing will be written to
        /// the console output for release builds.
        /// </summary>
        public static LogLevel LOG_LEVEL = LogLevel.Debug;

        static Logger()
        {
            LogInstance = LogManager.GetLogger("SolutionManager");
           
            // Set up a ConsoleAppender if no reporting has been configured for Error messages.
            if (!LogInstance.Logger.IsEnabledFor(Level.Error))
            {
                ConsoleAppender appender = new ConsoleAppender
                {
                    Layout = new PatternLayout("[%date{yyyy-MM-dd HH:mm:ss.fff}] - [%level] - %message%newline")
                };
                BasicConfigurator.Configure(appender);

                if (LOG_LEVEL != LogLevel.None)
                    LogInstance.Info("No log configuration found, defaulting to console logging");
            }
        }

        /// <summary>
        /// Sends a log message to the Log4net logging engine.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        /// <param name="level">The severity of the log entry.</param>
        public static void Log(object message, LogLevel level)
        {
            Log(message, level, null);
        }

        /// <summary>
        /// Sends a log message to the Log4net logging engine.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        /// <param name="level">The severity of the log entry.</param>
        /// <param name="exception">The exception that was raised.</param>
        public static void Log(object message, LogLevel level, Exception exception)
        {
            OnLogMessage?.Invoke(message, level);

            switch (level)
            {
                case LogLevel.Debug:
                    if (LOG_LEVEL == LogLevel.Debug)
                        LogInstance.Debug(message, exception);
                    break;
                case LogLevel.Info:
                    if (LOG_LEVEL == LogLevel.Debug
                        || LOG_LEVEL == LogLevel.Info)
                        LogInstance.Info(message, exception);
                    break;
                case LogLevel.Warning:
                    if (LOG_LEVEL == LogLevel.Debug
                        || LOG_LEVEL == LogLevel.Info
                        || LOG_LEVEL == LogLevel.Warning)
                        LogInstance.Warn(message, exception);
                    break;
                case LogLevel.Error:
                    if (LOG_LEVEL == LogLevel.Debug
                        || LOG_LEVEL == LogLevel.Info
                        || LOG_LEVEL == LogLevel.Warning
                        || LOG_LEVEL == LogLevel.Error)
                        LogInstance.Error(message, exception);
                    break;
                case LogLevel.Fatal:
                    if (LOG_LEVEL == LogLevel.Debug
                        || LOG_LEVEL == LogLevel.Info
                        || LOG_LEVEL == LogLevel.Warning
                        || LOG_LEVEL == LogLevel.Error
                        || LOG_LEVEL == LogLevel.Fatal)
                        LogInstance.Fatal(message, exception);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// If the library is compiled with DEBUG defined, an event will be
        /// fired if an <code>OnLogMessage</code> handler is registered and the
        /// message will be sent to the logging engine
        /// </summary>
        /// <param name="message">The message to log at the DEBUG level to the
        /// current logging engine</param>
        [Conditional("DEBUG")]
        public static void DebugLog(object message)
        {
            if (LOG_LEVEL == LogLevel.Debug)
            {
                OnLogMessage?.Invoke(message, LogLevel.Debug);

                LogInstance.Debug(message);
            }
        }
    }
}
