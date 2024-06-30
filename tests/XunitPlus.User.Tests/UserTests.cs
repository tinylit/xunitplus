using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace XunitPlus.User.Tests;

[AttributeUsage(AttributeTargets.Class)]
public class UserAccountAttribute(long id, string role) : UserAttribute
{
    public override ClaimsPrincipal CreateUser()
    {
        //? 指定账户信息。
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, id.ToString()),
            new(ClaimTypes.Role, role),
            new("timestamp", DateTime.Now.Ticks.ToString())
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "hys"));
    }
}

[UserAccount(1, "User")]
[PatternSeek("XunitPlus.*")]
public class UserTests(IHttpContextAccessor accessor)
{
    [Fact]
    public void Test()
    {
        Debug.WriteLine(accessor!.HttpContext!.User.Identity!.Name);
    }
}