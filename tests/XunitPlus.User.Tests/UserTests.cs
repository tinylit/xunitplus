using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

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

[TestPriority(1)]
[UserAccount(1, "User")]
[PatternSeek("XunitPlus.*")]
public class UserTests
{
    private readonly IHttpContextAccessor _accessor;
    private readonly ITestOutputHelper _output;
    private readonly ILogger<UserTests> _logger;

    public UserTests(IHttpContextAccessor accessor, ITestOutputHelper output, ILogger<UserTests> logger)
    {
        _accessor = accessor;
        _output = output;
        _logger = logger;
    }

    [Fact]
    public void Test()
    {
        _output.WriteLine("Test-1");
        Debug.WriteLine(_accessor!.HttpContext!.User.Identity!.Name);
    }

    [Fact]
    public void Test2()
    {
        _output.WriteLine("Test2-1");
        _logger.LogInformation("Test2-1-LogInformation");
        Debug.WriteLine(_accessor!.HttpContext!.User.Identity!.IsAuthenticated);
    }
}

[UserAccount(1, "User")]
[PatternSeek("XunitPlus.*")]
public class UserTests2
{
    private readonly IHttpContextAccessor _accessor;
    private readonly ITestOutputHelper _output;

    public UserTests2(IHttpContextAccessor accessor, ITestOutputHelper output)
    {
        _accessor = accessor;
        _output = output;
    }

    [Fact]
    public void Test()
    {
        _output.WriteLine("2Test-1");
        Debug.WriteLine(_accessor!.HttpContext!.User.Identity!.Name);
    }

    [Fact]
    public void Test2()
    {
        _output.WriteLine("2Test2-1");
        Debug.WriteLine(_accessor!.HttpContext!.User.Identity!.IsAuthenticated);
    }
}