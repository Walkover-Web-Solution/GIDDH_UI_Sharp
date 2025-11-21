using GiddhTemplate.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Formatting.Json;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Exporter;
using System.Diagnostics;

public class Program
{
    public static async Task Main(string[] args)
    {
        // ===========================================
        // GLOBAL FALLBACK EXCEPTION HANDLERS (EARLY)
        // ===========================================
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Log.Fatal(e.ExceptionObject as Exception, "Unhandled (domain)");

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Log.Error(e.Exception, "Unobserved task");
            e.SetObserved();
        };

        // Load configuration early
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile(
                $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json",
                optional: true)
            .AddEnvironmentVariables()
            .Build();

        var grafanaEnv = Environment.GetEnvironmentVariable("GRAFANA_APP_ENV");
        if (!string.IsNullOrEmpty(grafanaEnv))
        {
            configuration["Serilog:WriteTo:2:Args:labels:1:value"] = grafanaEnv;
        }

        var serviceVersion = Environment.GetEnvironmentVariable("APP_VERSION") ?? "1.0.0";
        var environmentName = Environment.GetEnvironmentVariable("GRAFANA_APP_ENV")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Development";
        var slackEnvironment = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? environmentName;
        var serviceName = Environment.GetEnvironmentVariable("SERVICE_NAME") ?? "giddh-template";
        var serviceType = Environment.GetEnvironmentVariable("GRAFANA_SERVICE_TYPE") ?? "api";
        var company = Environment.GetEnvironmentVariable("GRAFANA_COMPANY") ?? "Walkover";
        var product = Environment.GetEnvironmentVariable("GRAFANA_PRODUCT") ?? "GIDDH";
        var serverRegion = Environment.GetEnvironmentVariable("SERVER_REGION") ?? "IN";
        var orgId = Environment.GetEnvironmentVariable("GRAFANA_ORG_ID");

        // ===========================================
        // SERILOG (STRUCTURED JSON LOGS + FILE + CONSOLE)
        // ===========================================
        var logFilePath = "/var/log/template-logs/giddh-template.log";

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", "GiddhTemplateService")
            .Enrich.WithProperty("Version", serviceVersion)
            .Enrich.WithProperty("Service", serviceName)
            .Enrich.WithProperty("ServiceType", serviceType)
            .Enrich.WithProperty("Environment", environmentName)
            .WriteTo.Console(new JsonFormatter())
            .WriteTo.File(new JsonFormatter(), logFilePath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
            .CreateLogger();

        try
        {
            Log.Information("Starting GIDDH Template Service...");

            var builder = WebApplication.CreateBuilder(args);

            // Prevent duplicate logs
            builder.Logging.ClearProviders();
            builder.Host.UseSerilog();

            // ===========================================
            // OPENTELEMETRY SETUP
            // ===========================================
            var openTelemetry = builder.Services.AddOpenTelemetry();

            openTelemetry.ConfigureResource(resource =>
                resource.AddService(
                    serviceName: serviceName,
                    serviceVersion: serviceVersion,
                    serviceInstanceId: Environment.MachineName)
                .AddAttributes(new KeyValuePair<string, object>[]
                {
                    new("deployment.environment", environmentName),
                    new("company", company),
                    new("product", product),
                    new("service.type", serviceType),
                    new("service.namespace", product),
                    new("service.instance.id", Environment.MachineName),
                    new("server.region", serverRegion)
                }));

            openTelemetry.WithTracing(tracing =>
            {
                tracing
                    .SetSampler(new AlwaysOnSampler())
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddHttpClientInstrumentation()
                    .AddSource("GiddhTemplate.*")
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri("http://127.0.0.1:4318/v1/traces");
                        options.Protocol = OtlpExportProtocol.HttpProtobuf;

                        if (!string.IsNullOrWhiteSpace(orgId))
                        {
                            options.Headers = $"X-Scope-OrgID={orgId}";
                        }
                    });
            });

            openTelemetry.WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("GiddhTemplate.Metrics")
                    .AddPrometheusExporter();
            });

            // Dependency injection
            builder.Services.AddHttpClient();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ISlackService, SlackService>();
            builder.Services.AddScoped<PdfService>();
            builder.Services.AddControllers();

            var app = builder.Build();

            // ===========================================
            // GLOBAL EXCEPTION HANDLER WITH SLACK ALERT
            // ===========================================
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async ctx =>
                {
                    var exceptionDetails = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
                    if (exceptionDetails?.Error is Exception ex)
                    {
                        // Log locally
                        Log.Error(ex, "Unhandled exception in {Route}", ctx.Request.Path);

                        // Add exception to OpenTelemetry trace
                        Activity.Current?.AddEvent(new ActivityEvent("exception", DateTimeOffset.UtcNow, new ActivityTagsCollection
                        {
                            ["exception.type"] = ex.GetType().FullName,
                            ["exception.message"] = ex.Message,
                            ["exception.stacktrace"] = ex.ToString()
                        }));

                        // Slack alert
                        try
                        {
                            var slackService = ctx.RequestServices.GetRequiredService<ISlackService>();
                            await slackService.SendErrorAlertAsync(
                                ctx.Request.Path.Value ?? "unknown",
                                slackEnvironment,
                                ex.Message,
                                ex.StackTrace ?? "No stack trace available");
                        }
                        catch (Exception slackEx)
                        {
                            Log.Warning(slackEx, "Failed to send Slack alert for {Path}", ctx.Request.Path);
                        }
                    }

                    ctx.Response.StatusCode = 500;
                    ctx.Response.ContentType = "application/json";

                    await ctx.Response.WriteAsJsonAsync(new
                    {
                        error = "Internal Server Error",
                        traceId = ctx.TraceIdentifier
                    });
                });
            });

            // ===========================================
            // SERILOG HTTP REQUEST LOGGING
            // ===========================================
            app.UseSerilogRequestLogging(options =>
            {
                options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} ({Elapsed:0.0000}ms) [{ContentLength}b]";
                options.GetLevel = (httpContext, elapsed, ex) =>
                {
                    if (ex != null || httpContext.Response.StatusCode >= 500)
                        return Serilog.Events.LogEventLevel.Error;
                    if (httpContext.Response.StatusCode >= 400)
                        return Serilog.Events.LogEventLevel.Warning;
                    return Serilog.Events.LogEventLevel.Information;
                };
                options.EnrichDiagnosticContext = (diagCtx, httpContext) =>
                {
                    diagCtx.Set("RequestHost", httpContext.Request.Host.Value);
                    diagCtx.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault());
                    diagCtx.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress?.ToString());
                    diagCtx.Set("ContentLength", httpContext.Response.ContentLength ?? 0);
                    diagCtx.Set("Referer", httpContext.Request.Headers["Referer"].FirstOrDefault());

                    var activity = Activity.Current;
                    if (activity != null)
                    {
                        diagCtx.Set("TraceId", activity.TraceId.ToString());
                        diagCtx.Set("SpanId", activity.SpanId.ToString());
                    }
                };
            });

            app.MapPrometheusScrapingEndpoint();
            app.MapControllers();

            Log.Information("GIDDH Template Service started successfully on port 5000");
            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "GIDDH Template Service terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.Information("GIDDH Template Service is shutting down...");
            await Log.CloseAndFlushAsync();
        }
    }

    private static Uri? TryCreateUri(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : null;
    }
}
