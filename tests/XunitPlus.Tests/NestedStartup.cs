using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit.Abstractions;

namespace XunitPlus.Tests;

/// <summary>
/// 依赖注入。
/// </summary>
public interface IDependencyInjectionByNestedStartup
{
    /// <summary>
    /// 测试。
    /// </summary>
    void Test();
}

/// <summary>
/// 干扰依赖注入。
/// </summary>
public class DisturbDependencyInjectionByNestedStartup : IDependencyInjectionByNestedStartup
{
    /// <summary>
    /// 测试。
    /// </summary>
    public void Test() => throw new NotImplementedException();
}

/// <summary>
/// 依赖注入。
/// </summary>
public class DependencyInjectionByNestedStartup : IDependencyInjectionByNestedStartup
{
    /// <summary>
    /// 测试。
    /// </summary>
    public void Test() => Debug.WriteLine("测试内嵌的启动类启动类的依赖注入。");
}

/// <summary>
/// 内嵌的启动。
/// </summary>
public class NestedStartup(ITestOutputHelper helper, IDependencyInjectionByNestedStartup dependency)
{
    /// <summary>
    /// 测试。
    /// </summary>
    [Fact]
    public void Test()
    {
        helper.WriteLine("测试");
        dependency.Test();
    }

    private /*static */ class Startup //? 不限访问级别。
    {
        public /* static */ IHostBuilder CreateHostBuilder() => new HostBuilder();

        public /* static */ void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IDependencyInjectionByNestedStartup, DependencyInjectionByNestedStartup>();
        }
    }
}