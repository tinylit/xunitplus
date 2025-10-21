using Xunit;

namespace XunitPlus;

public class XunitPlusTestCollectionRunner : XunitTestCollectionRunner
{
    private readonly IReadOnlyDictionary<ITestClass, DependencyInjectionContext> _contexts;

    public XunitPlusTestCollectionRunner(
        IReadOnlyDictionary<ITestClass, DependencyInjectionContext> contexts,
        ITestCollection testCollection,
        IEnumerable<IXunitTestCase> testCases,
        IMessageSink diagnosticMessageSink,
        IMessageBus messageBus,
        ITestCaseOrderer testCaseOrderer,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource)
        : base(testCollection, testCases, diagnosticMessageSink, messageBus, testCaseOrderer, aggregator, cancellationTokenSource)
    {
        _contexts = contexts;
    }
    protected override Task<RunSummary> RunTestClassAsync(ITestClass testClass, IReflectionTypeInfo @class, IEnumerable<IXunitTestCase> testCases)
    {
        bool serializable = testClass.Class.GetCustomAttributes(typeof(SerializableAttribute)).Any()
                            || testClass.Class.GetCustomAttributes(typeof(CollectionAttribute)).Any();

        if (_contexts.TryGetValue(testClass, out var value))
        {
            return new XunitPlusTestClassRunner(value,
                serializable,
                testClass,
                @class,
                testCases,
                DiagnosticMessageSink,
                MessageBus,
                TestCaseOrderer,
                new(Aggregator),
                CancellationTokenSource,
                CollectionFixtureMappings).RunAsync();
        }

        return base.RunTestClassAsync(testClass, @class, testCases);
    }
}