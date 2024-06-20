using System.Reflection;

namespace XunitPlus;

public static class ExceptionExtensions
{
    public static Exception Unwrap(this Exception ex)
    {
        while (ex is TargetInvocationException { InnerException: not null } tie) ex = tie.InnerException!;

        while (ex is AggregateException { InnerException: not null } ae) ex = ae.InnerException!;

        return ex;
    }
}