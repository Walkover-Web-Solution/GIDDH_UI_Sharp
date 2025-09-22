using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;
using System.Diagnostics;

namespace GiddhTemplate.Extensions
{
    /// <summary>
    /// Action filter that automatically logs method entry, exit, parameters, and execution time
    /// </summary>
    public class AutoLoggingActionFilter : ActionFilterAttribute
    {
        private readonly Serilog.ILogger _logger;
        private Stopwatch? _stopwatch;

        public AutoLoggingActionFilter()
        {
            _logger = Log.Logger;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            _stopwatch = Stopwatch.StartNew();
            
            var controllerName = context.Controller.GetType().Name;
            var actionName = context.ActionDescriptor.DisplayName ?? context.ActionDescriptor.RouteValues["action"];
            var parameters = GetParametersInfo(context.ActionArguments);

            _logger.Information("→ Entering {ControllerName}.{ActionName} with parameters: {@Parameters}", 
                controllerName, actionName, parameters);

            base.OnActionExecuting(context);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            _stopwatch?.Stop();
            
            var controllerName = context.Controller.GetType().Name;
            var actionName = context.ActionDescriptor.DisplayName ?? context.ActionDescriptor.RouteValues["action"];
            var elapsedMs = _stopwatch?.ElapsedMilliseconds ?? 0;

            if (context.Exception != null)
            {
                _logger.Error(context.Exception, "✗ Exception in {ControllerName}.{ActionName} after {ElapsedMs}ms", 
                    controllerName, actionName, elapsedMs);
            }
            else
            {
                var result = GetResultInfo(context.Result);
                _logger.Information("← Exiting {ControllerName}.{ActionName} successfully in {ElapsedMs}ms with result: {@Result}", 
                    controllerName, actionName, elapsedMs, result);
            }

            base.OnActionExecuted(context);
        }

        private object GetParametersInfo(IDictionary<string, object?> actionArguments)
        {
            var parameters = new Dictionary<string, object?>();
            
            foreach (var kvp in actionArguments)
            {
                parameters[kvp.Key] = SerializeValue(kvp.Value);
            }

            return parameters;
        }

        private object GetResultInfo(object? result)
        {
            return SerializeValue(result);
        }

        private object? SerializeValue(object? value)
        {
            if (value == null) return null;
            
            var type = value.GetType();
            
            // Handle common types that are safe to log
            if (type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) || type == typeof(Guid))
            {
                return value;
            }
            
            // For complex objects, return type name and basic info to avoid circular references
            if (type.IsClass)
            {
                try
                {
                    // For simple objects, try to get basic info
                    if (type.Namespace?.StartsWith("System") == false && type.Namespace?.StartsWith("Microsoft") == false)
                    {
                        return new { Type = type.Name, Value = value.ToString() };
                    }
                }
                catch
                {
                    // If serialization fails, just return type info
                }
                
                return new { Type = type.Name };
            }
            
            return value;
        }
    }
}
