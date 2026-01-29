using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace XunitPlus;

internal static class StartupLoader
{
    public static DependencyInjectionContext CreateHost(Type serviceType, Type startupType, AssemblyName? assemblyName, IMessageSink? diagnosticMessageSink)
    {
        var (hostBuilder, startup, buildHostMethod, configureMethod) =
            CreateHostBuilder(serviceType, startupType, assemblyName, diagnosticMessageSink);

        return new(CreateHost(hostBuilder, serviceType, startupType, startup, buildHostMethod, configureMethod));
    }

    public static (IHostBuilder, object?, MethodInfo?, MethodInfo?) CreateHostBuilder(Type serviceType, Type startupType,
        AssemblyName? assemblyName, IMessageSink? diagnosticMessageSink)
    {
        var methodInfos = startupType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

        var createHostBuilderMethod = FindMethod(startupType, methodInfos, nameof(CreateHostBuilder), typeof(IHostBuilder));
        var configureHostMethod = FindMethod(startupType, methodInfos, nameof(ConfigureHost));
        var configureServicesMethod = FindMethod(startupType, methodInfos, nameof(ConfigureServices));
        var configureMethod = FindMethod(startupType, methodInfos, nameof(Configure));
        var buildHostMethod = FindMethod(startupType, methodInfos, nameof(BuildHost), typeof(IHost));

        var startup = createHostBuilderMethod is { IsStatic: false } ||
                      configureHostMethod is { IsStatic: false } ||
                      configureServicesMethod is { IsStatic: false } ||
                      buildHostMethod is { IsStatic: false } ||
                      configureMethod is { IsStatic: false }
            ? CreateStartup(serviceType, startupType)
            : null;

        var hostBuilder = CreateHostBuilder(assemblyName, startup, startupType, createHostBuilderMethod) ?? Host.CreateDefaultBuilder();

        if (diagnosticMessageSink != null)
        {
            hostBuilder.ConfigureServices(services => services.TryAddSingleton(diagnosticMessageSink));
        }

        // 配置日志记录
        hostBuilder.ConfigureServices(services =>
        {
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        });

        ConfigureHost(hostBuilder, startup, startupType, configureHostMethod);

        ConfigureServices(hostBuilder, startup, startupType, configureServicesMethod);

        hostBuilder.ConfigureServices(services =>
        {
            // ITestOutputHelper 不应该通过 DI 容器注册
            // xUnit 会在每个测试用例运行时自动创建和初始化它
            // 如果通过 DI 注册，会导致 "There is no currently active test" 错误

            services.TryAddSingleton<IHttpContextAccessor>(new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext()
            });
        });

