using Microsoft.AspNetCore.Http;

namespace XunitPlus;

/// <summary>
/// 请求上下文头部。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public abstract class HttpContextAttribute : Attribute
{
    /// <summary>
    /// 请求上下文。
    /// </summary>
    /// <returns></returns>
    public abstract HttpContext CreateContext();
}