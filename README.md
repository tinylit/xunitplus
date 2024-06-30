![Inkslab](inkslab.jpg 'Logo')

![GitHub](https://img.shields.io/github/license/tinylit/xunitplus.svg)
![language](https://img.shields.io/github/languages/top/tinylit/xunitplus.svg)
![codeSize](https://img.shields.io/github/languages/code-size/tinylit/xunitplus.svg)

### “[xunit+](https://www.nuget.org/packages/xunitPlus/)”是什么？

[xunit+](https://www.nuget.org/packages/xunitPlus/) 是基于“[xunit](https://github.com/xunit/xunit.git)”再结合“[inkslab](https://github.com/tinylit/inkslab.git)”对构造函数参数进行自动注入的单元测试包。

### 如何安装？
First, [install NuGet](http://docs.nuget.org/docs/start-here/installing-nuget). Then, install [xunit+](https://www.nuget.org/packages/xunitPlus/) from the package manager console: 

```
PM> Install-Package xunitPlus
```
### 如何使用？
* 通用模式：
  - 单元测试类的构造函数正常注入即可。 
* 自定义模式：
  - 自定义启动类。
  ```C#
  ///<summary>
  /// 启动类（可选）。
  ///</summary>
  public class Startup {

    ///<summary>
    /// 创建宿主构建器（可选）。
    ///</summary>
    public IHostBuilder CreateHostBuilder() => Host.CreateDefaultBuilder();

    ///<summary>
    /// 配置宿主构建器（可选）。
    ///</summary>
    public void ConfigureHost(IHostBuilder builder){
        //TODO: 配置构建器。
    }

    ///<summary>
    /// 配置依赖注入的服务及生命周期（可选）。
    ///</summary>
    public void ConfigureServices(IServiceCollection services){
        //TODO: 配置依赖注入。
    }

    ///<summary>
    /// 构建宿主（可选）。
    ///</summary>
    public IHost BuildHost(IHostBuilder builder) => builder.Build();

    ///<summary>
    /// 配置服务（可选）。
    ///</summary>
    public void Configure(/* 参数将被自动依赖注入。 */){
        //TODO: 配置服务。
    }
  }
  ```
  > 1. 启动类可以支持唯一的构造函数参数类型为 `Type` 表示当前运行的单元测试类。
  > 2. 启动类的所有方法均为可选，返回值类型为 `void` 的不可更改，否则方法返回值相同或返回值必须是指定接口的实现类。
  > 3. 方法均是可选方法。
  > 4. 不区分是否是静态方法。
  > 5. 不区分是否是静态类。
  
  - 启动类查找规则：权重从上到下依次 **递减**。
    * 指定启动类。
        ```C#
        ///<summary>
        /// 指定类。
        ///</summary>
        public class SpecifyStartup { 
            
            ///<summary>
            /// 配置依赖注入的服务及生命周期（可选）。
            ///</summary>
            public void ConfigureServices(IServiceCollection services){
                // 注入需要的参数。
                services.AddTransient<ITest, Test>();
            }
        }

        ///<summary>
        /// 指定类测试。
        ///</summary>
        [Startup(typeof(SpecifyStartup))]
        public class SpecifyStartupTests(ITest test) {

            ///<summary>
            /// 测试。
            ///</summary>
            [Fact]
            public void Test() {
                Assert.True(test is Test);
            }
        }
        ```
        > 在单元测试类上通过 `StartupAttribute` 标记。

    * 单元测试类的内嵌 `Startup` 类。
        ```C#
        ///<summary>
        /// 内嵌类测试。
        ///</summary>
        public class NestedStartupTests(ITest test) {

            ///<summary>
            /// 测试。
            ///</summary>
            [Fact]
            public void Test() {
                Assert.True(test is Test);
            }

            ///<summary>
            /// 内嵌类。
            ///</summary>
            public class Startup { 
            
                ///<summary>
                /// 配置依赖注入的服务及生命周期（可选）。
                ///</summary>
                public void ConfigureServices(IServiceCollection services){
                    // 注入需要的参数。
                    services.AddTransient<ITest, Test>();
                }
            }
        }
        ```
        > 在单元测试类中，查找类名为 `Startup` 的类作为启动类。

    * 单元测试类的程序集 `Startup` 类。
        ```C#
        ///<summary>
        /// 程序集类。
        ///</summary>
        public class Startup { 
        
            ///<summary>
            /// 配置依赖注入的服务及生命周期（可选）。
            ///</summary>
            public void ConfigureServices(IServiceCollection services){
                // 注入需要的参数。
                services.AddTransient<ITest, Test>();
            }
        }

        ///<summary>
        /// 程序集类测试。
        ///</summary>
        public class NestedStartupTests(ITest test) {

            ///<summary>
            /// 测试。
            ///</summary>
            [Fact]
            public void Test() {
                Assert.True(test is Test);
            }
        }
        ```
        > 使用单元测试类的命名空间，或命名空间按照 `“.”` 逐步右截断查找名为 `Startup` 的类作为启动类。

    * 默认 `XunitPlus.Startup` 类。
        > 1. 在以上规则都没有设置的情况下生效。
        > 2. 使用 `Inkslab.DI` 实现的自动查找和实现依赖注入。
* 便捷标记：可以 *混合* 使用，权重从上往下依次 **递增** 。
  - 使用 `HeaderAttribute` 添加请求头。
  - 使用 `AuthorizationAttribute` 添加认证请求头。
  - 继承 `HttpContextAttribute` 创建 `HttpContext` 请求上下文。
  - 继承 `UserAttribute` 创建请求上下文的用户信息。
  - 使用 `PatternSeekAttribute` 指定自动依赖注入扫描 `DLL` 文件的范围，如：`Inkslab.*.dll`。