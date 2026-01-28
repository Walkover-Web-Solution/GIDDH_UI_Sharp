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
        // CENTRALIZED SERILOG WITH STRUCTURED JSON LOGGING
        // ===========================================
        var logFilePath = "/var/log/template-logs/giddh-template.log";

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .Enrich.WithMachineName()
            .Enrich.WithProperty("Application", "GiddhTemplateService")
            .Enrich.WithProperty("Version", serviceVersion)
            .Enrich.WithProperty("Service", serviceName)
            .Enrich.WithProperty("ServiceType", serviceType)
            .Enrich.WithProperty("Environment", environmentName)
            .Enrich.WithProperty("Company", company)
            .Enrich.WithProperty("Product", product)
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
            builder.Services.AddSingleton<RazorTemplateService>();
            builder.Services.AddSingleton<PdfService>();
            builder.Services.AddScoped<AccountStatementPdfService>();
            builder.Services.AddHostedService<MemoryReservationService>();
            builder.Services.AddHostedService<PdfCleanupService>();
            builder.Services.AddControllers();

            var app = builder.Build();

            // Pre-warm browser on startup
            var pdfService = app.Services.GetRequiredService<PdfService>();
            await pdfService.GetBrowserAsync();

            // Register browser disposal on shutdown
            var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStopping.Register(() =>
            {
                PdfService.DisposeBrowserAsync().GetAwaiter().GetResult();
            });

            // ===========================================
            // CENTRALIZED GLOBAL EXCEPTION HANDLER WITH RICH CONTEXT
            // ===========================================
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async ctx =>
                {
                    var exceptionDetails = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
                    if (exceptionDetails?.Error is Exception ex)
                    {
                        // Capture rich context for centralized logging
                        var userAgent = ctx.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";
                        var remoteIp = ctx.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                        var method = ctx.Request.Method;
                        var route = ctx.Request.Path.Value ?? "unknown";
                        var queryString = ctx.Request.QueryString.Value ?? "";
                        
                        // Get distributed tracing context
                        var activity = Activity.Current;
                        var traceId = activity?.TraceId.ToString() ?? ctx.TraceIdentifier;
                        var spanId = activity?.SpanId.ToString() ?? "N/A";

                        // Centralized structured logging with rich context
                        Log.Error(ex, 
                            "Unhandled exception | Route: {Route} | Method: {Method} | TraceId: {TraceId} | SpanId: {SpanId} | UserAgent: {UserAgent} | RemoteIP: {RemoteIP} | Query: {QueryString}",
                            route, method, traceId, spanId, userAgent, remoteIp, queryString);

                        // Add exception to OpenTelemetry trace with context
                        activity?.AddEvent(new ActivityEvent("exception", DateTimeOffset.UtcNow, new ActivityTagsCollection
                        {
                            ["exception.type"] = ex.GetType().FullName ?? "Unknown",
                            ["exception.message"] = ex.Message,
                            ["exception.stacktrace"] = ex.ToString(),
                            ["http.method"] = method,
                            ["http.route"] = route,
                            ["http.user_agent"] = userAgent,
                            ["http.remote_ip"] = remoteIp,
                            ["http.status_code"] = "500"
                        }));

                        // Centralized Slack alerting with context
                        try
                        {
                            var slackService = ctx.RequestServices.GetRequiredService<ISlackService>();
                            var errorContext = $"**Route:** {route} {method}\n**TraceId:** {traceId}\n**UserAgent:** {userAgent}\n**RemoteIP:** {remoteIp}";
                            
                            await slackService.SendErrorAlertAsync(
                                route,
                                slackEnvironment,
                                $"{ex.GetType().Name}: {ex.Message}\n\n{errorContext}",
                                ex.StackTrace ?? "No stack trace available");
                        }
                        catch (Exception slackEx)
                        {
                            Log.Warning(slackEx, "Failed to send Slack alert for {Route} {Method}", route, method);
                        }
                    }

                    ctx.Response.StatusCode = 500;
                    ctx.Response.ContentType = "application/json";

                    await ctx.Response.WriteAsJsonAsync(new
                    {
                        error = "Internal Server Error",
                        traceId = ctx.TraceIdentifier,
                        timestamp = DateTimeOffset.UtcNow
                    });
                });
            });

            // ===========================================
            // CENTRALIZED HTTP REQUEST LOGGING WITH RICH CONTEXT
            // ===========================================
            app.UseSerilogRequestLogging(options =>
            {
                options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} ({Elapsed:0.0000}ms) [{ContentLength}b] | TraceId: {TraceId}";
                options.GetLevel = (httpContext, elapsed, ex) =>
                {
                    if (ex != null || httpContext.Response.StatusCode >= 500)
                        return Serilog.Events.LogEventLevel.Error;
                    if (httpContext.Response.StatusCode >= 400)
                        return Serilog.Events.LogEventLevel.Warning;
                    if (elapsed > 5000) // Log slow requests as warnings for performance monitoring
                        return Serilog.Events.LogEventLevel.Warning;
                    return Serilog.Events.LogEventLevel.Information;
                };
                options.EnrichDiagnosticContext = (diagCtx, httpContext) =>
                {
                    // Request context
                    diagCtx.Set("RequestHost", httpContext.Request.Host.Value);
                    diagCtx.Set("RequestScheme", httpContext.Request.Scheme);
                    diagCtx.Set("QueryString", httpContext.Request.QueryString.Value);
                    
                    // Client context
                    diagCtx.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown");
                    diagCtx.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown");
                    diagCtx.Set("Referer", httpContext.Request.Headers["Referer"].FirstOrDefault());
                    
                    // Response context
                    diagCtx.Set("ContentLength", httpContext.Response.ContentLength ?? 0);
                    diagCtx.Set("ContentType", httpContext.Response.ContentType);
                    
                    // Distributed tracing context
                    var activity = Activity.Current;
                    if (activity != null)
                    {
                        diagCtx.Set("TraceId", activity.TraceId.ToString());
                        diagCtx.Set("SpanId", activity.SpanId.ToString());
                        diagCtx.Set("ParentId", activity.ParentId);
                    }
                    
                    // Performance context
                    diagCtx.Set("RequestStartTime", DateTimeOffset.UtcNow);
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
