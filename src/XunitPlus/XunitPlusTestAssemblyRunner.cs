namespace XunitPlus;

public class XunitPlusTestAssemblyRunner : XunitTestAssemblyRunner
{
    private readonly IReadOnlyDictionary<ITestClass, DependencyInjectionContext> _contexts;

    public XunitPlusTestAssemblyRunner(
        IReadOnlyDictionary<ITestClass, DependencyInjectionContext> contexts,
        ITestAssembly testAssembly,
        IEnumerable<IXunitTestCase> testCases,
        IMessageSink diagnosticMessageSink,
        IMessageSink executionMessageSink,
        ITestFrameworkExecutionOptions executionOptions,
        IEnumerable<Exception> exceptions) : base(testAssembly, testCases, diagnosticMessageSink,
        executionMessageSink, executionOptions)
    {
        _contexts = contexts;
        
        foreach (var exception in exceptions)
        {
            Aggregator.Add(exception);
        }
    }

    protected override Task<RunSummary> RunTestCollectionAsync(IMessageBus messageBus, ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases, CancellationTokenSource cancellationTokenSource)
        => new XunitPlusTestCollectionRunner(_contexts, testCollection, testCases, DiagnosticMessageSink,
            messageBus, TestCaseOrderer, new(Aggregator), cancellationTokenSource).RunAsync();
}