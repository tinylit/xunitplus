using System.Diagnostics;
using Xunit.Abstractions;

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

/// <summary>
/// 步骤测试(非自动注入)。
/// </summary>
[Serializable]
public class XunitNonAutomaticInjectionStepTest
{
    private readonly ITestOutputHelper _logger;

    public XunitNonAutomaticInjectionStepTest(ITestOutputHelper logger)
    {
        _logger = logger;
    }

    [Fact]
    [Step(1)]
    public void Test()
    {
        _logger.WriteLine("测试步骤。");

        Debug.WriteLine("测试步骤执行 1。");
    }

    [Fact]
    [Step(2)]
    public void Test2()
    {
        Debug.WriteLine("测试步骤执行 2。");
    }

    [Fact]
    [Step(3)]
    public void Test3()
    {
        Debug.WriteLine("测试步骤执行 3。");
    }

    [Fact]
    [Step(4)]
    public void Test4()
    {
        Debug.WriteLine("测试步骤执行 4。");
    }

    [Fact]
    [Step(5)]
    public void Test5()
    {
        Debug.WriteLine("测试步骤执行 5。");
    }

    [Fact]
    [Step(6)]
    public void Test6()
    {
        Debug.WriteLine("测试步骤执行 6。");
    }

    [Fact]
    [Step(7)]
    public void Test7()
    {
        Debug.WriteLine("测试步骤执行 7。");
    }

    [Fact]
    [Step(8)]
    public void Test8()
    {
        Debug.WriteLine("测试步骤执行 8。");
    }

    [Fact]
    [Step(9)]
    public void Test9()
    {
        Debug.WriteLine("测试步骤执行 9。");
    }

    [Fact]
    [Step(10)]
    public void Test10()
    {
        Debug.WriteLine("测试步骤执行 10。");
    }

    [Fact]
    [Step(11)]
    public void Test11()
    {
        Debug.WriteLine("测试步骤执行 11。");
    }

    [Fact]
    [Step(12)]
    public void Test12()
    {
        Debug.WriteLine("测试步骤执行 12。");
    }

    [Fact]
    [Step(13)]
    public void Test13()
    {
        Debug.WriteLine("测试步骤执行 13。");
    }

    [Fact]
    [Step(14)]
    public void Test14()
    {
        Debug.WriteLine("测试步骤执行 14。");
    }

    [Fact]
    [Step(15)]
    public void Test15()
    {
        Debug.WriteLine("测试步骤执行 15。");
    }

    [Fact]
    [Step(16)]
    public void Test16()
    {
        Debug.WriteLine("测试步骤执行 16。");
    }

    [Fact]
    [Step(17)]
    public void Test17()
    {
        Debug.WriteLine("测试步骤执行 17。");
    }

    [Fact]
    [Step(18)]
    public void Test18()
    {
        Debug.WriteLine("测试步骤执行 18。");
    }

    [Fact]
    [Step(19)]
    public void Test19()
    {
        Debug.WriteLine("测试步骤执行 19。");
    }

    [Fact]
    [Step(20)]
    public void Test20()
    {
        Debug.WriteLine("测试步骤执行 20。");
    }

    [Fact]
    [Step(21)]
    public void Test21()
    {
        Debug.WriteLine("测试步骤执行 21。");
    }

    [Fact]
    [Step(22)]
    public void Test22()
    {
        Debug.WriteLine("测试步骤执行 22。");
    }
}

/// <summary>
/// 步骤测试(自动注入)。
/// </summary>
[Serializable]
public class StepTest
{
    private readonly IDependencyInjectionByAutomaticInjection _dependency;

    public StepTest(IDependencyInjectionByAutomaticInjection dependency)
    {
        _dependency = dependency;
    }

    [Fact]
    [Step(1)]
    public void Test()
    {
        _dependency.Test();

        Debug.WriteLine("测试步骤执行 1。");
    }

    [Fact]
    [Step(2)]
    public void Test2()
    {
        Debug.WriteLine("测试步骤执行 2。");
    }

    [Fact]
    [Step(3)]
    public void Test3()
    {
        Debug.WriteLine("测试步骤执行 3。");
    }

    [Fact]
    [Step(4)]
    public void Test4()
    {
        Debug.WriteLine("测试步骤执行 4。");
    }

    [Fact]
    [Step(5)]
    public void Test5()
    {
        Debug.WriteLine("测试步骤执行 5。");
    }

    [Fact]
    [Step(6)]
    public void Test6()
    {
        Debug.WriteLine("测试步骤执行 6。");
    }

    [Fact]
    [Step(7)]
    public void Test7()
    {
        Debug.WriteLine("测试步骤执行 7。");
    }

    [Fact]
    [Step(8)]
    public void Test8()
    {
        Debug.WriteLine("测试步骤执行 8。");
    }

    [Fact]
    [Step(9)]
    public void Test9()
    {
        Debug.WriteLine("测试步骤执行 9。");
    }

    [Fact]
    [Step(10)]
    public void Test10()
    {
        Debug.WriteLine("测试步骤执行 10。");
    }

    [Fact]
    [Step(11)]
    public void Test11()
    {
        Debug.WriteLine("测试步骤执行 11。");
    }

    [Fact]
    [Step(12)]
    public void Test12()
    {
        Debug.WriteLine("测试步骤执行 12。");
    }

    [Fact]
    [Step(13)]
    public void Test13()
    {
        Debug.WriteLine("测试步骤执行 13。");
    }

    [Fact]
    [Step(14)]
    public void Test14()
    {
        Debug.WriteLine("测试步骤执行 14。");
    }

    [Fact]
    [Step(15)]
    public void Test15()
    {
        Debug.WriteLine("测试步骤执行 15。");
    }

    [Fact]
    [Step(16)]
    public void Test16()
    {
        Debug.WriteLine("测试步骤执行 16。");
    }

    [Fact]
    [Step(17)]
    public void Test17()
    {
        Debug.WriteLine("测试步骤执行 17。");
    }

    [Fact]
    [Step(18)]
    public void Test18()
    {
        Debug.WriteLine("测试步骤执行 18。");
    }

    [Fact]
    [Step(19)]
    public void Test19()
    {
        Debug.WriteLine("测试步骤执行 19。");
    }

    [Fact]
    [Step(20)]
    public void Test20()
    {
        Debug.WriteLine("测试步骤执行 20。");
    }

    [Fact]
    [Step(21)]
    public void Test21()
    {
        Debug.WriteLine("测试步骤执行 21。");
    }

    [Fact]
    [Step(22)]
    public void Test22()
    {
        Debug.WriteLine("测试步骤执行 22。");
    }
}