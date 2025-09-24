using System;

namespace GiddhTemplate.Extensions
{
    /// <summary>
    /// Attribute to mark methods for automatic logging via interception
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class LogMethodAttribute : Attribute
    {
        /// <summary>
        /// Optional custom message for the log entry
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Whether to include method parameters in the log (default: false for performance)
        /// </summary>
        public bool IncludeParameters { get; set; } = false;

        /// <summary>
        /// Whether to include return value summary in the log (default: true)
        /// </summary>
        public bool IncludeReturnValue { get; set; } = true;

        public LogMethodAttribute()
        {
        }

        public LogMethodAttribute(string message)
        {
            Message = message;
        }
    }
}
