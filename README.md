# xunit+ 

<!-- METADATA
name: xunit+
package_id: xunitPlus
type: xunit-extension-framework
platform: .NET 6+
dependencies: xunit 2.7+, Inkslab.DI 1.2.24+
features: auto-dependency-injection, startup-configuration, step-ordering, test-priority, httpcontext-simulation
-->

![Inkslab](inkslab.jpg 'Logo')

![GitHub](https://img.shields.io/github/license/tinylit/xunitplus.svg)
![language](https://img.shields.io/github/languages/top/tinylit/xunitplus.svg)
![codeSize](https://img.shields.io/github/languages/code-size/tinylit/xunitplus.svg)

> 基于 [xunit](https://github.com/xunit/xunit.git) + [Inkslab.DI](https://github.com/tinylit/inkslab.git) 的增强型单元测试框架，核心能力是**自动依赖注入**——构造函数参数自动从 DI 容器获取，无需手动 Mock。

---

## 目录

- [快速开始](#快速开始)
- [核心概念](#核心概念)
  - [自动依赖注入](#1-自动依赖注入)
  - [启动类配置](#2-启动类配置)
  - [测试步骤控制 `[Step]`](#3-测试步骤控制)
  - [测试类优先级 `[TestPriority]`](#4-测试类优先级)
  - [HTTP 上下文模拟](#5-http-上下文模拟)
- [API 参考](#api-参考)
- [项目结构](#项目结构)
- [常见问题](#常见问题)

---

## 快速开始

### 安装

```bash
dotnet add package xunitPlus
```

### 配置 `.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net6.0</TargetFramework>
        <IsPackable>false</IsPackable>
        <IsTestProject>true</IsTestProject>
    </PropertyGroup>

    <!-- 可选：指定程序集扫描规则，限制自动注入范围 -->
    <ItemGroup>
        <AssemblyAttribute Include="XunitPlus.PatternSeek">
            <_Parameter1>MyProject.*</_Parameter1>
        </AssemblyAttribute>
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.5.0" />
        <PackageReference Include="xunit" Version="2.7.0" />
        <PackageReference Include="xunit.runner.visualstudio" Version="2.5.8" />
        <PackageReference Include="xunitPlus" Version="1.2.12" />
    </ItemGroup>
</Project>
```

### 最简示例

```csharp
public interface ICalculator
{
    int Add(int a, int b);
}

public class Calculator : ICalculator
{
    public int Add(int a, int b) => a + b;
}

// 构造函数参数自动注入，无需手动配置
public class CalculatorTests
{
    private readonly ICalculator _calculator;

    public CalculatorTests(ICalculator calculator)
    {
        _calculator = calculator;
    }

    [Fact]
    public void Add_TwoNumbers_ReturnsSum()
    {
        Assert.Equal(5, _calculator.Add(2, 3));
    }
}
```

---

## 核心概念

### 1. 自动依赖注入

框架自动扫描程序集，将实现类注册到 DI 容器。测试类构造函数参数会自动从容器中解析。

**支持的特殊参数类型**（无需注册）：

| 类型 | 说明 |
|------|------|
| `ITestOutputHelper` | xunit 内置测试输出 |
| `CancellationToken` | 自动绑定测试取消令牌 |
| 带 `default` 值的参数 | 容器未注册时使用默认值 |
| 可选参数 (`optional`) | 容器未注册时使用类型默认值 |

#### 配置程序集扫描范围

通过 `[PatternSeek]` 特性限制 DI 扫描的程序集范围（支持通配符，规则同 `Directory.GetFiles`）：

```csharp
// 方式一：程序集级别（推荐在 .csproj 中配置）
[assembly: XunitPlus.PatternSeek("MyProject.*")]
[assembly: XunitPlus.PatternSeek("ThirdParty.*.dll")]

// 方式二：测试类级别（仅影响该类）
[PatternSeek("MyProject.*")]
public class MyTests { }
```

### 2. 启动类配置

框架按以下**优先级**（从高到低）查找启动类：

| 优先级 | 方式 | 说明 |
|:------:|------|------|
| 1 | `[Startup(typeof(T))]` 特性 | 指定启动类类型，可设置 `Shared = true` 共享实例 |
| 2 | 内嵌 `Startup` 类 | 测试类内部定义的嵌套类，向上查找外层类 |
| 3 | 命名空间级 `Startup` 类 | 从当前命名空间逐级向上查找同名 `Startup` 类 |
| 4 | `XunitPlus.Startup`（默认） | 框架内置默认启动类，自动扫描并注册依赖 |

#### 启动类约定

启动类支持以下**可选**方法（均可为实例方法或静态方法）：

```csharp
public class MyStartup
{
    // 1. 创建 HostBuilder（可选，返回 IHostBuilder）
    public IHostBuilder CreateHostBuilder() => Host.CreateDefaultBuilder();
    // 或带参数版本：
    // public IHostBuilder CreateHostBuilder(AssemblyName assemblyName) => ...;

    // 2. 配置宿主（可选，参数为 IHostBuilder）
    public void ConfigureHost(IHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((ctx, config) =>
        {
            config.AddJsonFile("appsettings.json");
        });
    }

    // 3. 配置服务（可选，参数组合灵活）
    public void ConfigureServices(IServiceCollection services) { }
    // 或：
    // public void ConfigureServices(IServiceCollection services, HostBuilderContext context) { }
    // 或：
    // public void ConfigureServices(HostBuilderContext context, IServiceCollection services) { }

    // 4. 构建宿主（可选，返回 IHost）
    public IHost BuildHost(IHostBuilder builder) => builder.Build();

    // 5. 应用配置（可选，参数从 DI 容器解析）
    public void Configure(IServiceProvider provider) { }
}
```

> **注意**：启动类构造函数支持无参或单个 `Type` 参数（表示当前测试类类型）。

#### 示例：`[Startup]` 特性指定

```csharp
public class CustomStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<ICalculator, Calculator>();
    }
}

// Shared = true 时，同一启动类型的多个测试类共享同一个 Host
[Startup(typeof(CustomStartup), Shared = true)]
public class CalculatorTests
{
    private readonly ICalculator _calculator;

    public CalculatorTests(ICalculator calculator)
    {
        _calculator = calculator;
    }

    [Fact]
    public void Test() => Assert.NotNull(_calculator);
}
```

#### 示例：内嵌 `Startup` 类

```csharp
public class NestedStartupTests
{
    private readonly ICalculator _calculator;

    public NestedStartupTests(ICalculator calculator)
    {
        _calculator = calculator;
    }

    [Fact]
    public void Test() => Assert.NotNull(_calculator);

    // 不限访问级别，可以是 private/internal/public
    private class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ICalculator, Calculator>();
        }
    }
}
```

### 3. 测试步骤控制

使用 `[Step(priority)]` 控制**同一测试类内**的测试方法执行顺序（值越小越先执行）。

**必须**配合 `[Serializable]` 或 `[Collection]` 使用，否则测试方法将并行执行，顺序不保证。

```csharp
[Serializable]
public class OrderedTests
{
    private static int _counter = 0;

    [Fact, Step(1)]
    public void FirstTest()
    {
        _counter++;
        Assert.Equal(1, _counter);
    }

    [Fact, Step(2)]
    public void SecondTest()
    {
        _counter++;
        Assert.Equal(2, _counter);
    }

    [Fact, Step(3)]
    public void ThirdTest()
    {
        _counter++;
        Assert.Equal(3, _counter);
    }
}
```

> 未标记 `[Step]` 的方法排在已标记方法之后，按方法名字母序执行。

### 4. 测试类优先级

使用 `[TestPriority(value)]` 控制**不同测试类/集合之间**的执行顺序（值越小越先执行）。

```csharp
[TestPriority(1)]
public class FirstPriorityTest
{
    [Fact]
    public void Test() { /* 最先执行 */ }
}

[TestPriority(2)]
public class SecondPriorityTest
{
    [Fact]
    public void Test() { /* 其次执行 */ }
}
```

### 5. HTTP 上下文模拟

框架内置 `IHttpContextAccessor` 支持，并提供便捷特性模拟请求上下文：

| 特性 | 说明 | 覆盖优先级 |
|------|------|:----------:|
| `[Header(key, value)]` | 添加 HTTP 请求头（可多次标记） | 低 |
| `[Authorization(token)]` | 添加 `Authorization` 请求头 | 低 |
| `[HttpContext]`（抽象类） | 自定义创建整个 `HttpContext` | 高 |
| `[User]`（抽象类） | 自定义创建 `ClaimsPrincipal` 用户 | 高 |

#### 示例：请求头与认证

```csharp
[Header("X-Custom-Header", "CustomValue")]
[Authorization("Bearer your-jwt-token-here")]
public class ApiTests
{
    private readonly IHttpContextAccessor _accessor;

    public ApiTests(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    [Fact]
    public void Test_Headers_AreSet()
    {
        var headers = _accessor.HttpContext!.Request.Headers;
        Assert.Equal("CustomValue", headers["X-Custom-Header"]);
        Assert.Equal("Bearer your-jwt-token-here", headers.Authorization);
    }
}
```

#### 示例：自定义用户

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class UserAccountAttribute : UserAttribute
{
    private readonly long _id;
    private readonly string _role;

    public UserAccountAttribute(long id, string role)
    {
        _id = id;
        _role = role;
    }

    public override ClaimsPrincipal CreateUser()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, _id.ToString()),
            new(ClaimTypes.Role, _role)
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }
}

[UserAccount(1, "Admin")]
public class UserTests
{
    private readonly IHttpContextAccessor _accessor;

    public UserTests(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    [Fact]
    public void Test_User_IsAuthenticated()
    {
        Assert.True(_accessor.HttpContext!.User.Identity!.IsAuthenticated);
        Assert.Equal("1", _accessor.HttpContext.User.Identity.Name);
    }
}
```

---

## API 参考

### 特性（Attributes）

| 特性 | 目标 | 参数 | 说明 |
|------|------|------|------|
| `[PatternSeek(pattern)]` | Assembly / Class | `string pattern` — DLL 匹配模式 | 限制 DI 程序集扫描范围 |
| `[Startup(type)]` | Class | `Type startupType`, `bool Shared = false` | 指定启动类 |
| `[Step(priority)]` | Method | `int priority` — 执行优先级（默认 0） | 方法级执行顺序 |
| `[TestPriority(value)]` | Class | `int value` — 执行优先级（默认 0） | 类/集合级执行顺序 |
| `[Header(key, value)]` | Class | `string key`, `string stringValues` | 添加 HTTP 请求头 |
| `[Authorization(token)]` | Class | `string token` | 添加 Authorization 请求头 |
| `[HttpContext]` | Class | 抽象类，需实现 `CreateContext()` | 自定义 HttpContext |
| `[User]` | Class | 抽象类，需实现 `CreateUser()` | 自定义 ClaimsPrincipal |

### 启动类方法约定

| 方法名 | 返回类型 | 参数 | 说明 |
|--------|----------|------|------|
| `CreateHostBuilder` | `IHostBuilder` | 无 或 `AssemblyName` | 创建宿主构建器 |
| `ConfigureHost` | `void` | `IHostBuilder` | 配置宿主 |
| `ConfigureServices` | `void` | `IServiceCollection` [, `HostBuilderContext`] | 注册服务 |
| `BuildHost` | `IHost` | `IHostBuilder` | 构建宿主 |
| `Configure` | `void` | 任意（从 DI 解析） | 应用配置 |

### 执行行为

| 场景 | 行为 |
|------|------|
| 测试类未标记 `[Serializable]` 或 `[Collection]` | 类内方法**并行**执行 |
| 测试类标记 `[Serializable]` 或 `[Collection]` | 类内方法**串行**执行（`[Step]` 排序生效） |
| 测试类标记 `[TestPriority]` | 跨类按优先级**串行**执行 |

---

## 项目结构

```
xunitplus/
├── src/XunitPlus/                          # 核心框架
│   ├── XunitPlusXunitTestFramework.cs      # 框架入口（替换 xunit 默认框架）
│   ├── XunitPlusTestFrameworkExecutor.cs   # 测试执行器（创建 Host、管理生命周期）
│   ├── XunitPlusTestAssemblyRunner.cs      # 程序集运行器（禁用集合并行）
│   ├── XunitPlusTestCollectionRunner.cs    # 集合运行器（DI 上下文分发）
│   ├── XunitPlusTestClassRunner.cs         # 类运行器（构造函数注入）
│   ├── XunitPlusTestMethodRunner.cs        # 方法运行器（并行执行）
│   ├── Startup.cs                          # 默认启动类
│   ├── StartupLoader.cs                    # 启动类加载与 Host 构建
│   ├── HostManager.cs                      # Host 生命周期管理
│   ├── DependencyInjectionContext.cs       # DI 上下文封装
│   ├── StepOrderer.cs                      # [Step] 排序器
│   ├── StepCollectionOrderer.cs            # [TestPriority] 集合排序器
│   └── ...                                 # 特性定义
├── tests/
│   ├── XunitPlus.Tests/                    # 基础功能测试
│   ├── XunitPlus.Auto.Tests/              # 自动注入 + 步骤控制测试
│   └── XunitPlus.User.Tests/             # HTTP 上下文 + 用户模拟测试
└── README.md
```

---

## 常见问题

### Q: 服务没有被注入？

1. 确认 `[PatternSeek]` 匹配规则覆盖了目标程序集
2. 确认服务类是 `public` 的，且有公共构造函数
3. 如无 `[PatternSeek]`，框架默认使用 `Inkslab.*` 扫描模式

### Q: `[Step]` 特性不生效？

在测试类上添加 `[Serializable]` 特性，使类内测试方法串行执行。否则方法将并行执行，顺序无法保证。

### Q: 如何完全手动配置服务？

使用 `[Startup]` 特性指定自定义启动类，在 `ConfigureServices` 中手动注册所有服务。

### Q: 多个测试类如何共享 Host？

使用 `[Startup(typeof(SharedStartup), Shared = true)]`，相同启动类型的测试类将共享同一个 Host 实例。

---

## 相关链接

- **NuGet**: [https://www.nuget.org/packages/xunitPlus/](https://www.nuget.org/packages/xunitPlus/)
- **GitHub**: [https://github.com/tinylit/xunitplus](https://github.com/tinylit/xunitplus)
- **xunit**: [https://xunit.net/](https://xunit.net/)
- **Inkslab**: [https://github.com/tinylit/inkslab](https://github.com/tinylit/inkslab)

## 许可证

MIT — 详见 [LICENSE](LICENSE)
