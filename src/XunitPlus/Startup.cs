using Inkslab;
using Inkslab.DI.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace XunitPlus;

/// <summary>
/// 默认启动类型。
/// </summary>
/// <param name="serviceType">当前单元测试类的类型。</param>
public class Startup(Type serviceType)
{
    /// <summary>
    /// 静态构造函数。
    /// </summary>
    static Startup()
    {
        using var startup = new XStartup("Inkslab.*.dll");

        startup.DoStartup();
    }

    /// <summary>
    /// 配置服务。
    /// </summary>
    /// <param name="services">服务池。</param>
    /// <param name="context">宿主上下文。</param>
    public virtual void ConfigureServices(IServiceCollection services, HostBuilderContext context)
    {
        var patternSeeks = serviceType.GetCustomAttributes<PatternSeekAttribute>();

        var dependencyInjectionServices = services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
            .DependencyInjection(new DependencyInjectionOptions(), context, context.Configuration, context.HostingEnvironment);


        if (patternSeeks is null)
        {
            dependencyInjectionServices = dependencyInjectionServices.SeekAssemblies();
        }
        else
        {
            var pattarns = patternSeeks.Select(x => x.Pattern).ToArray();

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
            .AddTransient(serviceType)
            .ConfigureByAuto();
    }
}