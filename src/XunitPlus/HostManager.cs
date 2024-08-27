using System.Reflection;
using Microsoft.Extensions.Hosting;

namespace XunitPlus;

internal sealed class HostManager(AssemblyName assemblyName, IMessageSink diagnosticMessageSink)
    : IHostedService, IDisposable
{
    private readonly List<IHost> _hosts = [];
    private readonly Dictionary<Type, DependencyInjectionContext> _hostMap = [];

    private static Type FindStartup(Type testClassType, out bool shared)
    {
        var attr = testClassType.GetCustomAttribute<StartupAttribute>();

        if (attr != null)
        {
            shared = attr.Shared;

            return attr.StartupType;
        }

        shared = false;

        var declaringType = testClassType;

        do
        {
            var startupType = declaringType.GetNestedType("Startup", BindingFlags.Static | BindingFlags.Instance | BindingFlags.Default | BindingFlags.Public | BindingFlags.NonPublic);

            if (startupType is null)
            {
                declaringType = declaringType.DeclaringType;

                continue;
            }

            return startupType;

        } while (declaringType != null);

        var ns = testClassType.Namespace;

        while (true)
        {
            var startupTypeString = "Startup";

            bool flag = ns?.Length > 0;

            if (flag)
            {
                startupTypeString = ns + ".Startup";
            }

            var startupType = testClassType.Assembly.GetType(startupTypeString);

            if (startupType != null)
            {
                return startupType;
            }

            if (flag)
            {
                var index = ns!.LastIndexOf('.');

                if (index > 0)
                {
                    ns = ns[..index];

                    continue;
                }
            }

            break;
        }

        return typeof(Startup);
    }

    public DependencyInjectionContext CreateHost(Type serviceType)
    {
        var startupType = FindStartup(serviceType, out var shared);

        if (shared)
        {
            if (_hostMap.TryGetValue(startupType, out var startup))
            {
                return startup;
            }
        }

        var host = StartupLoader.CreateHost(serviceType, startupType, assemblyName, diagnosticMessageSink);

        if (shared)
        {
            _hostMap.TryAdd(startupType, host);
        }

        _hosts.Add(host.Host);

        return host;
    }

    public Task StartAsync(CancellationToken cancellationToken) =>
        Task.WhenAll(_hosts.Select(x => x.StartAsync(cancellationToken)));

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _hosts.Reverse();

        return Task.WhenAll(_hosts.Select(x => x.StopAsync(cancellationToken)));
    }

    //DisposalTracker not support IAsyncDisposable
    public void Dispose() => Task.WaitAll(_hosts.Select(DisposeAsync).ToArray());

    private static Task DisposeAsync(IDisposable disposable)
    {
        switch (disposable)
        {
            case IAsyncDisposable ad:
                return ad.DisposeAsync().AsTask();
            default:
                disposable.Dispose();

                return Task.CompletedTask;
        }
    }
}