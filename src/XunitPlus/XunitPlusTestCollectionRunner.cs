using Xunit;

namespace XunitPlus;

public class XunitPlusTestCollectionRunner(
    IReadOnlyDictionary<ITestClass, DependencyInjectionContext> contexts,
    ITestCollection testCollection,
    IEnumerable<IXunitTestCase> testCases,
    IMessageSink diagnosticMessageSink,
    IMessageBus messageBus,
    ITestCaseOrderer testCaseOrderer,
    ExceptionAggregator aggregator,
    CancellationTokenSource cancellationTokenSource)
    : XunitTestCollectionRunner(testCollection, testCases, diagnosticMessageSink, messageBus, testCaseOrderer, aggregator, cancellationTokenSource)
{
    protected override Task<RunSummary> RunTestClassAsync(ITestClass testClass, IReflectionTypeInfo @class, IEnumerable<IXunitTestCase> testCases)
    {
        bool serializable = testClass.Class.GetCustomAttributes(typeof(SerializableAttribute)).Any()
                            || testClass.Class.GetCustomAttributes(typeof(CollectionAttribute)).Any();

        if (contexts.TryGetValue(testClass, out var value))
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