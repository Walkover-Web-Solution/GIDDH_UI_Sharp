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
            // Skip logging for health check endpoints
            if (IsHealthCheckRequest(context))
            {
                return;
            }

            _stopwatch = Stopwatch.StartNew();
            
            var controllerName = GetCleanControllerName(context);
            var actionName = GetCleanActionName(context);
            var parameters = GetParametersInfo(context.ActionArguments);

            // Only log if there are meaningful parameters or it's a business operation
            if (IsBusinessOperation(context) || parameters.ToString() != "{}")
            {
                _logger.Information("→ {ControllerName}.{ActionName} started {@Parameters}",
                    controllerName, actionName, parameters);
            }

            base.OnActionExecuting(context);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            // Skip logging for health check endpoints
            if (IsHealthCheckRequest(context))
            {
                return;
            }

            _stopwatch?.Stop();
            
            var controllerName = GetCleanControllerName(context);
            var actionName = GetCleanActionName(context);
            var elapsedMs = _stopwatch?.ElapsedMilliseconds ?? 0;

            if (context.Exception != null)
            {
                _logger.Error(context.Exception, "✗ {ControllerName}.{ActionName} failed after {ElapsedMs}ms",
                    controllerName, actionName, elapsedMs);
            }
            else if (IsBusinessOperation(context) || elapsedMs > 100) // Only log if it's business operation or took significant time
            {
                var result = GetMeaningfulResultInfo(context.Result);
                _logger.Information("← {ControllerName}.{ActionName} completed in {ElapsedMs}ms {Result}",
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
            if (result == null)
                return new { Type = "null", Value = "null" };

            var type = result.GetType();
            var serializedValue = SerializeValue(result);

            // For ActionResult types, provide more detailed information
            if (type.Name.Contains("ActionResult") || type.Name.Contains("Result"))
            {
                return new 
                { 
                    Type = type.Name,
                    FullType = type.FullName ?? "Unknown",
                    Value = serializedValue ?? "null",
                    IsActionResult = true
                };
            }

            // For other types, provide type and value information
            return new 
            { 
                Type = type.Name,
                FullType = type.FullName ?? "Unknown",
                Value = serializedValue ?? "null"
            };
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
            
            // Handle JsonElement specifically for better parameter logging
            if (type.Name == "JsonElement")
            {
                try
                {
                    var jsonElement = (System.Text.Json.JsonElement)value;
                    return ExtractJsonElementValue(jsonElement);
                }
                catch
                {
                    return new { Type = "JsonElement", Value = "Could not parse" };
                }
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

        private object ExtractJsonElementValue(System.Text.Json.JsonElement element)
        {
            switch (element.ValueKind)
            {
                case System.Text.Json.JsonValueKind.Object:
                    var obj = new Dictionary<string, object?>();
                    foreach (var property in element.EnumerateObject())
                    {
                        obj[property.Name] = ExtractJsonElementValue(property.Value);
                    }
                    return obj;
                
                case System.Text.Json.JsonValueKind.Array:
                    var array = new List<object?>();
                    foreach (var item in element.EnumerateArray())
                    {
                        array.Add(ExtractJsonElementValue(item));
                    }
                    return array;
                
                case System.Text.Json.JsonValueKind.String:
                    return element.GetString() ?? "";
                
                case System.Text.Json.JsonValueKind.Number:
                    if (element.TryGetInt32(out int intValue))
                        return intValue;
                    if (element.TryGetDouble(out double doubleValue))
                        return doubleValue;
                    return element.GetRawText();
                
                case System.Text.Json.JsonValueKind.True:
                    return true;
                
                case System.Text.Json.JsonValueKind.False:
                    return false;
                
                case System.Text.Json.JsonValueKind.Null:
                    return null;
                
                default:
                    return element.GetRawText();
            }
        }

        private bool IsHealthCheckRequest(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;
            var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "";
            var path = httpContext.Request.Path.Value ?? "";
            
            // Skip ELB health checks and simple GET requests to root
            return userAgent.Contains("ELB-HealthChecker") || 
                   userAgent.Contains("HealthCheck") ||
                   (path == "/" && httpContext.Request.Method == "GET");
        }

        private bool IsHealthCheckRequest(ActionExecutedContext context)
        {
            var httpContext = context.HttpContext;
            var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "";
            var path = httpContext.Request.Path.Value ?? "";
            
            // Skip ELB health checks and simple GET requests to root
            return userAgent.Contains("ELB-HealthChecker") || 
                   userAgent.Contains("HealthCheck") ||
                   (path == "/" && httpContext.Request.Method == "GET");
        }

        private bool IsBusinessOperation(ActionExecutingContext context)
        {
            var controllerName = context.Controller.GetType().Name;
            var path = context.HttpContext.Request.Path.Value ?? "";
            
            // Consider PDF generation and API operations as business operations
            return controllerName.Contains("Pdf") || 
                   path.StartsWith("/api/") ||
                   context.HttpContext.Request.Method != "GET";
        }

        private bool IsBusinessOperation(ActionExecutedContext context)
        {
            var controllerName = context.Controller.GetType().Name;
            var path = context.HttpContext.Request.Path.Value ?? "";
            
            // Consider PDF generation and API operations as business operations
            return controllerName.Contains("Pdf") || 
                   path.StartsWith("/api/") ||
                   context.HttpContext.Request.Method != "GET";
        }

        private string GetCleanControllerName(ActionExecutingContext context)
        {
            return context.Controller.GetType().Name.Replace("Controller", "");
        }

        private string GetCleanControllerName(ActionExecutedContext context)
        {
            return context.Controller.GetType().Name.Replace("Controller", "");
        }

        private string GetCleanActionName(ActionExecutingContext context)
        {
            return context.ActionDescriptor.RouteValues["action"] ?? 
                   context.ActionDescriptor.DisplayName?.Split('.').LastOrDefault() ?? "Unknown";
        }

        private string GetCleanActionName(ActionExecutedContext context)
        {
            return context.ActionDescriptor.RouteValues["action"] ?? 
                   context.ActionDescriptor.DisplayName?.Split('.').LastOrDefault() ?? "Unknown";
        }

        private string GetMeaningfulResultInfo(object? result)
        {
            if (result == null) return "";
            
            var type = result.GetType().Name;
            
            // Provide meaningful result information
            return type switch
            {
                "FileContentResult" => "→ PDF generated",
                "OkObjectResult" => "→ Success",
                "BadRequestObjectResult" => "→ Bad request",
                "NotFoundResult" => "→ Not found",
                "StatusCodeResult" => "→ Status code response",
                _ => $"→ {type}"
            };
        }
    }
}
