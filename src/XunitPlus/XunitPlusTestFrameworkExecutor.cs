using Inkslab;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace XunitPlus;

public class XunitPlusTestFrameworkExecutor : XunitTestFrameworkExecutor
{
    private class XunitTestCaseComparer : IEqualityComparer<ITestClass>
    {
        public bool Equals(ITestClass? x, ITestClass? y)
        {
            if (x is null && y is null)
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return x.TestCollection.UniqueID == y.TestCollection.UniqueID;
        }

        public int GetHashCode([DisallowNull] ITestClass obj) => obj.TestCollection.UniqueID.GetHashCode();


        public static readonly XunitTestCaseComparer Instance = new();
    }

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

        var xunitTestCases = new List<IXunitTestCase>(testCases);

        var contexts = new Dictionary<Type, DependencyInjectionContext>();

        var uniqueTypes = xunitTestCases.GroupBy(tc => tc.TestMethod.TestClass, XunitTestCaseComparer.Instance).ToDictionary(g => g.Key.TestCollection.UniqueID, g => g.Key.Class.ToRuntimeType());

        foreach (var serviceType in xunitTestCases
            .Select(tc => tc.TestMethod.TestClass.Class.ToRuntimeType())
            .Distinct()
        )
        {
            try
            {
                //? 先触发静态构造函数，然后再进行宿主操作。
                RuntimeHelpers.RunClassConstructor(serviceType.TypeHandle);

                var context = _hostManager.CreateHost(serviceType);

                contexts.Add(serviceType, context);
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