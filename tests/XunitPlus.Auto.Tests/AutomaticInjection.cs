using System.Diagnostics;

namespace XunitPlus.Auto.Tests;

/// <summary>
/// 依赖注入。
/// </summary>
public interface IDependencyInjectionByAutomaticInjection
{
    /// <summary>
    /// 测试。
    /// </summary>
    void Test();
}

/// <summary>
/// 依赖注入。
/// </summary>
public class DependencyInjectionByAutomaticInjection : IDependencyInjectionByAutomaticInjection
{
    /// <summary>
    /// 测试。
    /// </summary>
    public void Test() => Debug.WriteLine("测试自动依赖注入。");
}

/// <summary>
/// 自动注入。
/// </summary>
public class AutomaticInjection
{
    private readonly IDependencyInjectionByAutomaticInjection _dependency;

    public AutomaticInjection(IDependencyInjectionByAutomaticInjection dependency)
    {
        _dependency = dependency;
    }

    /// <summary>
    /// 测试。
    /// </summary>
    [Fact]
    public void Test() => _dependency.Test();
}