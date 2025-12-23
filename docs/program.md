# Program.cs Documentation

## Overview
Program.cs (`@/Users/divyanshu/walkover/GiddhTemplate/Program.cs`) is the application entry point that configures services, middleware, logging, monitoring, and error handling for the Giddh Template Service.

## Application Architecture

### Main Method Structure (`@/Users/divyanshu/walkover/GiddhTemplate/Program.cs:13-280`)

**Execution Flow:**
1. **Global Exception Handlers Setup**
2. **Configuration Loading**
3. **Structured Logging Configuration**
4. **Web Application Builder Setup**
5. **OpenTelemetry Configuration**
6. **Service Registration**
7. **Middleware Pipeline Configuration**
8. **Application Startup**

## Configuration Management

### Early Configuration Loading (`@/Users/divyanshu/walkover/GiddhTemplate/Program.cs:27-34`)
```csharp
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile(
        $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json",
        optional: true)
    .AddEnvironmentVariables()
    .Build();
```

**Configuration Sources (Priority Order):**
1. Environment Variables (highest)
2. Environment-specific JSON files
3. Base appsettings.json (lowest)

### Environment Variable Processing (`@/Users/divyanshu/walkover/GiddhTemplate/Program.cs:36-52`)
```csharp
var serviceVersion = Environment.GetEnvironmentVariable("APP_VERSION") ?? "1.0.0";
var environmentName = Environment.GetEnvironmentVariable("GRAFANA_APP_ENV")
    ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    ?? "Development";
var serviceName = Environment.GetEnvironmentVariable("SERVICE_NAME") ?? "giddh-template";
var company = Environment.GetEnvironmentVariable("GRAFANA_COMPANY") ?? "Walkover";
var product = Environment.GetEnvironmentVariable("GRAFANA_PRODUCT") ?? "GIDDH";
```

**Key Environment Variables:**
- `APP_VERSION` - Application version for telemetry
- `GRAFANA_APP_ENV` - Environment label for monitoring
- `SERVICE_NAME` - Service identifier
- `GRAFANA_COMPANY` - Company name for telemetry
- `GRAFANA_PRODUCT` - Product name for telemetry
- `SERVER_REGION` - Geographic region identifier
- `GRAFANA_ORG_ID` - Organization ID for telemetry

## Logging Configuration

### Serilog Setup (`@/Users/divyanshu/walkover/GiddhTemplate/Program.cs:59-75`)
```csharp
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
```

**Enrichment Properties:**
- **Context:** Request correlation and tracing
- **Environment:** Machine name, process ID, thread ID
- **Application:** Service metadata and versioning
- **Business:** Company and product information

**Output Destinations:**
- **Console:** JSON formatted for container logs
- **File:** `/var/log/template-logs/giddh-template.log` with 30-day retention

## OpenTelemetry Configuration

### Resource Configuration (`@/Users/divyanshu/walkover/GiddhTemplate/Program.cs:92-106`)
```csharp
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
```

### Distributed Tracing (`@/Users/divyanshu/walkover/GiddhTemplate/Program.cs:108-128`)
```csharp
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
```

**Tracing Features:**
- **100% Sampling:** All requests traced
- **Exception Recording:** Automatic exception capture
- **HTTP Instrumentation:** Outbound HTTP calls traced
- **OTLP Export:** OpenTelemetry Protocol export
- **Organization Headers:** Multi-tenant support

### Metrics Collection (`@/Users/divyanshu/walkover/GiddhTemplate/Program.cs:130-137`)
```csharp
openTelemetry.WithMetrics(metrics =>
{
    metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("GiddhTemplate.Metrics")
        .AddPrometheusExporter();
});
```

**Metrics Sources:**
- **ASP.NET Core:** HTTP request metrics
- **Runtime:** .NET runtime performance
- **Custom:** Application-specific metrics
- **Prometheus:** Metrics export endpoint

## Service Registration

### Dependency Injection (`@/Users/divyanshu/walkover/GiddhTemplate/Program.cs:139-144`)
```csharp
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ISlackService, SlackService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddControllers();
```

**Service Lifetimes:**
- **Scoped:** Per-request instances for business services
- **Singleton:** HTTP client factory (built-in)
- **Transient:** HTTP context accessor (built-in)

## Global Exception Handling

### Early Exception Handlers (`@/Users/divyanshu/walkover/GiddhTemplate/Program.cs:18-25`)
```csharp
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
    Log.Fatal(e.ExceptionObject as Exception, "Unhandled (domain)");

TaskScheduler.UnobservedTaskException += (_, e) =>
{
    Log.Error(e.Exception, "Unobserved task");
    e.SetObserved();
};
```

### Centralized Exception Handler (`@/Users/divyanshu/walkover/GiddhTemplate/Program.cs:151-216`)
```csharp
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async ctx =>
    {
        var exceptionDetails = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        if (exceptionDetails?.Error is Exception ex)
        {
            // Rich context capture
            var userAgent = ctx.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";
            var remoteIp = ctx.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var method = ctx.Request.Method;
            var route = ctx.Request.Path.Value ?? "unknown";
            var queryString = ctx.Request.QueryString.Value ?? "";
            
            // Distributed tracing context
            var activity = Activity.Current;
            var traceId = activity?.TraceId.ToString() ?? ctx.TraceIdentifier;
            var spanId = activity?.SpanId.ToString() ?? "N/A";
        }
    });
});
```

