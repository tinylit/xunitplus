namespace XunitPlus;

/// <summary>
/// 认证头。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class AuthorizationAttribute : HeaderAttribute
{
    /// <summary>
    /// 构造函数。
    /// </summary>
    /// <param name="token">认证令牌。</param>
    public AuthorizationAttribute(string token) : base("Authorization", token)
    {
    }
}