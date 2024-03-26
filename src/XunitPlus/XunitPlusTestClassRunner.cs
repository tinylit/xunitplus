using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace XunitPlus;

public class XunitPlusTestClassRunner(
    DependencyInjectionContext context,
    bool serializable,
    ITestClass testClass,
    IReflectionTypeInfo @class,
    IEnumerable<IXunitTestCase> testCases,
    IMessageSink diagnosticMessageSink,
    IMessageBus messageBus,
    ITestCaseOrderer testCaseOrderer,
    ExceptionAggregator aggregator,
    CancellationTokenSource cancellationTokenSource,
    IDictionary<Type, object> collectionFixtureMappings)
    : XunitTestClassRunner(testClass, @class, testCases, diagnosticMessageSink,
        messageBus, testCaseOrderer, aggregator, cancellationTokenSource, collectionFixtureMappings)
{
    private readonly AsyncServiceScope serviceScope = context.Services.CreateAsyncScope();

    private IDictionary<Type, object> CollectionFixtureMappings { get; } = collectionFixtureMappings;

    /// <inheritdoc />
    protected override object?[] CreateTestClassConstructorArguments()
    {
        if ((!Class.Type.IsAbstract ? 0 : Class.Type.IsSealed ? 1 : 0) != 0)
            return Array.Empty<object?>();

        var constructor = SelectTestClassConstructor();

        if (constructor is null)
            return Array.Empty<object?>();

        var parameters = constructor.GetParameters();

        var objArray = new object?[parameters.Length];

        for (var index = 0; index < parameters.Length; ++index)
        {
            var parameterInfo = parameters[index];

            if (TryGetConstructorArgument(constructor, index, parameterInfo, out var argumentValue))
            {
                objArray[index] = argumentValue;
            }
            else
            {
                Aggregator.Add(new TestClassException($"Parameter {parameterInfo.Name} of type {Class.Type.Name} is not supported!"));
            }
        }

        return objArray;
    }

    /// <inheritdoc />
    protected override bool TryGetConstructorArgument(ConstructorInfo constructor, int index, ParameterInfo parameter,
        out object? argumentValue)
    {
        if (parameter.HasDefaultValue)
        {
            argumentValue = parameter.DefaultValue;

            return true;
        }

        if (parameter.IsOptional)
        {
            argumentValue = parameter.ParameterType.IsValueType
                ? Activator.CreateInstance(parameter.ParameterType)
                : null;

            return true;
        }

        if (parameter.ParameterType == typeof(CancellationToken))
        {
            argumentValue = CancellationTokenSource.Token;

            return true;
        }

        argumentValue = serviceScope.ServiceProvider.GetService(parameter.ParameterType);

        return argumentValue != null;
    }

    /// <inheritdoc />
    protected override void CreateClassFixture(Type fixtureType)
    {
        var ctors = fixtureType.GetTypeInfo()
            .DeclaredConstructors
            .Where(ci => !ci.IsStatic && ci.IsPublic)
            .ToList();

        if (ctors.Count != 1)
        {
            Aggregator.Add(new TestClassException($"Class fixture type '{fixtureType.FullName}' may only define a single public constructor."));

            return;
        }

        var missingParameters = new List<ParameterInfo>();
        var ctorArgs = ctors[0].GetParameters().Select(p =>
        {
            if (CollectionFixtureMappings.TryGetValue(p.ParameterType, out var arg)) return arg;

            arg = serviceScope.ServiceProvider.GetService(p.ParameterType);

            if (arg is null) missingParameters.Add(p);

            return arg;
        }).ToArray();

        if (missingParameters.Count > 0)
            Aggregator.Add(new TestClassException(
                $"Class fixture type '{fixtureType.FullName}' had one or more unresolved constructor arguments: {string.Join(", ", missingParameters.Select(p => $"{p.ParameterType.Name} {p.Name}"))}"));
        else Aggregator.Run(() => ClassFixtureMappings[fixtureType] = ctors[0].Invoke(ctorArgs));
    }

    /// <inheritdoc />
    protected override async Task BeforeTestClassFinishedAsync()
    {
        await base.BeforeTestClassFinishedAsync();

        foreach (var fixture in CollectionFixtureMappings.Values)
        {
            if (fixture is IAsyncDisposable asyncDisposable)
            {
                await Aggregator.RunAsync(() => asyncDisposable.DisposeAsync().AsTask());
            }
            else if (fixture is IDisposable disposable)
            {
                Aggregator.Run(disposable.Dispose);
            }
        }

        await serviceScope.DisposeAsync();
    }

    // This method has been slightly modified from the original implementation to run tests in parallel
    protected override async Task<RunSummary> RunTestMethodsAsync()
    {
        IEnumerable<IXunitTestCase> orderedTestCases;
        try
        {
            orderedTestCases = TestCaseOrderer.OrderTestCases(TestCases);
        }
        catch (Exception ex)
        {
            while (ex is TargetInvocationException { InnerException: not null } tie) ex = tie.InnerException;

            while (ex is AggregateException { InnerException: not null } ae) ex = ae.InnerException;

            DiagnosticMessageSink.OnMessage(new DiagnosticMessage(
                $"Test case orderer '{TestCaseOrderer.GetType().FullName}' throw '{ex.GetType().FullName}' during ordering: {ex.Message}{Environment.NewLine}{ex.StackTrace}"));

            orderedTestCases = TestCases;
        }

        var constructorArguments = CreateTestClassConstructorArguments();

        var methodTasks = orderedTestCases.GroupBy(tc => tc.TestMethod, TestMethodComparer.Instance)
            .Select(m => RunTestMethodAsync(m.Key, (IReflectionMethodInfo)m.Key.Method, m, constructorArguments));

        var summary = new RunSummary();

        foreach (var methodSummary in await Task.WhenAll(methodTasks))
            summary.Aggregate(methodSummary);

        return summary;
    }

    /// <inheritdoc />
    protected override Task<RunSummary> RunTestMethodAsync(ITestMethod testMethod,
        IReflectionMethodInfo method, IEnumerable<IXunitTestCase> testCases, object?[] constructorArguments)
    {
        if (serializable)
        {
            return base.RunTestMethodAsync(testMethod, method, testCases, constructorArguments);
        }

        return new XunitPlusTestMethodRunner(testMethod,
            Class,
            method,
            testCases,
            DiagnosticMessageSink,
            MessageBus,
            new(Aggregator),
            CancellationTokenSource,
            constructorArguments).RunAsync();
    }
}