**Exception Context Capture:**
- **HTTP Context:** Method, route, query parameters
- **Client Information:** User agent, IP address
- **Tracing:** Trace ID, span ID correlation
- **Timing:** Request timestamp and duration

**Error Response:**
```csharp
await ctx.Response.WriteAsJsonAsync(new
{
    error = "Internal Server Error",
    traceId = ctx.TraceIdentifier,
    timestamp = DateTimeOffset.UtcNow
});
```

### Slack Integration (`@/Users/divyanshu/walkover/GiddhTemplate/Program.cs:189-203`)
```csharp
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
```

## Request Logging

### Structured HTTP Logging (`@/Users/divyanshu/walkover/GiddhTemplate/Program.cs:221-262`)
```csharp
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} ({Elapsed:0.0000}ms) [{ContentLength}b] | TraceId: {TraceId}";
    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        if (ex != null || httpContext.Response.StatusCode >= 500)
            return Serilog.Events.LogEventLevel.Error;
        if (httpContext.Response.StatusCode >= 400)
            return Serilog.Events.LogEventLevel.Warning;
        if (elapsed > 5000) // Slow requests
            return Serilog.Events.LogEventLevel.Warning;
        return Serilog.Events.LogEventLevel.Information;
    };
});
```

**Log Level Strategy:**
- **Error:** Exceptions or 5xx status codes
- **Warning:** 4xx status codes or slow requests (>5s)
- **Information:** Successful requests

### Request Enrichment (`@/Users/divyanshu/walkover/GiddhTemplate/Program.cs:234-261`)
```csharp
options.EnrichDiagnosticContext = (diagCtx, httpContext) =>
{
    // Request context
    diagCtx.Set("RequestHost", httpContext.Request.Host.Value);
    diagCtx.Set("RequestScheme", httpContext.Request.Scheme);
    diagCtx.Set("QueryString", httpContext.Request.QueryString.Value);
    
    // Client context
    diagCtx.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown");
    diagCtx.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown");
    
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
};
```

## Application Startup

### Endpoint Configuration (`@/Users/divyanshu/walkover/GiddhTemplate/Program.cs:264-265`)
```csharp
app.MapPrometheusScrapingEndpoint();
app.MapControllers();
```

**Endpoints:**
- `/metrics` - Prometheus metrics scraping
- API Controllers - Business logic endpoints

### Graceful Shutdown (`@/Users/divyanshu/walkover/GiddhTemplate/Program.cs:270-279`)
```csharp
try
{
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
```

**Shutdown Process:**
1. **Graceful Stop:** Await pending requests
2. **Log Flush:** Ensure all logs are written
3. **Resource Cleanup:** Dispose of services

## Performance Optimizations

### Async Operations
- **Non-blocking I/O:** All file and network operations
- **Concurrent Processing:** Multiple requests handled simultaneously
- **Resource Pooling:** HTTP client factory usage

### Memory Management
- **Scoped Services:** Per-request lifecycle
- **Logging Buffers:** Efficient log batching
- **Configuration Caching:** Single configuration load

### Monitoring Integration
- **Health Checks:** Built-in ASP.NET Core health endpoints
- **Metrics Collection:** Real-time performance data
- **Distributed Tracing:** Request flow visibility

## Security Considerations

### Error Information Disclosure
- **Generic Client Responses:** No sensitive information exposed
- **Detailed Server Logs:** Full context for debugging
- **Trace Correlation:** Secure trace ID generation

### Input Validation
- **Model Binding:** Automatic request validation
- **Content Type Validation:** JSON content enforcement
- **Request Size Limits:** Built-in ASP.NET Core limits

## Development vs Production

### Environment-Specific Behavior
```csharp
var environmentName = Environment.GetEnvironmentVariable("GRAFANA_APP_ENV")
    ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    ?? "Development";
```

**Development Features:**
- Detailed exception pages
- Swagger UI enabled
- Console logging prominent

**Production Features:**
- Structured JSON logging
- External monitoring integration
- Error alerting via Slack

## Troubleshooting

### Common Startup Issues

1. **Configuration Missing:**
   - Verify environment variables
   - Check appsettings.json structure
   - Validate file permissions

2. **Logging Failures:**
   - Ensure log directory exists (`/var/log/template-logs/`)
   - Check write permissions
   - Verify Grafana Loki connectivity

3. **OpenTelemetry Issues:**
   - Validate OTLP endpoint accessibility
   - Check organization ID configuration
   - Verify network connectivity

### Debug Techniques

1. **Startup Logging:**
   ```csharp
   Log.Information("Starting GIDDH Template Service...");
   ```

2. **Configuration Validation:**
   - Log loaded configuration values
   - Verify environment variable resolution

3. **Service Registration:**
   - Validate dependency injection setup
   - Check service lifetime configurations

---

**Author/Developer:** Divyanshu Shrivastava  
**Last Updated:** December 2025
