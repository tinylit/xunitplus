# xunit+ 框架使用指南

![Inkslab](inkslab.jpg 'Logo')

![GitHub](https://img.shields.io/github/license/tinylit/xunitplus.svg)
![language](https://img.shields.io/github/languages/top/tinylit/xunitplus.svg)
![codeSize](https://img.shields.io/github/languages/code-size/tinylit/xunitplus.svg)

## 目录

- [框架介绍](#框架介绍)
- [快速开始](#快速开始)
- [核心特性](#核心特性)
- [使用指南](#使用指南)
- [高级用法](#高级用法)
- [常见问题](#常见问题)
- [示例项目](#示例项目)

## 框架介绍

**[xunit+](https://www.nuget.org/packages/xunitPlus/)** 是基于 [xunit](https://github.com/xunit/xunit.git) 测试框架，结合 [inkslab](https://github.com/tinylit/inkslab.git) 依赖注入框架开发的增强型单元测试框架。它支持：

-  **自动依赖注入**：构造函数参数自动注入，无需手动创建测试对象
-  **灵活的启动配置**：支持多种启动类配置方式
-  **ASP.NET Core 集成**：完美支持 `IHostBuilder` 和 `IServiceCollection`
-  **测试步骤控制**：通过 `[Step]` 特性控制测试执行顺序
-  **HTTP 上下文支持**：内置 `IHttpContextAccessor` 支持

## 快速开始

### 安装

通过 NuGet 包管理器安装：

```bash
PM> Install-Package xunitPlus
```

或通过 .NET CLI：

```bash
dotnet add package xunitPlus
```

### 基础用例

创建一个最简单的单元测试：

```csharp
// 定义测试接口和实现
public interface ICalculator
{
    int Add(int a, int b);
}

public class Calculator : ICalculator
{
    public int Add(int a, int b) => a + b;
}

// 编写单元测试（构造函数参数自动注入）
public class CalculatorTests
{
    private readonly ICalculator _calculator;

    // 构造函数参数会自动从 DI 容器中注入
    public CalculatorTests(ICalculator calculator)
    {
        _calculator = calculator;
    }

    [Fact]
    public void Add_TwoNumbers_ReturnsSum()
    {
        // Arrange & Act
        var result = _calculator.Add(2, 3);

        // Assert
        Assert.Equal(5, result);
    }
}
```

**注意**：框架会自动扫描程序集并注册实现类，无需手动配置服务！

## 核心特性

### 1. 自动依赖注入

xunit+ 会自动扫描并注册程序集中的服务，支持：

- 自动注册实现类到 DI 容器
- 支持构造函数参数自动注入
- 支持 `ITestOutputHelper` 等 xunit 特殊类型

#### 配置程序集扫描规则

通过 `[PatternSeek]` 特性配置要扫描的程序集：

```csharp
// 在 AssemblyInfo.cs 或测试类上标记
[assembly: XunitPlus.PatternSeek("MyProject.*")]
[assembly: XunitPlus.PatternSeek("ThirdParty.*.dll")]
```

或在 `.csproj` 文件中配置：

```xml
<ItemGroup>
    <AssemblyAttribute Include="XunitPlus.PatternSeek">
        <_Parameter1>MyProject.*</_Parameter1>
    </AssemblyAttribute>
</ItemGroup>
```

### 2. 启动类配置

xunit+ 提供多种启动类配置方式，优先级从高到低：

#### 方式一：通过 `[Startup]` 特性指定

```csharp
public class CustomStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<ICalculator, Calculator>();
    }
}

[Startup(typeof(CustomStartup))]
public class CalculatorTests
{
    private readonly ICalculator _calculator;

    public CalculatorTests(ICalculator calculator)
    {
        _calculator = calculator;
    }

    [Fact]
    public void Test_WithCustomStartup()
    {
        Assert.NotNull(_calculator);
    }
}
```

#### 方式二：内嵌 `Startup` 类

```csharp
public class NestedStartupTests
{
    private readonly ICalculator _calculator;

    public NestedStartupTests(ICalculator calculator)
    {
        _calculator = calculator;
    }

    [Fact]
    public void Test_WithNestedStartup()
    {
        Assert.NotNull(_calculator);
    }

    // 内嵌启动类
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ICalculator, Calculator>();
        }
    }
}
```

#### 方式三：程序集级别 `Startup` 类

在测试项目的命名空间下创建 `Startup` 类：

```csharp
namespace MyProject.Tests
{
    // 程序集级别的启动类
    public class Startup
    {
        // 创建宿主构建器（可选）
        public IHostBuilder CreateHostBuilder() 
            => Host.CreateDefaultBuilder();

        // 配置宿主构建器（可选）
        public void ConfigureHost(IHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json");
            });
        }

        // 配置服务（可选）
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ICalculator, Calculator>();
        }

        // 构建宿主（可选）
        public IHost BuildHost(IHostBuilder builder) => builder.Build();

        // 应用配置（可选）
        public void Configure(IServiceProvider provider)
        {
            // 应用启动配置
        }
    }
}
```

#### 方式四：框架默认级别 `XunitPlus.Startup` 类

* 注：自动按照指定的`PatternSeekAttribute`的规则（未指定时，默认为所有程序集）扫描程序集，自动注入当前单元测试类所需依赖。

```csharp
public class DefaultStartupTests
{
    private readonly ICalculator _calculator;

    public DefaultStartupTests(ICalculator calculator)
    {
        _calculator = calculator;
    }

    [Fact]
    public void Test_WithNestedStartup()
    {
        Assert.NotNull(_calculator);
    }
}
```

> **注意**：
> 1. 启动类可以支持唯一的构造函数参数类型为 `Type`，表示当前运行的单元测试类
> 2. 启动类的所有方法均为可选，返回值类型为 `void` 的不可更改
> 3. 不区分是否是静态方法或静态类

### 3. 测试步骤控制

使用 `[Step]` 特性控制测试执行顺序，确保测试按照指定顺序运行：

```csharp
[Serializable]
public class OrderedTests
{
    private static int _counter = 0;

    [Fact]
    [Step(1)]
    public void FirstTest()
    {
        _counter++;
        Assert.Equal(1, _counter);
    }

    [Fact]
    [Step(2)]
    public void SecondTest()
    {
        _counter++;
        Assert.Equal(2, _counter);
    }

    [Fact]
    [Step(3)]
    public void ThirdTest()
    {
        _counter++;
        Assert.Equal(3, _counter);
    }
}
```

> **重要**：需要在测试类上添加 `[Serializable]` 特性，否则测试顺序可能不会生效。

### 4. ASP.NET Core 集成测试

xunit+ 完美支持 ASP.NET Core 集成测试：

```csharp
public class WebApiTests
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public WebApiTests(
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    [Fact]
    public void Test_HttpContext_IsAvailable()
    {
        Assert.NotNull(_httpContextAccessor);
    }

    public class Startup
    {
        public IHostBuilder CreateHostBuilder()
            => Host.CreateDefaultBuilder();

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
        }
    }
}
```

### 5. 便捷标记特性

框架提供多种便捷标记，可以混合使用（权重从上往下依次递增）：

- `[Header]` - 添加 HTTP 请求头
- `[Authorization]` - 添加认证请求头
- `[HttpContext]` - 创建 HttpContext 请求上下文（需继承）
- `[User]` - 创建请求用户信息（需继承）

```csharp
[Header("X-Custom-Header", "CustomValue")]
[Authorization("Bearer", "your-token-here")]
public class ApiTests
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiTests(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    [Fact]
    public void Test_Headers_AreSet()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        Assert.NotNull(httpContext);
    }
}
```

## 使用指南

### 项目配置

#### 1. 创建测试项目

```bash
dotnet new xunit -n MyProject.Tests
cd MyProject.Tests
dotnet add package xunitPlus
```

#### 2. 配置 `.csproj` 文件

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net6.0</TargetFramework>
        <IsPackable>false</IsPackable>
        <IsTestProject>true</IsTestProject>
    </PropertyGroup>

    <ItemGroup>
        <!-- 配置程序集扫描规则 -->
        <AssemblyAttribute Include="XunitPlus.PatternSeek">
            <_Parameter1>MyProject.*</_Parameter1>
        </AssemblyAttribute>
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.5.0" />
        <PackageReference Include="xunit" Version="2.7.0" />
        <PackageReference Include="xunit.runner.visualstudio" Version="2.5.8" />
        <PackageReference Include="xunitPlus" Version="1.2.8" />
    </ItemGroup>
</Project>
```

## 高级用法

### 1. 自定义服务生命周期

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Transient：每次请求创建新实例
        services.AddTransient<ITransientService, TransientService>();
        
        // Scoped：每个作用域创建一个实例
        services.AddScoped<IScopedService, ScopedService>();
        
        // Singleton：整个应用生命周期只创建一个实例
        services.AddSingleton<ISingletonService, SingletonService>();
    }
}
```

### 2. 使用配置文件

```csharp
public class Startup
{
    public IHostBuilder CreateHostBuilder()
        => Host.CreateDefaultBuilder();

    public void ConfigureHost(IHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.SetBasePath(Directory.GetCurrentDirectory())
                  .AddJsonFile("appsettings.json", optional: false)
                  .AddEnvironmentVariables();
        });
    }

    public void ConfigureServices(IServiceCollection services, HostBuilderContext context)
    {
        var configuration = context.Configuration;
        services.Configure<MyOptions>(configuration.GetSection("MyOptions"));
    }
}
```

### 3. 使用 ITestOutputHelper

```csharp
public class OutputTests
{
    private readonly ITestOutputHelper _output;
    private readonly ICalculator _calculator;

    public OutputTests(ITestOutputHelper output, ICalculator calculator)
    {
        _output = output;
        _calculator = calculator;
    }

    [Fact]
    public void Test_WithOutput()
    {
        _output.WriteLine("开始测试...");
        var result = _calculator.Add(2, 3);
        _output.WriteLine($"计算结果: {result}");
        Assert.Equal(5, result);
    }
}
```

## 常见问题

### Q1: 为什么我的服务没有被注入？

**检查以下几点**：
1. 确认已配置 `[PatternSeek]` 特性，且匹配程序集名称
2. 确认服务类是 `public` 的
3. 确认服务类有公共构造函数
4. 查看测试输出中的诊断信息

### Q2: 如何禁用自动程序集扫描？

如果不想使用自动扫描，可以完全手动配置服务：

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 手动注册所有服务
        services.AddTransient<IService1, Service1>();
        services.AddTransient<IService2, Service2>();
    }
}
```

### Q3: 多个测试类如何共享启动配置？

使用程序集级别的 `Startup` 类（见方式三）。

### Q4: [Step] 特性不生效怎么办？

确保测试类添加了 `[Serializable]` 特性：

```csharp
[Serializable]
public class OrderedTests
{
    [Fact]
    [Step(1)]
    public void FirstTest() { }
    
    [Fact]
    [Step(2)]
    public void SecondTest() { }
}
```

### Q5: 如何在测试中使用 Moq 等 Mock 框架？

完全兼容，可以混合使用：

```csharp
public class MockTests
{
    [Fact]
    public void Test_WithMock()
    {
        // 使用 Moq 创建 Mock 对象
        var mockCalculator = new Mock<ICalculator>();
        mockCalculator.Setup(x => x.Add(It.IsAny<int>(), It.IsAny<int>()))
                     .Returns(10);

        var result = mockCalculator.Object.Add(2, 3);
        Assert.Equal(10, result);
    }
}
```

## 示例项目

查看完整示例：

- `tests/XunitPlus.Tests` - 基础功能示例
- `tests/XunitPlus.Auto.Tests` - 自动注入和步骤控制示例
- `tests/XunitPlus.User.Tests` - HTTP 上下文和用户场景示例

## 项目结构

```
xunitplus/
 src/
    XunitPlus/                           # 核心框架代码
        XunitPlusXunitTestFramework.cs   # 测试框架入口
        XunitPlusTestFrameworkExecutor.cs # 测试执行器
        StepOrderer.cs                   # 步骤排序器
        Startup.cs                       # 默认启动类
        StartupAttribute.cs              # 启动类特性
        StepAttribute.cs                 # 测试步骤特性
        ...
 tests/
     XunitPlus.Tests/              # 基础测试
     XunitPlus.Auto.Tests/         # 自动注入测试
     XunitPlus.User.Tests/         # 用户场景测试
```

## 相关链接

- **NuGet 包**: [https://www.nuget.org/packages/xunitPlus/](https://www.nuget.org/packages/xunitPlus/)
- **GitHub**: [https://github.com/tinylit/xunitplus](https://github.com/tinylit/xunitplus)
- **xunit 官方文档**: [https://xunit.net/](https://xunit.net/)
- **inkslab**: [https://github.com/tinylit/inkslab](https://github.com/tinylit/inkslab)

## 许可证

本项目基于 MIT 许可证开源。详见 [LICENSE](LICENSE) 文件。

## 贡献

欢迎提交 Issue 和 Pull Request！

---

**Happy Testing!**
