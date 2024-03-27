using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

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
        var createHostBuilderMethod = FindMethod(startupType, nameof(CreateHostBuilder), typeof(IHostBuilder));
        var configureHostMethod = FindMethod(startupType, nameof(ConfigureHost));
        var configureServicesMethod = FindMethod(startupType, nameof(ConfigureServices));
        var configureMethod = FindMethod(startupType, nameof(Configure));
        var buildHostMethod = FindMethod(startupType, nameof(BuildHost), typeof(IHost));

        var startup = createHostBuilderMethod is { IsStatic: false } ||
                      configureHostMethod is { IsStatic: false } ||
                      configureServicesMethod is { IsStatic: false } ||
                      buildHostMethod is { IsStatic: false } ||
                      configureMethod is { IsStatic: false }
            ? CreateStartup(serviceType, startupType)
            : null;

        var hostBuilder = CreateHostBuilder(assemblyName, startup, startupType, createHostBuilderMethod) ?? Host.CreateDefaultBuilder();

        if (diagnosticMessageSink != null)
            hostBuilder.ConfigureServices(services => services.TryAddSingleton(diagnosticMessageSink));

        ConfigureHost(hostBuilder, startup, startupType, configureHostMethod);

        ConfigureServices(hostBuilder, startup, startupType, configureServicesMethod);

        hostBuilder.ConfigureServices(services =>
        {
            var httpContextAccessorType = typeof(IHttpContextAccessor);

            if (services.All(x => x.ServiceType != httpContextAccessorType))
            {
                services.TryAddSingleton<IHttpContextAccessor>(new HttpContextAccessor
                {
                    HttpContext = new DefaultHttpContext()
                });
            }
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
            throw new ArgumentNullException(nameof(startupType));

        if (startupType is { IsAbstract: true, IsSealed: true }) return null;

        var ctors = startupType.GetConstructors();

        if (ctors.Length != 1)
            throw new InvalidOperationException(
                $"'{startupType.FullName}' must have a public constructor.");

        var ctor = ctors[0];

        var parameterInfos = ctor.GetParameters();

        switch (parameterInfos.Length)
        {
            case 0:
                return Activator.CreateInstance(startupType);
            case 1 when parameterInfos.All(x => x.ParameterType == typeof(Type)):
                return Activator.CreateInstance(startupType, new object[1] { serviceType });
            default:
                throw new InvalidOperationException(
                    $"'{startupType.FullName}' must have a single parameterless public constructor, or public constructor with a single parameter of type 'Type'.");
        }
    }

    public static IHostBuilder? CreateHostBuilder(AssemblyName? assemblyName, object? startup, Type startupType,
        MethodInfo? method)
    {
        if (method is null) return null;

        var parameters = method.GetParameters();
        if (parameters.Length == 0)
            return (IHostBuilder)method.Invoke(method.IsStatic ? null : startup, Array.Empty<object>());

        if (parameters.Length > 1 || parameters[0].ParameterType != typeof(AssemblyName))
            throw new InvalidOperationException(
                $"The '{method.Name}' method of startup type '{startupType.FullName}' must parameterless or have the single 'AssemblyName' parameter.");

        if (assemblyName is null)
            throw new InvalidOperationException(
                $"The '{method.Name}' method of startup type '{startupType.FullName}' must parameterless when use XunitWebApplicationFactory.");

        return (IHostBuilder)method.Invoke(method.IsStatic ? null : startup, [assemblyName]);
    }

    public static void ConfigureHost(IHostBuilder builder, object? startup, Type startupType, MethodInfo? method)
    {
        if (method is null) return;

        var parameters = method.GetParameters();
        if (parameters.Length != 1 || parameters[0].ParameterType != typeof(IHostBuilder))
            throw new InvalidOperationException(
                $"The '{method.Name}' method of startup type '{startupType.FullName}' must have the single 'IHostBuilder' parameter.");

        method.Invoke(method.IsStatic ? null : startup, [builder]);
    }

    public static void ConfigureServices(IHostBuilder builder, object? startup, Type startupType, MethodInfo? method)
    {
        if (method is null) return;

        var parameters = method.GetParameters();

        builder.ConfigureServices(parameters.Length switch
        {
            1 when parameters[0].ParameterType == typeof(IServiceCollection) =>
                (_, services) => method.Invoke(method.IsStatic ? null : startup, [services]),
            2 when parameters[0].ParameterType == typeof(IServiceCollection) &&
                   parameters[1].ParameterType == typeof(HostBuilderContext) =>
                (context, services) =>
                    method.Invoke(method.IsStatic ? null : startup, [services, context]),
            2 when parameters[1].ParameterType == typeof(IServiceCollection) &&
                   parameters[0].ParameterType == typeof(HostBuilderContext) =>
                (context, services) =>
                    method.Invoke(method.IsStatic ? null : startup, [context, services]),
            _ => throw new InvalidOperationException(
                $"The '{method.Name}' method in the type '{startupType.FullName}' must have a 'IServiceCollection' parameter and optional 'HostBuilderContext' parameter.")
        });
    }

    // Not allow async Configure method
    public static void Configure(IServiceProvider provider, object? startup, MethodInfo? method)
    {
        if (method is null) return;

        using var scope = provider.CreateScope();

        method.Invoke(method.IsStatic ? null : startup, method.GetParameters()
            .Select(p => p.ParameterType)
            .Select(scope.ServiceProvider.GetRequiredService)
            .ToArray());
    }

    public static IHost? BuildHost(IHostBuilder hostBuilder, object? startup, Type startupType, MethodInfo? method)
    {
        if (method is null) return null;

        if (!typeof(IHost).IsAssignableFrom(method.ReturnType))
            throw new InvalidOperationException(
                $"The '{method.Name}' method in the type '{startupType.FullName}' return type must assignable to '{typeof(IHost)}'.");

        var parameters = method.GetParameters();
        if (parameters.Length != 1 || parameters[0].ParameterType != typeof(IHostBuilder))
            throw new InvalidOperationException(
                $"The '{method.Name}' method of startup type '{startupType.FullName}' must have the single 'IHostBuilder' parameter.");

        return (IHost?)method.Invoke(method.IsStatic ? null : startup, [hostBuilder]);
    }

    public static MethodInfo? FindMethod(Type startupType, string methodName) =>
        FindMethod(startupType, methodName, typeof(void));

    public static MethodInfo? FindMethod(Type startupType, string methodName, Type returnType)
    {
        var selectedMethods = startupType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(method => method.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase)).ToList();

        if (selectedMethods.Count > 1)
            throw new InvalidOperationException(
                $"Having multiple overloads of method '{methodName}' is not supported.");

        var methodInfo = selectedMethods.FirstOrDefault();
        if (methodInfo == null) return methodInfo;

        if (returnType == typeof(void))
        {
            if (methodInfo.ReturnType != returnType)
                throw new InvalidOperationException(
                    $"The '{methodInfo.Name}' method in the type '{startupType.FullName}' must have no return type.");
        }
        else if (!returnType.IsAssignableFrom(methodInfo.ReturnType))
            throw new InvalidOperationException(
                $"The '{methodInfo.Name}' method in the type '{startupType.FullName}' return type must assignable to '{returnType}'.");

        return methodInfo;
    }
}