        return (hostBuilder, startup, buildHostMethod, configureMethod);
    }

    public static IHost CreateHost(IHostBuilder hostBuilder, Type serviceType, Type startupType, object? startup,
        MethodInfo? buildHostMethod, MethodInfo? configureMethod)
    {
        var host = BuildHost(hostBuilder, startup, startupType, buildHostMethod) ?? hostBuilder.Build();

        Configure(host.Services, startup, configureMethod);

        var httpContextAccessor = host.Services.GetRequiredService<IHttpContextAccessor>();

        InitializeHttpContextAccessor(serviceType, httpContextAccessor);

        return host;
    }

    private static void InitializeHttpContextAccessor(Type serviceType, IHttpContextAccessor accessor)
    {
        var contextAttr = serviceType.GetCustomAttribute<HttpContextAttribute>();

        var context = contextAttr is null
            ? accessor.HttpContext ?? new DefaultHttpContext()
            : contextAttr.CreateContext();

        var headerAttrs = new List<HeaderAttribute>(serviceType.GetCustomAttributes<HeaderAttribute>());

        if (headerAttrs.Count > 0)
        {
            var features = context.Features;

            if (features is null)
            {
                if (context is DefaultHttpContext defaultHttpContext)
                {
                    defaultHttpContext.Initialize(features = new FeatureCollection());
                }
            }

            if (features is null)
            {
            }
            else
            {
                var requestFeature = features.Get<IHttpRequestFeature>();

                if (requestFeature is null)
                {
                    features.Set(requestFeature = new HttpRequestFeature
                    {
                        Headers = new HeaderDictionary(headerAttrs.Count)
                    });
                }
                else
                {
                    requestFeature.Headers ??= new HeaderDictionary(headerAttrs.Count);
                }

                foreach (var headerAttr in headerAttrs)
                {
                    requestFeature.Headers.Add(headerAttr.Key, headerAttr.StringValues);
                }
            }
        }

        var userAttr = serviceType.GetCustomAttribute<UserAttribute>();

        if (userAttr != null)
        {
            context.User = userAttr.CreateUser();
        }

        accessor.HttpContext = context;
    }

    public static object? CreateStartup(Type serviceType, Type startupType)
    {
        if (startupType is null)
        {
            throw new ArgumentNullException(nameof(startupType));
        }

        if (startupType is { IsAbstract: true, IsSealed: true })
        {
            return null;
        }

        var ctors = startupType.GetConstructors();

        if (ctors.Length != 1)
        {
            throw new InvalidOperationException(
                $"'{startupType.FullName}' must have a public constructor.");
        }

        var ctor = ctors[0];

        var parameterInfos = ctor.GetParameters();

        return parameterInfos.Length switch
        {
            0 => Activator.CreateInstance(startupType),
            1 when parameterInfos.All(x => x.ParameterType == typeof(Type)) => Activator.CreateInstance(startupType, new object[] { serviceType }),
            _ => throw new InvalidOperationException(
                                $"'{startupType.FullName}' must have a single parameterless public constructor, or public constructor with a single parameter of type 'Type'."),
        };
    }

    public static IHostBuilder? CreateHostBuilder(AssemblyName? assemblyName, object? startup, Type startupType,
        MethodInfo? method)
    {
        if (method is null)
        {
            return null;
        }

        var parameters = method.GetParameters();
        if (parameters.Length == 0)
        {
            return (IHostBuilder?)method.Invoke(method.IsStatic ? null : startup, Array.Empty<object>());
        }

        if (parameters.Length > 1 || parameters[0].ParameterType != typeof(AssemblyName))
        {
            throw new InvalidOperationException(
                $"The '{method.Name}' method of startup type '{startupType.FullName}' must parameterless or have the single 'AssemblyName' parameter.");
        }

        return assemblyName is null
            ? throw new InvalidOperationException(
                $"The '{method.Name}' method of startup type '{startupType.FullName}' must parameterless when use XunitWebApplicationFactory.")
            : (IHostBuilder?)method.Invoke(method.IsStatic ? null : startup, new object[] { assemblyName });
    }

    public static void ConfigureHost(IHostBuilder builder, object? startup, Type startupType, MethodInfo? method)
    {
        if (method is null)
        {
            return;
        }

        var parameters = method.GetParameters();

        if (parameters.Length != 1 || parameters[0].ParameterType != typeof(IHostBuilder))
        {
            throw new InvalidOperationException(
                $"The '{method.Name}' method of startup type '{startupType.FullName}' must have the single 'IHostBuilder' parameter.");
        }

        method.Invoke(method.IsStatic ? null : startup, new object[] { builder });
    }

    public static void ConfigureServices(IHostBuilder builder, object? startup, Type startupType, MethodInfo? method)
    {
        if (method is null)
        {
            return;
        }

        var parameters = method.GetParameters();

        Action<HostBuilderContext, IServiceCollection> configureDelegate = parameters.Length switch
        {
            1 when parameters[0].ParameterType == typeof(IServiceCollection) =>
                (_, services) => method.Invoke(method.IsStatic ? null : startup, new object[] { services }),
            2 when parameters[0].ParameterType == typeof(IServiceCollection) &&
                   parameters[1].ParameterType == typeof(HostBuilderContext) =>
                (context, services) =>
                    method.Invoke(method.IsStatic ? null : startup, new object[] { services, context }),
            2 when parameters[1].ParameterType == typeof(IServiceCollection) &&
                   parameters[0].ParameterType == typeof(HostBuilderContext) =>
                (context, services) =>
                    method.Invoke(method.IsStatic ? null : startup, new object[] { context, services }),
            _ => throw new InvalidOperationException(
                $"The '{method.Name}' method in the type '{startupType.FullName}' must have a 'IServiceCollection' parameter and optional 'HostBuilderContext' parameter.")
        };

        builder.ConfigureServices(configureDelegate);
    }

    // Not allow async Configure method
    public static void Configure(IServiceProvider provider, object? startup, MethodInfo? method)
    {
        if (method is null)
        {
            return;
        }

        using var scope = provider.CreateScope();

        method.Invoke(method.IsStatic ? null : startup, method.GetParameters()
            .Select(p => p.ParameterType)
            .Select(scope.ServiceProvider.GetRequiredService)
            .ToArray());
    }

    public static IHost? BuildHost(IHostBuilder hostBuilder, object? startup, Type startupType, MethodInfo? method)
    {
        if (method is null)
        {
            return null;
        }

        if (!typeof(IHost).IsAssignableFrom(method.ReturnType))
        {
            throw new InvalidOperationException(
                $"The '{method.Name}' method in the type '{startupType.FullName}' return type must assignable to '{typeof(IHost)}'.");
        }

        var parameters = method.GetParameters();

        return parameters.Length != 1 || parameters[0].ParameterType != typeof(IHostBuilder)
            ? throw new InvalidOperationException(
                $"The '{method.Name}' method of startup type '{startupType.FullName}' must have the single 'IHostBuilder' parameter.")
            : (IHost?)method.Invoke(method.IsStatic ? null : startup, new object[] { hostBuilder });
    }

    public static MethodInfo? FindMethod(Type startupType, MethodInfo[] methodInfos, string methodName) =>
        FindMethod(startupType, methodInfos, methodName, typeof(void));

    public static MethodInfo? FindMethod(Type startupType, MethodInfo[] methodInfos, string methodName, Type returnType)
    {
        var selectedMethods = Array.FindAll(methodInfos, x => x.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase));

        if (selectedMethods.Length > 1)
        {
            throw new InvalidOperationException(
                $"Having multiple overloads of method '{methodName}' is not supported.");
        }

        if (selectedMethods.Length == 0)
        {
            return null;
        }

        var methodInfo = selectedMethods[0];

        if (returnType == typeof(void))
        {
            if (methodInfo.ReturnType != returnType)
            {
                throw new InvalidOperationException(
                    $"The '{methodInfo.Name}' method in the type '{startupType.FullName}' must have no return type.");
            }
        }
        else if (!returnType.IsAssignableFrom(methodInfo.ReturnType))
        {
            throw new InvalidOperationException(
                $"The '{methodInfo.Name}' method in the type '{startupType.FullName}' return type must assignable to '{returnType}'.");
        }

        return methodInfo;
    }
}