using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace XunitPlus.Tests;

public class StartupInvocationTests
{
#if NET8_0_OR_GREATER
    [Fact]
    public void ConfigureAvoidsTemporaryArgumentArrays()
    {
        var provider = new NonAllocatingProvider();
        var startup = new Callbacks();
        var method = typeof(Callbacks).GetMethod(nameof(Callbacks.ConfigureOne))!;
        for (var i = 0; i < 512; i++)
        {
            StartupLoader.Configure(provider, startup, method);
        }

        const int iterations = 256;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < iterations; i++)
        {
            StartupLoader.Configure(provider, startup, method);
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // GetParameters still returns a small array; LINQ iterators and the
        // separate object[] for invocation must not be allocated on this path.
        Assert.True(allocated < iterations * 64, $"Allocated {allocated / iterations} bytes per call.");
    }

    private sealed class NonAllocatingProvider : IServiceProvider, IServiceScopeFactory, IServiceScope
    {
        public IServiceProvider ServiceProvider => this;
        public object? GetService(Type serviceType) =>
            serviceType == typeof(IServiceProvider) || serviceType == typeof(IServiceScopeFactory) ? this : null;
        public IServiceScope CreateScope() => this;
        public void Dispose() { }
    }
#endif

    [Theory]
    [InlineData(nameof(Callbacks.ConfigureEmpty))]
    [InlineData(nameof(Callbacks.ConfigureOne))]
    [InlineData(nameof(Callbacks.ConfigureFour))]
    [InlineData(nameof(Callbacks.ConfigureNine))]
    public void ConfigureResolvesAllArguments(string methodName)
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        var startup = new Callbacks();

        StartupLoader.Configure(provider, startup, typeof(Callbacks).GetMethod(methodName));

        Assert.Equal(1, startup.Calls);
    }

    [Fact]
    public void ConfigurePreservesTargetInvocationException()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        var startup = new Callbacks();

        var exception = Assert.Throws<TargetInvocationException>(() =>
            StartupLoader.Configure(provider, startup, typeof(Callbacks).GetMethod(nameof(Callbacks.ConfigureThrowing))));

        Assert.Same(startup.Failure, exception.InnerException);
    }

    [Theory]
    [InlineData(nameof(Callbacks.ServicesOnly))]
    [InlineData(nameof(Callbacks.ServicesFirst))]
    [InlineData(nameof(Callbacks.ContextFirst))]
    [InlineData(nameof(Callbacks.StaticServices))]
    public void ConfigureServicesSupportsAllSignatures(string methodName)
    {
        var startup = new Callbacks();
        var builder = new HostBuilder();

        StartupLoader.ConfigureServices(builder, startup, typeof(Callbacks), typeof(Callbacks).GetMethod(methodName));
        using var host = builder.Build();

        Assert.Equal("registered", host.Services.GetRequiredService<string>());
    }

    [Fact]
    public void ConfigureServicesPreservesTargetInvocationException()
    {
        var startup = new Callbacks();
        var builder = new HostBuilder();
        StartupLoader.ConfigureServices(builder, startup, typeof(Callbacks),
            typeof(Callbacks).GetMethod(nameof(Callbacks.ServicesThrowing)));

        var exception = Assert.Throws<TargetInvocationException>(() => builder.Build());

        Assert.Same(startup.Failure, exception.InnerException);
    }

    [Fact]
    public void HostCallbacksPreserveArgumentsAndResults()
    {
        var startup = new Callbacks();
        var assemblyName = typeof(Callbacks).Assembly.GetName();
        var builder = StartupLoader.CreateHostBuilder(assemblyName, startup, typeof(Callbacks),
            typeof(Callbacks).GetMethod(nameof(Callbacks.CreateHostBuilder)));
        Assert.Same(assemblyName, startup.AssemblyName);
        Assert.NotNull(builder);

        StartupLoader.ConfigureHost(builder!, startup, typeof(Callbacks),
            typeof(Callbacks).GetMethod(nameof(Callbacks.ConfigureHost)));
        using var host = StartupLoader.BuildHost(builder!, startup, typeof(Callbacks),
            typeof(Callbacks).GetMethod(nameof(Callbacks.BuildHost)));

        Assert.NotNull(host);
        Assert.Equal("registered", host!.Services.GetRequiredService<string>());
    }

    [Fact]
    public void ParameterlessStaticFactoryPreservesItsResult()
    {
        var builder = StartupLoader.CreateHostBuilder(null, null, typeof(Callbacks),
            typeof(Callbacks).GetMethod(nameof(Callbacks.StaticFactory)));

        Assert.IsType<HostBuilder>(builder);
    }

    [Fact]
    public void OpenGenericConfigurePreservesReflectionValidation()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        var method = typeof(Callbacks).GetMethod(nameof(Callbacks.GenericConfigure))!;

        Assert.Throws<InvalidOperationException>(() => StartupLoader.Configure(provider, new Callbacks(), method));
    }

    [Fact]
    public void TargetInvocationExceptionThrownByStartupIsWrappedAgain()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        var method = typeof(Callbacks).GetMethod(nameof(Callbacks.ConfigureNestedException))!;
        var startup = new Callbacks();

        var exception = Assert.Throws<TargetInvocationException>(() => StartupLoader.Configure(provider, startup, method));

        Assert.Same(startup.Failure, Assert.IsType<TargetInvocationException>(exception.InnerException).InnerException);
    }

    public class Callbacks
    {
        public int Calls { get; private set; }
        public Exception Failure { get; } = new InvalidOperationException("startup failure");
        public AssemblyName? AssemblyName { get; private set; }

        public void ConfigureEmpty() => Calls++;
        public void ConfigureOne(IServiceProvider provider) { Assert.NotNull(provider); Calls++; }
        public void ConfigureFour(IServiceProvider a, IServiceProvider b, IServiceProvider c, IServiceProvider d)
        {
            Assert.Same(a, b); Assert.Same(a, c); Assert.Same(a, d); Calls++;
        }
        public void ConfigureNine(IServiceProvider a, IServiceProvider b, IServiceProvider c, IServiceProvider d,
            IServiceProvider e, IServiceProvider f, IServiceProvider g, IServiceProvider h, IServiceProvider i)
        {
            Assert.All(new[] { b, c, d, e, f, g, h, i }, item => Assert.Same(a, item));
            Calls++;
        }
        public void ConfigureThrowing(IServiceProvider provider) => throw Failure;
        public void ConfigureNestedException() => throw new TargetInvocationException(Failure);
        public void GenericConfigure<T>() { }
        public static IHostBuilder StaticFactory() => new HostBuilder();
        public void ServicesOnly(IServiceCollection services) => services.AddSingleton("registered");
        public void ServicesFirst(IServiceCollection services, HostBuilderContext context)
        {
            Assert.NotNull(context); ServicesOnly(services);
        }
        public void ContextFirst(HostBuilderContext context, IServiceCollection services) => ServicesFirst(services, context);
        public static void StaticServices(IServiceCollection services) => services.AddSingleton("registered");
        public void ServicesThrowing(IServiceCollection services) => throw Failure;
        public IHostBuilder CreateHostBuilder(AssemblyName assemblyName)
        {
            AssemblyName = assemblyName;
            return new HostBuilder();
        }
        public void ConfigureHost(IHostBuilder builder) => builder.ConfigureServices(ServicesOnly);
        public IHost BuildHost(IHostBuilder builder) => builder.Build();
    }
}
