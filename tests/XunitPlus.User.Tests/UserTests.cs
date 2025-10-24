using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace XunitPlus.User.Tests;

[AttributeUsage(AttributeTargets.Class)]
public class UserAccountAttribute : UserAttribute
{
    private readonly long _id;
    private readonly string _role;

    public UserAccountAttribute(long id, string role)
    {
        _id = id;
        _role = role;
    }

    public override ClaimsPrincipal CreateUser()
    {
        //? 指定账户信息。
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, _id.ToString()),
            new(ClaimTypes.Role, _role),
            new("timestamp", DateTime.Now.Ticks.ToString())
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "hys"));
    }
}

[UserAccount(1, "User")]
[PatternSeek("XunitPlus.*")]
public class UserTests
{
    private readonly IHttpContextAccessor _accessor;

    public UserTests(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    [Fact]
    public void Test()
    {
        Debug.WriteLine(_accessor!.HttpContext!.User.Identity!.Name);
    }

    [Fact]
    public void Test2()
    {
        Debug.WriteLine(_accessor!.HttpContext!.User.Identity!.IsAuthenticated);
    }
}