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
                options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
                options.GetLevel = (httpContext, elapsed, ex) => ex != null
                    ? Serilog.Events.LogEventLevel.Error
                    : httpContext.Response.StatusCode > 499
                        ? Serilog.Events.LogEventLevel.Error
                        : Serilog.Events.LogEventLevel.Information;
                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                    diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                    diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault());
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
