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

    /// <summary>
    /// 重写此方法以强制禁用测试集合的并行执行。
    /// 虽然基类代码看起来是 foreach + await 顺序执行，但 xUnit 内部会根据配置和调度机制
    /// 来并行执行测试集合。通过显式重写并使用 await，确保严格按 TestPriorityAttribute 顺序依次执行。
    /// </summary>
    protected override async Task<RunSummary> RunTestCollectionsAsync(IMessageBus messageBus, CancellationTokenSource cancellationTokenSource)
    {
        var summary = new RunSummary();
        var orderedTestCollections = OrderTestCollections();

        // 严格顺序执行每个测试集合
        foreach (var collection in orderedTestCollections)
        {
            summary.Aggregate(await RunTestCollectionAsync(messageBus, collection.Item1, collection.Item2, cancellationTokenSource));
            
            if (cancellationTokenSource.IsCancellationRequested)
            {
                break;
            }
        }

        return summary;
    }
}