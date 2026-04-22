using Inkslab.DI.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace XunitPlus;

/// <summary>
/// 启动类。
/// </summary>
public class Startup
{
    private readonly Type _serviceType;

    /// <summary>
    /// 构造函数。
    /// </summary>
    /// <param name="serviceType">服务类型。</param>
    public Startup(Type serviceType)
    {
        _serviceType = serviceType;
    }

    /// <summary>
    /// 配置服务。
    /// </summary>
    /// <param name="services">服务池。</param>
    /// <param name="context">上下文。</param>
    public virtual void ConfigureServices(IServiceCollection services, HostBuilderContext context)
    {
        var patternSeeks = _serviceType.GetCustomAttributes<PatternSeekAttribute>()
            .Union(_serviceType.Assembly.GetCustomAttributes<PatternSeekAttribute>());

        var dependencyInjectionServices = services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
            .DependencyInjection(new DependencyInjectionOptions(), context, context.Configuration, context.HostingEnvironment);

        var patterns = patternSeeks
            .Select(x => x.Pattern)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (patterns.Length == 0)
        {
            dependencyInjectionServices = dependencyInjectionServices.SeekAssemblies();
        }
        else
        {
            dependencyInjectionServices = dependencyInjectionServices.SeekAssemblies(patterns);
        }

        dependencyInjectionServices.ConfigureByDefined()
            .IgnoreType<ITestOutputHelper>()
            .AddTransient(_serviceType)
            .ConfigureByAuto();
    }
}