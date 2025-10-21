using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace XunitPlus.Tests;

/// <summary>
/// 依赖注入。
/// </summary>
public interface IDependencyInjectionByManuallySpecifyStartup
{
    /// <summary>
    /// 测试。
    /// </summary>
    void Test();
}

/// <summary>
/// 干扰依赖注入。
/// </summary>
public class DisturbDependencyInjectionByManuallySpecifyStartup : IDependencyInjectionByManuallySpecifyStartup
{
    /// <summary>
    /// 测试。
    /// </summary>
    public void Test() => throw new NotImplementedException();
}

/// <summary>
/// 依赖注入。
/// </summary>
public class DependencyInjectionByManuallySpecifyStartup : IDependencyInjectionByManuallySpecifyStartup
{
    /// <summary>
    /// 测试。
    /// </summary>
    public void Test() => Debug.WriteLine("测试手动指定启动类的依赖注入。");
}

public class ManuallyStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<IDependencyInjectionByManuallySpecifyStartup, DependencyInjectionByManuallySpecifyStartup>();
    }
}

/// <summary>
/// 指定启动。
/// </summary>
[Startup(typeof(ManuallyStartup), Shared = true)]
public class ManuallySpecifyStartup
{
    private readonly IDependencyInjectionByManuallySpecifyStartup _dependency;

    public ManuallySpecifyStartup(IDependencyInjectionByManuallySpecifyStartup dependency)
    {
        _dependency = dependency;
    }

    /// <summary>
    /// 测试。
    /// </summary>
    [Fact]
    public void Test() => _dependency.Test();
}