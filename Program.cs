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
        // SERILOG (STRUCTURED JSON LOGS)
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
            .WriteTo.Console(new JsonFormatter())  
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
                    .AddGrpcClientInstrumentation()
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

            builder.Services.AddScoped<ISlackService, SlackService>();
            builder.Services.AddScoped<PdfService>();
            builder.Services.AddControllers();

            var app = builder.Build();

            // GLOBAL EXCEPTION HANDLER (unchanged)
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async ctx =>
                {
                    var exceptionDetails = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
                    if (exceptionDetails?.Error is Exception ex)
                    {
                        Log.Error(ex, "Unhandled Exception in {Route}", ctx.Request.Path);
                        Activity.Current?.AddEvent(new ActivityEvent("exception"));
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

            app.UseSerilogRequestLogging();

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
