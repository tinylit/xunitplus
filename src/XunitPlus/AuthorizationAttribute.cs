namespace XunitPlus;

/// <summary>
/// 认证头。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class AuthorizationAttribute(string token) : HeaderAttribute("Authorization", token);