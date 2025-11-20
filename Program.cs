using GiddhTemplate.Services;
using GiddhTemplate.Controllers;
using GiddhTemplate.Extensions;
using Serilog;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Load configuration early
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Override Grafana environment label if set
        var grafanaEnv = Environment.GetEnvironmentVariable("GRAFANA_APP_ENV");
        if (!string.IsNullOrEmpty(grafanaEnv))
        {
            configuration["Serilog:WriteTo:2:Args:labels:1:value"] = grafanaEnv;
        }

        // Setup Serilog BEFORE the host starts
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        try
        {
            Log.Information("Starting GIDDH Template Service...");

            var builder = WebApplication.CreateBuilder(args);

            // Replace built-in logging with Serilog
            builder.Host.UseSerilog();

            // Dependency injection
            builder.Services.AddHttpClient();
            builder.Services.AddHttpContextAccessor();

            // Your services
            builder.Services.AddScoped<ISlackService, SlackService>();
            builder.Services.AddScoped<PdfService>();  // Metalama will inject logging, no proxy needed

            // Add MVC controllers + automatic action logging
            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<AutoLoggingActionFilter>();
            });

            var app = builder.Build();

            // Serilog middleware for HTTP request logs
            app.UseSerilogRequestLogging(options =>
            {
                options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} ({Elapsed:0.0000}ms)";

                options.GetLevel = (httpContext, elapsed, ex) =>
                {
                    var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "";
                    var path = httpContext.Request.Path.Value ?? "";

                    // Skip useless logs
                    if (userAgent.Contains("ELB-HealthChecker") ||
                        userAgent.Contains("HealthCheck") ||
                        (path == "/" && httpContext.Request.Method == "GET"))
                    {
                        return Serilog.Events.LogEventLevel.Debug;
                    }

                    if (ex != null || httpContext.Response.StatusCode >= 400)
                        return Serilog.Events.LogEventLevel.Error;

                    if (path.StartsWith("/api/") || httpContext.Request.Method != "GET")
                        return Serilog.Events.LogEventLevel.Information;

                    return Serilog.Events.LogEventLevel.Debug;
                };

                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                    diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault());

                    if (httpContext.Request.Path.Value?.StartsWith("/api/") == true)
                    {
                        diagnosticContext.Set("BusinessOperation", true);
                    }
                };
            });

            // Map controllers
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
