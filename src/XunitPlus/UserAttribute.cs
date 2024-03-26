using System.Security.Claims;

namespace XunitPlus;

/// <summary>
/// 用户。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public abstract class UserAttribute : Attribute
{
    /// <summary>
    /// 创建用户。
    /// </summary>
    /// <returns></returns>
    public abstract ClaimsPrincipal CreateUser();
}