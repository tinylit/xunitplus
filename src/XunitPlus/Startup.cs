using Inkslab.DI.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace XunitPlus;

/// <summary>
/// 启动类。
/// </summary>
/// <param name="serviceType">服务类型。</param>
public class Startup(Type serviceType)
{
    /// <summary>
    /// 配置服务。
    /// </summary>
    /// <param name="services">服务池。</param>
    /// <param name="context">上下文。</param>
    public virtual void ConfigureServices(IServiceCollection services, HostBuilderContext context)
    {
        var patternSeeks = serviceType.GetCustomAttributes<PatternSeekAttribute>();

        if (patternSeeks is null)
        {
            patternSeeks = serviceType.Assembly.GetCustomAttributes<PatternSeekAttribute>();
        }
        else
        {
            patternSeeks = patternSeeks.Union(serviceType.Assembly.GetCustomAttributes<PatternSeekAttribute>() ?? Array.Empty<PatternSeekAttribute>());
        }

        var dependencyInjectionServices = services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
            .DependencyInjection(new DependencyInjectionOptions(), context, context.Configuration, context.HostingEnvironment);

        if (patternSeeks is null)
        {
            dependencyInjectionServices = dependencyInjectionServices.SeekAssemblies();
        }
        else
        {
            var pattarns = patternSeeks
                .Select(x => x.Pattern)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (pattarns.Length == 0)
            {
                dependencyInjectionServices = dependencyInjectionServices.SeekAssemblies();
            }
            else
            {
                dependencyInjectionServices = dependencyInjectionServices.SeekAssemblies(pattarns);
            }
        }

        dependencyInjectionServices.ConfigureByDefined()
            .IgnoreType<ITestOutputHelper>()
            .AddTransient(serviceType)
            .ConfigureByAuto();
    }
}