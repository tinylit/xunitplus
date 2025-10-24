using Inkslab;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

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

        var xunitTestCases = new List<IXunitTestCase>();

        var contexts = new Dictionary<ITestClass, DependencyInjectionContext>();

        var testRuntimeCases = testCases
            .GroupBy(tc => tc.TestMethod.TestClass)
            .ToList();

        var testOutputHelperType = typeof(ITestOutputHelper);

        foreach (var runtimeGroup in testRuntimeCases)
        {
            try
            {
                var serviceType = runtimeGroup.Key.Class.ToRuntimeType();

                bool isCollectionFixture = serviceType.IsDefined(typeof(CollectionAttribute));

                var constructorArguments = serviceType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

                var xuintDefault = isCollectionFixture || constructorArguments.Length == 0 || constructorArguments.Any(x =>
                {
                    var parameters = x.GetParameters();

                    if (parameters.Length == 0)
                    {
                        return true;
                    }

                    return parameters.All(p => p.HasDefaultValue || p.ParameterType == testOutputHelperType);
                });

                //? 先触发静态构造函数，然后再进行宿主操作。
                RuntimeHelpers.RunClassConstructor(serviceType.TypeHandle);

                xunitTestCases.AddRange(runtimeGroup);

                if (xuintDefault)
                {
                    continue;
                }

                var context = _hostManager.CreateHost(serviceType);

                contexts.Add(runtimeGroup.Key, context);
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
            xunitTestCases, DiagnosticMessageSink, executionMessageSink, executionOptions, exceptions);

        await runner.RunAsync();

        await _hostManager.StopAsync(default);
    }
}