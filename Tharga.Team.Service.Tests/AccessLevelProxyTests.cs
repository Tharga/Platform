using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Tharga.Team;

namespace Tharga.Team.Service.Tests;

public interface ITestService
{
    [RequireAccessLevel(AccessLevel.Viewer)]
    string ViewerMethod();

    [RequireAccessLevel(AccessLevel.User)]
    string UserMethod();

    [RequireAccessLevel(AccessLevel.Administrator)]
    string AdminMethod();

    string UnprotectedMethod();
}

public class AccessLevelProxyTests
{
    private static IHttpContextAccessor CreateAccessor(string teamKey, string accessLevel)
    {
        var claims = new List<Claim>();
        if (teamKey != null) claims.Add(new Claim(TeamClaimTypes.TeamKey, teamKey));
        if (accessLevel != null) claims.Add(new Claim(TeamClaimTypes.AccessLevel, accessLevel));

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var context = new DefaultHttpContext { User = principal };

        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(context);
        return accessor;
    }

    private static ITestService CreateProxy(string teamKey, string accessLevel)
    {
        var target = Substitute.For<ITestService>();
        target.ViewerMethod().Returns("viewer-ok");
        target.UserMethod().Returns("user-ok");
        target.AdminMethod().Returns("admin-ok");
        target.UnprotectedMethod().Returns("unprotected-ok");

        var accessor = CreateAccessor(teamKey, accessLevel);
        return AccessLevelProxy<ITestService>.Create(target, accessor);
    }

    [Fact]
    public void Administrator_Can_Call_Viewer_Method()
    {
        var proxy = CreateProxy("team-1", "Administrator");
        Assert.Equal("viewer-ok", proxy.ViewerMethod());
    }

    [Fact]
    public void Administrator_Can_Call_User_Method()
    {
        var proxy = CreateProxy("team-1", "Administrator");
        Assert.Equal("user-ok", proxy.UserMethod());
    }

    [Fact]
    public void Administrator_Can_Call_Admin_Method()
    {
        var proxy = CreateProxy("team-1", "Administrator");
        Assert.Equal("admin-ok", proxy.AdminMethod());
    }

    [Fact]
    public void Viewer_Can_Call_Viewer_Method()
    {
        var proxy = CreateProxy("team-1", "Viewer");
        Assert.Equal("viewer-ok", proxy.ViewerMethod());
    }

    [Fact]
    public void Viewer_Cannot_Call_User_Method()
    {
        var proxy = CreateProxy("team-1", "Viewer");
        Assert.Throws<UnauthorizedAccessException>(() => proxy.UserMethod());
    }

    [Fact]
    public void Viewer_Cannot_Call_Admin_Method()
    {
        var proxy = CreateProxy("team-1", "Viewer");
        Assert.Throws<UnauthorizedAccessException>(() => proxy.AdminMethod());
    }

    [Fact]
    public void User_Can_Call_User_Method()
    {
        var proxy = CreateProxy("team-1", "User");
        Assert.Equal("user-ok", proxy.UserMethod());
    }

    [Fact]
    public void User_Cannot_Call_Admin_Method()
    {
        var proxy = CreateProxy("team-1", "User");
        Assert.Throws<UnauthorizedAccessException>(() => proxy.AdminMethod());
    }

    [Fact]
    public void Owner_Can_Call_Admin_Method()
    {
        var proxy = CreateProxy("team-1", "Owner");
        Assert.Equal("admin-ok", proxy.AdminMethod());
    }

    [Fact]
    public void No_Team_Throws()
    {
        var proxy = CreateProxy(null, "Administrator");
        Assert.Throws<UnauthorizedAccessException>(() => proxy.ViewerMethod());
    }

    [Fact]
    public void No_AccessLevel_Throws()
    {
        var proxy = CreateProxy("team-1", null);
        Assert.Throws<UnauthorizedAccessException>(() => proxy.ViewerMethod());
    }

    [Fact]
    public void Missing_Attribute_Throws_InvalidOperation()
    {
        var proxy = CreateProxy("team-1", "Administrator");
        Assert.Throws<InvalidOperationException>(() => proxy.UnprotectedMethod());
    }

    [Fact]
    public void Custom_Cannot_Call_Viewer_Method()
    {
        // Custom is the lowest tier, so it fails even the least-restrictive access-level gate.
        var proxy = CreateProxy("team-1", "Custom");
        Assert.Throws<UnauthorizedAccessException>(() => proxy.ViewerMethod());
    }

    [Fact]
    public void Custom_Cannot_Call_Admin_Method()
    {
        var proxy = CreateProxy("team-1", "Custom");
        Assert.Throws<UnauthorizedAccessException>(() => proxy.AdminMethod());
    }
}
