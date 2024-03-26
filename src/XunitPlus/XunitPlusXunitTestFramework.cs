using System.Reflection;

namespace XunitPlus;

public class XunitPlusXunitTestFramework(IMessageSink messageSink) : XunitTestFramework(messageSink)
{
    protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName)
        => new XunitPlusTestFrameworkExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);
}