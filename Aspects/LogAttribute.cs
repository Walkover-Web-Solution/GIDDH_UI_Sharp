using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Diagnostics;

namespace GiddhTemplate.Aspects;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class LogAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        // Apply logging to all public methods in the type
        foreach (var method in builder.Target.Methods.Where(m => m.Accessibility == Accessibility.Public))
        {
            builder.Advice.Override(method, nameof(OverrideMethod));
        }
    }

    [Template]
    public dynamic? OverrideMethod()
    {
        var methodName = meta.Target.Method.ToDisplayString();
        var stopwatch = Stopwatch.StartNew();
        
        // Use Serilog.Log at runtime (not compile-time)
        Serilog.Log.Information("→ {MethodName} started", methodName);

        try
        {
            var result = meta.Proceed();
            
            stopwatch.Stop();
            Serilog.Log.Information("← {MethodName} completed in {ElapsedMs}ms", 
                methodName, stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Serilog.Log.Error(ex, "✖ Exception in {MethodName} after {ElapsedMs}ms", 
                methodName, stopwatch.ElapsedMilliseconds);
            
            // Send to Slack using your existing SlackService
            SlackHelper.NotifyException(methodName, ex);
            
            throw;
        }
    }
}

// Helper class to use your existing SlackService
public static class SlackHelper
{
    public static void NotifyException(string methodName, Exception ex)
    {
        // Fire and forget - use your existing SlackService logic
        _ = Task.Run(async () =>
        {
            try
            {
                // Use the same logic as your SlackService.SendErrorAlertAsync
                var slackWebhookUrl = Environment.GetEnvironmentVariable("SLACK_WEBHOOK_URL");
                if (string.IsNullOrEmpty(slackWebhookUrl))
                {
                    Serilog.Log.Warning("SLACK_WEBHOOK_URL not configured - skipping Slack notification");
                    return;
                }

                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
                
                // Create payload exactly like your SlackService
                var keyValuePairs = new Dictionary<string, string>
                {
                    { "url", methodName },
                    { "env", environment },
                    { "error", $"Exception in {methodName}: {ex.Message}" },
                    { "errorStackTrace", ex.StackTrace ?? "No stack trace available" }
                };

                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                
                var json = System.Text.Json.JsonSerializer.Serialize(keyValuePairs);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                Serilog.Log.Information("Sending exception to Slack: {MethodName}", methodName);
                var response = await httpClient.PostAsync(slackWebhookUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Serilog.Log.Warning("Failed to send to Slack: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
                }
                else
                {
                    Serilog.Log.Information("Successfully sent exception to Slack for {MethodName}", methodName);
                }
            }
            catch (Exception slackEx)
            {
                Serilog.Log.Warning(slackEx, "Error sending exception to Slack for {MethodName}", methodName);
            }
        });
    }
}
