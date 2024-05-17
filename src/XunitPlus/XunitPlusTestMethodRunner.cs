namespace XunitPlus;

public class XunitPlusTestMethodRunner(
    ITestMethod testMethod,
    IReflectionTypeInfo @class,
    IReflectionMethodInfo method,
    IEnumerable<IXunitTestCase> testCases,
    IMessageSink diagnosticMessageSink,
    IMessageBus messageBus,
    ExceptionAggregator aggregator,
    CancellationTokenSource cancellationTokenSource,
    object?[] constructorArguments)
    : XunitTestMethodRunner(testMethod, @class, method, testCases, diagnosticMessageSink, messageBus, aggregator, cancellationTokenSource, constructorArguments)
{
    // This method has been slightly modified from the original implementation to run tests in parallel
    protected override async Task<RunSummary> RunTestCasesAsync()
    {
        // Respect MaxParallelThreads by using the MaxConcurrencySyncContext if it exists, mimicking how collections are run
        var scheduler = SynchronizationContext.Current == null
            ? TaskScheduler.Default
            : TaskScheduler.FromCurrentSynchronizationContext();

        var tasks = TestCases.Select(testCase => Task.Factory.StartNew(
            state => RunTestCaseAsync((IXunitTestCase)state!),
            testCase,
            CancellationTokenSource.Token,
            TaskCreationOptions.DenyChildAttach | TaskCreationOptions.HideScheduler, scheduler).Unwrap());

        var summary = new RunSummary();

        foreach (var caseSummary in await Task.WhenAll(tasks))
            summary.Aggregate(caseSummary);

        return summary;
    }
}