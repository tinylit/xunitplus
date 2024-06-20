using System.Reflection;
using System.Runtime.CompilerServices;

namespace XunitPlus;

public class XunitPlusTestFrameworkExecutor : XunitTestFrameworkExecutor
{
    private readonly HostManager hostManager;

    public XunitPlusTestFrameworkExecutor(
        AssemblyName assemblyName,
        ISourceInformationProvider sourceInformationProvider,
        IMessageSink messageSink) : base(assemblyName, sourceInformationProvider, messageSink)
    {
        DisposalTracker.Add(hostManager = new(assemblyName, messageSink));
    }

    protected override async void RunTestCases(IEnumerable<IXunitTestCase> testCases, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
    {
        var results = testCases as List<IXunitTestCase> ?? testCases.ToList();

        var exceptions = new List<Exception>();

        var contexts = new Dictionary<ITestClass, DependencyInjectionContext>();

        var errorTests = new List<ITestClass>();

        foreach (var group in results
                     .GroupBy(tc => tc.TestMethod.TestClass))
        {
            try
            {
                var serviceType = group.Key.Class.ToRuntimeType();

                //? 先触发静态构造函数，然后再进行宿主操作。
                RuntimeHelpers.RunClassConstructor(serviceType.TypeHandle);

                if (serviceType.GetConstructor(Type.EmptyTypes) is null)
                {
                    var context = hostManager.CreateHost(serviceType);

                    contexts.Add(group.Key, context);
                }
            }
            catch (Exception ex)
            {
                errorTests.Add(group.Key);

                exceptions.Add(ex.Unwrap());
            }
        }

        if (errorTests.Count > 0) //? 存在无法注入的测试类。
            results.RemoveAll(x => errorTests.Contains(x.TestMethod.TestClass));

        try
        {
            await hostManager.StartAsync(default);
        }
        catch (Exception ex)
        {
            exceptions.Add(ex);
        }

        using var runner = new XunitPlusTestAssemblyRunner(contexts, TestAssembly,
            results, DiagnosticMessageSink, executionMessageSink, executionOptions, exceptions);
        await runner.RunAsync();

        await hostManager.StopAsync(default);
    }
}