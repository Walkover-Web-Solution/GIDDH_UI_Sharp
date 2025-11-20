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
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Override Grafana env label if needed
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
        var tempoEndpoint = TryCreateUri(Environment.GetEnvironmentVariable("GRAFANA_TEMPO_URL"));

        // ===========================================
        // ENHANCED SERILOG SETUP WITH ENRICHERS
        // ===========================================
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
            .WriteTo.Console(new JsonFormatter())  // Structured JSON for Grafana Agent/Promtail
            .CreateLogger();

        try
        {
            Log.Information("Starting GIDDH Template Service...");

            var builder = WebApplication.CreateBuilder(args);

            // Replace built-in logging with Serilog
            builder.Host.UseSerilog();

            var resourceBuilder = ResourceBuilder.CreateDefault()
                .AddService(serviceName: serviceName, serviceVersion: serviceVersion, serviceInstanceId: Environment.MachineName)
                .AddAttributes(new KeyValuePair<string, object>[]
                {
                    new("deployment.environment", environmentName),
                    new("company", company),
                    new("product", product),
                    new("service.type", serviceType),
                    new("service.namespace", product),
                    new("service.instance.id", Environment.MachineName),
                    new("server.region", serverRegion)
                });

            // ===========================================
            // OPENTELEMETRY SETUP (TRACES & METRICS)
            // ===========================================
            var openTelemetry = builder.Services.AddOpenTelemetry();

            openTelemetry.ConfigureResource(rb => rb.AddResource(resourceBuilder.Build()));

            openTelemetry.WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnrichWithHttpRequest = (activity, request) =>
                        {
                            activity.SetTag("http.request.body.size", request.ContentLength ?? 0);
                        };
                        options.EnrichWithHttpResponse = (activity, response) =>
                        {
                            activity.SetTag("http.response.body.size", response.ContentLength ?? 0);
                        };
                    })
                    .AddHttpClientInstrumentation()
                    .AddSource("GiddhTemplate.*");

                if (tempoEndpoint is not null)
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = tempoEndpoint;
                        options.Protocol = OtlpExportProtocol.Grpc;
                        if (!string.IsNullOrWhiteSpace(orgId))
                        {
                            options.Headers = $"X-Scope-OrgID={orgId}";
                        }
                    });
                }
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

            // Services
            builder.Services.AddScoped<ISlackService, SlackService>();
            builder.Services.AddScoped<PdfService>();

            // Controllers (no action filter needed - using global exception handler)
            builder.Services.AddControllers();

            var app = builder.Build();

            // ===========================================
            // ENHANCED GLOBAL EXCEPTION HANDLER
            // ===========================================
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async ctx =>
                {
                    var exceptionDetails = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();

                    if (exceptionDetails?.Error is Exception ex)
                    {
                        // Enrich with correlation data
                        using (Serilog.Context.LogContext.PushProperty("route", ctx.Request.Path.Value))
                        using (Serilog.Context.LogContext.PushProperty("method", ctx.Request.Method))
                        using (Serilog.Context.LogContext.PushProperty("userAgent", ctx.Request.Headers["User-Agent"].FirstOrDefault()))
                        using (Serilog.Context.LogContext.PushProperty("remoteIp", ctx.Connection.RemoteIpAddress?.ToString()))
                        using (Serilog.Context.LogContext.PushProperty("traceId", Activity.Current?.TraceId.ToString()))
                        using (Serilog.Context.LogContext.PushProperty("spanId", Activity.Current?.SpanId.ToString()))
                        using (Serilog.Context.LogContext.PushProperty("userId", ctx.User?.Identity?.Name)) // if authenticated
                        using (Serilog.Context.LogContext.PushProperty("tenant", ctx.Request.Headers["X-Tenant-Id"].FirstOrDefault())) // if multi-tenant
                        using (Serilog.Context.LogContext.PushProperty("exceptionType", ex.GetType().Name))
                        using (Serilog.Context.LogContext.PushProperty("statusCode", 500))
                        {
                            Log.Error(ex, "Unhandled exception in {Route} {Method}", ctx.Request.Path, ctx.Request.Method);
                        }

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
                        
                        // Add exception event to current OpenTelemetry span if available
                        Activity.Current?.AddEvent(new ActivityEvent("exception", DateTimeOffset.UtcNow, new ActivityTagsCollection
                        {
                            ["exception.type"] = ex.GetType().FullName,
                            ["exception.message"] = ex.Message,
                            ["exception.stacktrace"] = ex.ToString()
                        }));
                    }

                    ctx.Response.StatusCode = 500;
                    ctx.Response.ContentType = "application/json";
                    
                    await ctx.Response.WriteAsJsonAsync(new
                    {
                        error = "Internal Server Error",
                        traceId = ctx.TraceIdentifier,
                        timestamp = DateTimeOffset.UtcNow.ToString("O")
                    });
                });
            });

            // ===========================================
            // ENHANCED SERILOG HTTP REQUEST LOGGING
            // ===========================================
            app.UseSerilogRequestLogging(options =>
            {
                options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} ({Elapsed:0.0000}ms) [{ContentLength}b]";

                options.GetLevel = (httpContext, elapsed, ex) =>
                {
                    var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "";
                    var path = httpContext.Request.Path.Value ?? "";

                    // Skip noise from health checkers
                    if (userAgent.Contains("ELB-HealthChecker") ||
                        userAgent.Contains("HealthCheck") ||
                        userAgent.Contains("kube-probe") ||
                        (path == "/health" && httpContext.Request.Method == "GET"))
                    {
                        return Serilog.Events.LogEventLevel.Debug;
                    }

                    if (ex != null || httpContext.Response.StatusCode >= 500)
                        return Serilog.Events.LogEventLevel.Error;
                    
                    if (httpContext.Response.StatusCode >= 400)
                        return Serilog.Events.LogEventLevel.Warning;

                    if (path.StartsWith("/api/") || httpContext.Request.Method != "GET")
                        return Serilog.Events.LogEventLevel.Information;

                    return Serilog.Events.LogEventLevel.Debug;
                };

                options.EnrichDiagnosticContext = (diagCtx, httpContext) =>
                {
                    diagCtx.Set("RequestHost", httpContext.Request.Host.Value);
                    diagCtx.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault());
                    diagCtx.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress?.ToString());
                    diagCtx.Set("ContentLength", httpContext.Response.ContentLength ?? 0);
                    diagCtx.Set("Referer", httpContext.Request.Headers["Referer"].FirstOrDefault());
                    
                    // OpenTelemetry correlation
                    var activity = Activity.Current;
                    if (activity != null)
                    {
                        diagCtx.Set("TraceId", activity.TraceId.ToString());
                        diagCtx.Set("SpanId", activity.SpanId.ToString());
                    }
                    
                    // Multi-tenant support
                    var tenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
                    if (!string.IsNullOrEmpty(tenantId))
                        diagCtx.Set("TenantId", tenantId);
                    
                    // User context
                    if (httpContext.User?.Identity?.IsAuthenticated == true)
                    {
                        diagCtx.Set("UserId", httpContext.User.Identity.Name);
                        diagCtx.Set("UserRoles", string.Join(",", httpContext.User.Claims
                            .Where(c => c.Type == "role")
                            .Select(c => c.Value)));
                    }

                    // Business operation flag
                    if (httpContext.Request.Path.Value?.StartsWith("/api/") == true)
                        diagCtx.Set("BusinessOperation", true);
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
            throw; // Re-throw to ensure proper exit code
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