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
            throw;
        }
    }
}
