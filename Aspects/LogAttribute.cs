using Metalama.Framework.Aspects;
using System;

namespace GiddhTemplate.Aspects;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class LogAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var method = meta.Target.Method;

        try
        {
            Log.Information("→ {MethodName} started with args: {Arguments}",
                method.ToDisplayString(),
                meta.Target.Parameters);

            var result = meta.Proceed();

            Log.Information("← {MethodName} completed", method.ToDisplayString());
            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "✖ Exception in {MethodName}", method.ToDisplayString());
            throw;
        }
    }
}
