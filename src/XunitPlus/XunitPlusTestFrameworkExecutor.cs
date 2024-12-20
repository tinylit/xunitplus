using Inkslab;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace XunitPlus;

public class XunitPlusTestFrameworkExecutor : XunitTestFrameworkExecutor
{
    private readonly Assembly _assembly;
    private readonly HostManager _hostManager;

    public XunitPlusTestFrameworkExecutor(
        AssemblyName assemblyName,
        ISourceInformationProvider sourceInformationProvider,
        IMessageSink messageSink) : base(assemblyName, sourceInformationProvider, messageSink)
    {
        _assembly = Assembly.Load(assemblyName);

        DisposalTracker.Add(_hostManager = new(assemblyName, messageSink));
    }

    protected override async void RunTestCases(IEnumerable<IXunitTestCase> testCases, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
    {
        var patternSeeks = _assembly.GetCustomAttributes(typeof(PatternSeekAttribute), true);

        if (patternSeeks is null || patternSeeks.Length == 0)
        {
            using (var startup = new XStartup("Inkslab.*.dll"))
            {
                startup.DoStartup();
            }
        }
        else
        {
            var pattarns = patternSeeks
                .Cast<PatternSeekAttribute>()
                .Select(x => x.Pattern)
                .ToArray();

            using (var startup = new XStartup(pattarns))
            {
                startup.DoStartup();
            }
        }

        var exceptions = new List<Exception>();

        var results = new List<IXunitTestCase>();

        var contexts = new Dictionary<ITestClass, DependencyInjectionContext>();

        foreach (var group in testCases
                     .GroupBy(tc => tc.TestMethod.TestClass))
        {
            try
            {
                var serviceType = group.Key.Class.ToRuntimeType();

                //? 先触发静态构造函数，然后再进行宿主操作。
                RuntimeHelpers.RunClassConstructor(serviceType.TypeHandle);

                if (serviceType.GetConstructor(Type.EmptyTypes) is null)
                {
                    var context = _hostManager.CreateHost(serviceType);

                    contexts.Add(group.Key, context);
                }

                results.AddRange(group);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex.Unwrap());
            }
        }

        try
        {
            await _hostManager.StartAsync(default);
        }
        catch (Exception ex)
        {
            exceptions.Add(ex);
        }

        using var runner = new XunitPlusTestAssemblyRunner(contexts, TestAssembly,
            results, DiagnosticMessageSink, executionMessageSink, executionOptions, exceptions);

        await runner.RunAsync();

        await _hostManager.StopAsync(default);
    }
}