using Castle.DynamicProxy;
using Serilog;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Microsoft.AspNetCore.Http;

namespace GiddhTemplate.Extensions
{
    /// <summary>
    /// Castle.DynamicProxy interceptor for automatic method logging based on LogMethodAttribute
    /// </summary>
    public class LoggingInterceptor : IInterceptor
    {
        private static readonly Serilog.ILogger _logger = Log.Logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LoggingInterceptor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void Intercept(IInvocation invocation)
        {
            var method = invocation.Method;
            var logAttribute = method.GetCustomAttribute<LogMethodAttribute>();

            // Only intercept methods marked with LogMethodAttribute
            if (logAttribute == null)
            {
                invocation.Proceed();
                return;
            }

            var className = invocation.TargetType.Name;
            var methodName = method.Name;
            var fullMethodName = $"{className}.{methodName}";
            var customMessage = logAttribute.Message ?? methodName;

            // Get ActionId from HTTP context (ASP.NET Core's built-in request correlation)
            var httpContext = _httpContextAccessor.HttpContext;
            var actionId = httpContext?.TraceIdentifier ?? "";
            var actionIdShort = !string.IsNullOrEmpty(actionId) ? actionId[..8] : "";
            var requestIdPrefix = !string.IsNullOrEmpty(actionIdShort) ? $"[{actionIdShort}] " : "";

            // Determine log level based on method name
            var isMainMethod = methodName == "GeneratePdfAsync";
            var logLevel = isMainMethod ? Serilog.Events.LogEventLevel.Information : Serilog.Events.LogEventLevel.Debug;

            var stopwatch = Stopwatch.StartNew();
            
            _logger.Write(logLevel, "{RequestIdPrefix}→ {MethodName} started", requestIdPrefix, fullMethodName);

            try
            {
                invocation.Proceed();

                // Handle async methods
                if (method.ReturnType.IsGenericType && 
                    method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    HandleAsyncMethod(invocation, fullMethodName, stopwatch, logAttribute, requestIdPrefix);
                }
                else if (method.ReturnType == typeof(Task))
                {
                    HandleAsyncVoidMethod(invocation, fullMethodName, stopwatch, requestIdPrefix);
                }
                else
                {
                    // Synchronous method
                    stopwatch.Stop();
                    var summary = logAttribute.IncludeReturnValue ? GetLightweightSummary(invocation.ReturnValue) : "completed";
                    _logger.Write(logLevel, "{RequestIdPrefix}← {MethodName} completed in {ElapsedMs}ms → {Summary}",
                        requestIdPrefix, fullMethodName, stopwatch.ElapsedMilliseconds, summary);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, "✗ {MethodName} failed after {ElapsedMs}ms", fullMethodName, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        private void HandleAsyncMethod(IInvocation invocation, string fullMethodName, Stopwatch stopwatch, LogMethodAttribute logAttribute, string requestIdPrefix)
        {
            var task = (Task)invocation.ReturnValue;
            var taskType = task.GetType();
            var methodName = invocation.Method.Name;
            var isMainMethod = methodName == "GeneratePdfAsync";
            var logLevel = isMainMethod ? Serilog.Events.LogEventLevel.Information : Serilog.Events.LogEventLevel.Debug;

            if (taskType.IsGenericType)
            {
                // Task<T> - has return value
                var continueWith = typeof(LoggingInterceptor)
                    .GetMethod(nameof(ContinueWithResult), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(taskType.GetGenericArguments()[0]);

                invocation.ReturnValue = continueWith.Invoke(null, new object[] { task, fullMethodName, stopwatch, logAttribute.IncludeReturnValue, logLevel, requestIdPrefix });
            }
            else
            {
                // Task - no return value
                invocation.ReturnValue = ContinueWithVoid(task, fullMethodName, stopwatch, logLevel, requestIdPrefix);
            }
        }

        private void HandleAsyncVoidMethod(IInvocation invocation, string fullMethodName, Stopwatch stopwatch, string requestIdPrefix)
        {
            var task = (Task)invocation.ReturnValue;
            var methodName = invocation.Method.Name;
            var isMainMethod = methodName == "GeneratePdfAsync";
            var logLevel = isMainMethod ? Serilog.Events.LogEventLevel.Information : Serilog.Events.LogEventLevel.Debug;
            invocation.ReturnValue = ContinueWithVoid(task, fullMethodName, stopwatch, logLevel, requestIdPrefix);
        }

        private static async Task<T> ContinueWithResult<T>(Task<T> task, string fullMethodName, Stopwatch stopwatch, bool includeReturnValue, Serilog.Events.LogEventLevel logLevel, string requestIdPrefix)
        {
            try
            {
                var result = await task;
                stopwatch.Stop();
                var summary = includeReturnValue ? GetLightweightSummary(result) : "completed";
                _logger.Write(logLevel, "{RequestIdPrefix}← {MethodName} completed in {ElapsedMs}ms → {Summary}",
                    requestIdPrefix, fullMethodName, stopwatch.ElapsedMilliseconds, summary);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, "✗ {MethodName} failed after {ElapsedMs}ms", fullMethodName, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        private static async Task ContinueWithVoid(Task task, string fullMethodName, Stopwatch stopwatch, Serilog.Events.LogEventLevel logLevel, string requestIdPrefix)
        {
            try
            {
                await task;
                stopwatch.Stop();
                _logger.Write(logLevel, "{RequestIdPrefix}← {MethodName} completed in {ElapsedMs}ms",
                    requestIdPrefix, fullMethodName, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, "✗ {MethodName} failed after {ElapsedMs}ms", fullMethodName, stopwatch.ElapsedMilliseconds);
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
