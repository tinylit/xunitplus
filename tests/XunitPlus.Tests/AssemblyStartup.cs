using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace XunitPlus.Tests;

/// <summary>
/// 依赖注入。
/// </summary>
public interface IDependencyInjectionByAssemblyStartup
{
    /// <summary>
    /// 测试。
    /// </summary>
    void Test();
}

/// <summary>
/// 依赖注入。
/// </summary>
public class DependencyInjectionByAssemblyStartup : IDependencyInjectionByAssemblyStartup
{
    /// <summary>
    /// 测试。
    /// </summary>
    public void Test() => Debug.WriteLine("测试程序集启动类依赖注入。");
}

/// <summary>
/// 启动类。
/// </summary>
public /* static */ class Startup
{
    public /* static */ IHostBuilder CreateHostBuilder() => new HostBuilder();

    public /* static */ void ConfigureServices(IServiceCollection services)
    {
        Debug.WriteLine("程序集启动类依赖注入。");
        
        services.AddTransient<IDependencyInjectionByAssemblyStartup, DependencyInjectionByAssemblyStartup>();
    }
}

/// <summary>
/// 程序集启动。
/// </summary>
public class AssemblyStartup(IDependencyInjectionByAssemblyStartup dependency)
{
    static AssemblyStartup()
    {
        Debug.WriteLine("测试构造函数优先。");
    }

    /// <summary>
    /// 测试。
    /// </summary>
    [Fact]
    public void Test() => dependency.Test();
}