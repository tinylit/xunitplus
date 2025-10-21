using System.Reflection;

namespace XunitPlus;

public class XunitPlusXunitTestFramework : XunitTestFramework
{
    public XunitPlusXunitTestFramework(IMessageSink messageSink) : base(messageSink)
    {
    }

    protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName)
        => new XunitPlusTestFrameworkExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);
}