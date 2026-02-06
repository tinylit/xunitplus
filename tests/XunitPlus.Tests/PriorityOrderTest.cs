using System.Diagnostics;
using Xunit.Abstractions;

namespace XunitPlus.Tests;

[TestPriority(1)]
public class FirstPriorityTest
{
    private readonly ITestOutputHelper _output;

    public FirstPriorityTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Test1()
    {
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] FirstPriorityTest.Test1 - Priority 1");
        Thread.Sleep(100); // 模拟耗时操作
        Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] FirstPriorityTest.Test1 执行完成");
    }

    [Fact]
    public void Test2()
    {
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] FirstPriorityTest.Test2 - Priority 1");
        Thread.Sleep(100);
        Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] FirstPriorityTest.Test2 执行完成");
    }
}

[TestPriority(2)]
public class SecondPriorityTest
{
    private readonly ITestOutputHelper _output;

    public SecondPriorityTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Test1()
    {
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SecondPriorityTest.Test1 - Priority 2");
        Thread.Sleep(100);
        Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SecondPriorityTest.Test1 执行完成");
    }

    [Fact]
    public void Test2()
    {
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SecondPriorityTest.Test2 - Priority 2");
        Thread.Sleep(100);
        Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SecondPriorityTest.Test2 执行完成");
    }
}

[TestPriority(3)]
public class ThirdPriorityTest
{
    private readonly ITestOutputHelper _output;

    public ThirdPriorityTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Test1()
    {
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ThirdPriorityTest.Test1 - Priority 3");
        Thread.Sleep(100);
        Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ThirdPriorityTest.Test1 执行完成");
    }

    [Fact]
    public void Test2()
    {
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ThirdPriorityTest.Test2 - Priority 3");
        Thread.Sleep(100);
        Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ThirdPriorityTest.Test2 执行完成");
    }
}
