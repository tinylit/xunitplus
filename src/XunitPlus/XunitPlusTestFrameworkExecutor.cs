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

        if (patternSeeks.Length == 0)
        {
            using var startup = new XStartup("Inkslab.*.dll");
            
            startup.DoStartup();
        }
        else
        {
            var patterns = patternSeeks
                .Cast<PatternSeekAttribute>()
                .Select(x => x.Pattern)
                .ToArray();

            using var startup = new XStartup(patterns);
            
            startup.DoStartup();
        }

        var exceptions = new List<Exception>();

        var xunitTestCases = new List<IXunitTestCase>();

        var contexts = new Dictionary<ITestClass, DependencyInjectionContext>();

        var testRuntimeCases = testCases
            .GroupBy(tc => tc.TestMethod.TestClass)
            .ToList();

        var uniqueTypes = new Dictionary<Guid, Type>();

        foreach (var runtimeGroup in testRuntimeCases)
        {
            try
            {
                var testClassType = runtimeGroup.Key;
                
                var serviceType = testClassType.Class.ToRuntimeType();

                //? 先触发静态构造函数，然后再进行宿主操作。
                RuntimeHelpers.RunClassConstructor(serviceType.TypeHandle);

                xunitTestCases.AddRange(runtimeGroup);

                var context = _hostManager.CreateHost(serviceType);

                contexts.Add(testClassType, context);
                
                uniqueTypes.Add(testClassType.TestCollection.UniqueID, serviceType);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex.Unwrap());
            }
        }

        try
        {
            await _hostManager.StartAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            exceptions.Add(ex);
        }

        using var runner = new XunitPlusTestAssemblyRunner(contexts, uniqueTypes, TestAssembly,
            xunitTestCases, DiagnosticMessageSink, executionMessageSink, executionOptions, exceptions);

        await runner.RunAsync();

        await _hostManager.StopAsync(CancellationToken.None);
    }
}