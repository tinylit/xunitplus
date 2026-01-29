using System.Reflection;
using Xunit;

namespace XunitPlus;

public class XunitPlusTestCollectionRunner : XunitTestCollectionRunner
{
    private readonly IReadOnlyDictionary<Type, DependencyInjectionContext> _contexts;

    public XunitPlusTestCollectionRunner(
        IReadOnlyDictionary<Type, DependencyInjectionContext> contexts,
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
        var serviceType = @class.ToRuntimeType();

        if (_contexts.TryGetValue(serviceType, out var context))
        {
            bool serializable = serviceType.IsDefined(typeof(CollectionAttribute))
                || serviceType.IsDefined(typeof(SerializableAttribute));

            return new XunitPlusTestClassRunner(context,
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