using Inkslab;
using Inkslab.DI.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace XunitPlus;

/// <summary>
/// Ĭ���������͡�
/// </summary>
/// <param name="serviceType">��ǰ��Ԫ����������͡�</param>
public class Startup(Type serviceType)
{
    /// <summary>
    /// ��̬���캯����
    /// </summary>
    static Startup()
    {
        using var startup = new XStartup("Inkslab.*.dll");

        startup.DoStartup();
    }

    /// <summary>
    /// ���÷���
    /// </summary>
    /// <param name="services">����ء�</param>
    /// <param name="context">���������ġ�</param>
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