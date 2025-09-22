using Serilog;

namespace GiddhTemplate.Extensions
{
    /// <summary>
    /// Extension methods for easy logging throughout the application
    /// </summary>
    public static class LoggingExtensions
    {
        /// <summary>
        /// Log information with context
        /// </summary>
        public static void LogInfo(this object source, string message, params object[] args)
        {
            Log.ForContext(source.GetType()).Information(message, args);
        }

        /// <summary>
        /// Log warning with context
        /// </summary>
        public static void LogWarning(this object source, string message, params object[] args)
        {
            Log.ForContext(source.GetType()).Warning(message, args);
        }

        /// <summary>
        /// Log error with context
        /// </summary>
        public static void LogError(this object source, string message, params object[] args)
        {
            Log.ForContext(source.GetType()).Error(message, args);
        }

        /// <summary>
        /// Log error with exception and context
        /// </summary>
        public static void LogError(this object source, Exception ex, string message, params object[] args)
        {
            Log.ForContext(source.GetType()).Error(ex, message, args);
        }

        /// <summary>
        /// Log debug information with context
        /// </summary>
        public static void LogDebug(this object source, string message, params object[] args)
        {
            Log.ForContext(source.GetType()).Debug(message, args);
        }

        /// <summary>
        /// Log with custom properties
        /// </summary>
        public static void LogWithProperties(this object source, string level, string message, object properties)
        {
            var logger = Log.ForContext(source.GetType());
            
            switch (level.ToLower())
            {
                case "info":
                case "information":
                    logger.Information("{Message} {@Properties}", message, properties);
                    break;
                case "warn":
                case "warning":
                    logger.Warning("{Message} {@Properties}", message, properties);
                    break;
                case "error":
                    logger.Error("{Message} {@Properties}", message, properties);
                    break;
                case "debug":
                    logger.Debug("{Message} {@Properties}", message, properties);
                    break;
                default:
                    logger.Information("{Message} {@Properties}", message, properties);
                    break;
            }
        }
    }
}
