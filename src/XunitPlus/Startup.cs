using Inkslab;
using Inkslab.DI.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace XunitPlus;

public class Startup(Type serviceType)
{
    static Startup()
    {
        using var startup = new XStartup();

        startup.DoStartup();
    }

    public IHostBuilder CreateHostBuilder()
        => Host.CreateDefaultBuilder();

    public void ConfigureServices(IServiceCollection services, HostBuilderContext context)
        => services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
            .DependencyInjection(new DependencyInjectionOptions(), context, context.Configuration, context.HostingEnvironment)
            .SeekAssemblies()
            .ConfigureByDefined()
            .AddTransient(serviceType);
}