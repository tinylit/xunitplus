using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using XunitPlus;

// Run in Release without a debugger. Compare both implementations on the same
// runtime; the baseline reproduces the original StartupLoader.Configure path.
var configure = typeof(Startup).Assembly.GetType("XunitPlus.StartupLoader")!
    .GetMethod("Configure")!
    .CreateDelegate<Action<IServiceProvider, object?, MethodInfo?>>();
var provider = new NonAllocatingProvider();
var startup = new Callbacks();
Console.WriteLine(RuntimeInformation.FrameworkDescription);
Console.WriteLine("Path,Arguments,ns/call,bytes/call");
foreach (var name in new[] { nameof(Callbacks.Zero), nameof(Callbacks.One), nameof(Callbacks.Four), nameof(Callbacks.Nine) })
{
    var method = typeof(Callbacks).GetMethod(name)!;
    Action baseline = () => LegacyConfigure(provider, startup, method);
    Action optimized = () => configure(provider, startup, method);
    // Warm both reflection caches and tiered JIT before collecting samples.
    for (var i = 0; i < 20_000; i++) { baseline(); optimized(); }
    Measure("Legacy", method.GetParameters().Length, baseline);
    Measure("Optimized", method.GetParameters().Length, optimized);
}
GC.KeepAlive(startup.Calls);

static void LegacyConfigure(IServiceProvider provider, object startup, MethodInfo method)
{
    using var scope = provider.CreateScope();
    method.Invoke(startup, method.GetParameters()
        .Select(p => p.ParameterType)
        .Select(scope.ServiceProvider.GetRequiredService)
        .ToArray());
}

static void Measure(string name, int arguments, Action action)
{
    const int iterations = 100_000;
    var times = new double[7];
    var allocations = new double[7];
    for (var sample = 0; sample < times.Length; sample++)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var before = GC.GetAllocatedBytesForCurrentThread();
        var start = Stopwatch.GetTimestamp();
        for (var i = 0; i < iterations; i++) { action(); }
        times[sample] = Stopwatch.GetElapsedTime(start).TotalNanoseconds / iterations;
        allocations[sample] = (double)(GC.GetAllocatedBytesForCurrentThread() - before) / iterations;
    }
    Array.Sort(times);
    Array.Sort(allocations);
    Console.WriteLine($"{name},{arguments},{times[3]:F1},{allocations[3]:F0}");
}

public sealed class Callbacks
{
    public int Calls;
    public void Zero() => Calls++;
    public void One(IServiceProvider a) => Calls++;
    public void Four(IServiceProvider a, IServiceProvider b, IServiceProvider c, IServiceProvider d) => Calls++;
    public void Nine(IServiceProvider a, IServiceProvider b, IServiceProvider c, IServiceProvider d,
        IServiceProvider e, IServiceProvider f, IServiceProvider g, IServiceProvider h, IServiceProvider i) => Calls++;
}

internal sealed class NonAllocatingProvider : IServiceProvider, IServiceScopeFactory, IServiceScope
{
    public IServiceProvider ServiceProvider => this;
    public object? GetService(Type serviceType) =>
        serviceType == typeof(IServiceProvider) || serviceType == typeof(IServiceScopeFactory) ? this : null;
    public IServiceScope CreateScope() => this;
    public void Dispose() { }
}
