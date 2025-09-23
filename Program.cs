using GiddhTemplate.Services;
using GiddhTemplate.Controllers;
using GiddhTemplate.Extensions;
using Serilog;

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
            builder.Services.AddSingleton<PdfService>();

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
                    
                    // Skip health checks completely
                    if (userAgent.Contains("ELB-HealthChecker") || 
                        userAgent.Contains("HealthCheck") ||
                        (path == "/" && httpContext.Request.Method == "GET"))
                    {
                        return null; // Don't log at all
                    }
                    
                    // Log errors and business operations
                    if (ex != null || httpContext.Response.StatusCode >= 400)
                        return Serilog.Events.LogEventLevel.Error;
                    
                    // Log business operations (API calls, POST requests)
                    if (path.StartsWith("/api/") || httpContext.Request.Method != "GET")
                        return Serilog.Events.LogEventLevel.Information;
                    
                    // Skip other simple GET requests
                    return null;
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
