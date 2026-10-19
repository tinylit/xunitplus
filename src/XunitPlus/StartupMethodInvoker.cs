#if NET8_0_OR_GREATER
using System.Reflection;
using System.Runtime.CompilerServices;

namespace XunitPlus;

internal static class StartupMethodInvoker
{
    // Weak keys avoid keeping collectible startup assemblies alive.
    private static readonly ConditionalWeakTable<MethodInfo, MethodInvoker> _invokers = [];

    public static object? Invoke(MethodInfo method, object? target, Span<object?> arguments)
    {
        // Keep reflection's validation behavior for unsupported open methods.
        if (method.ContainsGenericParameters)
        {
            return method.Invoke(target, arguments.ToArray());
        }

        var invoker = _invokers.GetValue(method, static key => MethodInvoker.Create(key));

        try
        {
            return invoker.Invoke(target, arguments);
        }
        catch (Exception exception)
        {
            // MethodInvoker propagates target exceptions directly; MethodInfo.Invoke
            // wraps them. Preserve the existing startup contract on every TFM.
            throw new TargetInvocationException(exception);
        }
    }
}
#endif
