using Microsoft.Extensions.Hosting;

namespace XunitPlus;

public class DependencyInjectionContext(IHost host)
{
    public IHost Host { get; } = host;

    public IServiceProvider Services => Host.Services;
}