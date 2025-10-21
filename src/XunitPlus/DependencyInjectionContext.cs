using Microsoft.Extensions.Hosting;

namespace XunitPlus;

public class DependencyInjectionContext
{
    public DependencyInjectionContext(IHost host)
    {
        Host = host;
    }

    public IHost Host { get; }

    public IServiceProvider Services => Host.Services;
}