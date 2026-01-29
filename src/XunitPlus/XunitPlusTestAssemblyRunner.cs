namespace XunitPlus;

public class XunitPlusTestAssemblyRunner : XunitTestAssemblyRunner
{
    private readonly IReadOnlyDictionary<Type, DependencyInjectionContext> _contexts;

    public XunitPlusTestAssemblyRunner(
        IReadOnlyDictionary<Type, DependencyInjectionContext> contexts,
        IReadOnlyDictionary<Guid, Type> uniqueTypes,
        ITestAssembly testAssembly,
        IEnumerable<IXunitTestCase> testCases,
        IMessageSink diagnosticMessageSink,
        IMessageSink executionMessageSink,
        ITestFrameworkExecutionOptions executionOptions,
        IEnumerable<Exception> exceptions) : base(testAssembly, testCases, diagnosticMessageSink,
        executionMessageSink, executionOptions)
    {
        _contexts = contexts;

        TestCaseOrderer = new StepOrderer();

        TestCollectionOrderer = new StepCollectionOrderer(uniqueTypes);

        foreach (var exception in exceptions)
        {
            Aggregator.Add(exception);
        }
    }

    protected override Task<RunSummary> RunTestCollectionAsync(IMessageBus messageBus, ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases,
        CancellationTokenSource cancellationTokenSource)
        => new XunitPlusTestCollectionRunner(_contexts, testCollection, testCases, DiagnosticMessageSink,
            messageBus, TestCaseOrderer, new(Aggregator), cancellationTokenSource).RunAsync();
}