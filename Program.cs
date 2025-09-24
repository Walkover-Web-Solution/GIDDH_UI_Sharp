using GiddhTemplate.Services;
using GiddhTemplate.Controllers;
using GiddhTemplate.Extensions;
using Serilog;
using Castle.DynamicProxy;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Configure Serilog early to capture startup logs
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Override Grafana environment label from GRAFANA_APP_ENV if set
        var grafanaEnv = Environment.GetEnvironmentVariable("GRAFANA_APP_ENV");
        if (!string.IsNullOrEmpty(grafanaEnv))
        {
            configuration["Serilog:WriteTo:2:Args:labels:1:value"] = grafanaEnv;
        }

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        try
        {
            Log.Information("Starting GIDDH Template Service...");
            
            var builder = WebApplication.CreateBuilder(args);

            // Replace default logging with Serilog
            builder.Host.UseSerilog();

            // Register services
            builder.Services.AddHttpClient();
            builder.Services.AddScoped<ISlackService, SlackService>();
            
            // Register HTTP context accessor for ActionId extraction
            builder.Services.AddHttpContextAccessor();
            
            // Register Castle.DynamicProxy components for automatic logging
            builder.Services.AddSingleton<ProxyGenerator>();
            builder.Services.AddScoped<LoggingInterceptor>(); // Changed to Scoped to access HttpContext
            
            // Register PdfService with proxy generation for automatic logging
            builder.Services.AddScoped<PdfService>(serviceProvider =>
            {
                var proxyGenerator = serviceProvider.GetRequiredService<ProxyGenerator>();
                var loggingInterceptor = serviceProvider.GetRequiredService<LoggingInterceptor>();
                
                // Create the actual PdfService instance
                var pdfService = new PdfService();
                
                // Create a proxy that intercepts method calls for automatic logging
                var proxy = proxyGenerator.CreateClassProxyWithTarget(pdfService, loggingInterceptor);
                
                // Set the proxy reference for internal method calls to go through the proxy
                proxy.SetProxyReference(proxy);
                
                return proxy;
            });

            builder.Services.AddControllers(options =>
            {
                // Add automatic logging for all controller actions
                options.Filters.Add<AutoLoggingActionFilter>();
            });

            var app = builder.Build();

            // Add Serilog request logging middleware for automatic HTTP logging
            app.UseSerilogRequestLogging(options =>
            {
                options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} ({Elapsed:0.0000}ms)";
                
                // Filter out health check requests and only log meaningful operations
                options.GetLevel = (httpContext, elapsed, ex) =>
                {
                    var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "";
                    var path = httpContext.Request.Path.Value ?? "";
                    
                    // Skip health checks by using Debug level (which is filtered out by config)
                    if (userAgent.Contains("ELB-HealthChecker") || 
                        userAgent.Contains("HealthCheck") ||
                        (path == "/" && httpContext.Request.Method == "GET"))
                    {
                        return Serilog.Events.LogEventLevel.Debug; // Debug level gets filtered out
                    }
                    
                    // Log errors and business operations
                    if (ex != null || httpContext.Response.StatusCode >= 400)
                        return Serilog.Events.LogEventLevel.Error;
                    
                    // Log business operations (API calls, POST requests)
                    if (path.StartsWith("/api/") || httpContext.Request.Method != "GET")
                        return Serilog.Events.LogEventLevel.Information;
                    
                    // Skip other simple GET requests by using Debug level
                    return Serilog.Events.LogEventLevel.Debug;
                };
                
                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                    diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault());
                    
                    // Add business context for meaningful requests
                    if (httpContext.Request.Path.Value?.StartsWith("/api/") == true)
                    {
                        diagnosticContext.Set("BusinessOperation", true);
                    }
                };
            });

            app.MapControllers();

            Log.Information("GIDDH Template Service started successfully on port 5000");
            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "GIDDH Template Service terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
