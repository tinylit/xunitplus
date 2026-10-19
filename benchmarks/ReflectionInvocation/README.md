# 启动反射调用基准

将原 `StartupLoader.Configure` 实现与当前库中的真实调用路径放在同一进程对比。使用无分配的服务提供者隔离 DI 作用域成本；每项预热 20,000 次，采样 7 轮、每轮调用 100,000 次，报告耗时和线程分配量的中位数。

在 PowerShell 中执行：

```powershell
# 关闭分层编译，减少短基准运行期间 JIT 升级对结果的影响。
$env:DOTNET_TieredCompilation = '0'
dotnet run --project benchmarks/ReflectionInvocation/ReflectionInvocation.csproj -c Release -f net8.0
dotnet run --project benchmarks/ReflectionInvocation/ReflectionInvocation.csproj -c Release -f net10.0
Remove-Item Env:DOTNET_TieredCompilation
```

2026-10-19，Windows x64，SDK 10.0.401，Release，无调试器：

| 运行时 | 参数数 | 原实现 ns/次 | 优化后 ns/次 | 原实现 B/次 | 优化后 B/次 |
|--------|-------:|-------------:|-------------:|------------:|------------:|
| .NET 8.0.29 | 0 | 70.1 | 40.6 | 64 | 0 |
| .NET 8.0.29 | 1 | 164.6 | 63.0 | 320 | 32 |
| .NET 8.0.29 | 4 | 221.4 | 107.9 | 368 | 56 |
| .NET 8.0.29 | 9 | 350.9 | 210.3 | 448 | 192 |
| .NET 10.0.12 | 0 | 101.0 | 52.4 | 64 | 0 |
| .NET 10.0.12 | 1 | 207.5 | 82.2 | 320 | 32 |
| .NET 10.0.12 | 4 | 303.0 | 159.9 | 368 | 56 |
| .NET 10.0.12 | 9 | 505.5 | 313.2 | 448 | 192 |

单参数热调用的分配减少 90%。优化使用 C# 12 的集合表达式与 `InlineArray`，以及 .NET 8 的 `MethodInvoker`；.NET 10 使用同一条件编译路径。

这些数字衡量缓存预热后的启动方法调用，包含参数反射与服务解析，不包含 Host 构建、程序集扫描和首次创建反射缓存的成本。实际测试套件的总耗时收益取决于启动类型复用与宿主构建成本；不同运行时的绝对数字也受测量环境影响。
