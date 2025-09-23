using System.Diagnostics;
using Serilog;

namespace GiddhTemplate.Extensions
{
    /// <summary>
    /// High-performance automatic method logging without serialization overhead
    /// </summary>
    public static class MethodLogger
    {
        private static readonly Serilog.ILogger _logger = Log.Logger;

        /// <summary>
        /// Executes a method with automatic lightweight logging
        /// </summary>
        public static T ExecuteWithLogging<T>(Func<T> method, [System.Runtime.CompilerServices.CallerMemberName] string methodName = "")
        {
            var stopwatch = Stopwatch.StartNew();
            
            _logger.Debug("→ {MethodName} started", methodName);

            try
            {
                var result = method();
                stopwatch.Stop();

                // Lightweight summary without heavy serialization
                var summary = GetLightweightSummary(result);
                _logger.Debug("← {MethodName} completed in {ElapsedMs}ms → {Summary}",
                    methodName, stopwatch.ElapsedMilliseconds, summary);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, "✗ {MethodName} failed after {ElapsedMs}ms", methodName, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// Executes an async method with automatic lightweight logging
        /// </summary>
        public static async Task<T> ExecuteWithLoggingAsync<T>(Func<Task<T>> method, [System.Runtime.CompilerServices.CallerMemberName] string methodName = "")
        {
            var stopwatch = Stopwatch.StartNew();
            
            _logger.Debug("→ {MethodName} started", methodName);

            try
            {
                var result = await method();
                stopwatch.Stop();

                // Lightweight summary without heavy serialization
                var summary = GetLightweightSummary(result);
                _logger.Debug("← {MethodName} completed in {ElapsedMs}ms → {Summary}",
                    methodName, stopwatch.ElapsedMilliseconds, summary);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, "✗ {MethodName} failed after {ElapsedMs}ms", methodName, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// Creates lightweight summary without expensive serialization (performance optimized)
        /// </summary>
        private static string GetLightweightSummary<T>(T value)
        {
            if (value == null) return "null";

            var type = value.GetType();

            // Handle byte arrays (PDF data) - NO serialization
            if (type == typeof(byte[]))
            {
                var bytes = (byte[])(object)value;
                return $"PDF {Math.Round(bytes.Length / 1024.0, 1)}KB";
            }

            // Handle strings - NO content serialization
            if (type == typeof(string))
            {
                var str = (string)(object)value;
                return $"Content {str.Length} chars";
            }

            // Handle tuples (like LoadStyles return)
            if (type.Name.StartsWith("ValueTuple"))
            {
                return "Styles loaded";
            }

            // Handle browser objects
            if (type.Name.Contains("Browser"))
            {
                return "Browser ready";
            }

            // Handle complex objects - NO deep inspection
            if (type.IsClass && !type.IsPrimitive)
            {
                return $"{type.Name} processed";
            }

            // Simple types only
            return value.ToString() ?? "processed";
        }
    }
}
