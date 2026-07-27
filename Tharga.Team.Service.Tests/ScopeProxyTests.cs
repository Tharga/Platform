using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Tharga.Team;

namespace Tharga.Team.Service.Tests;

/// <summary>A team service: every operation names the team it acts on, as its first argument.</summary>
public interface IScopedTestService
{
    [RequireScope("doc:read")]
    string ReadMethod(string teamKey);

    [RequireScope("doc:download")]
    string DownloadMethod(string teamKey);

    [RequireScope("doc:delete")]
    string DeleteMethod(string teamKey);

    string UnprotectedMethod(string teamKey);
}

/// <summary>A system service: no operation acts on a team.</summary>
public interface ISystemScopedTestService
{
    [RequireScope("system:read")]
    string ReadMethod();
}

public class ScopeProxyTests
{
    private static IHttpContextAccessor CreateAccessor(string teamKey, params string[] scopes)
        => BuildAccessor(teamKey, scopes, []);

    /// <summary>
    /// A system grant carries a different claim type, so a fixture must say which kind it is handing out —
    /// a team-level scope does not satisfy a system service and vice versa.
    /// </summary>
    private static IHttpContextAccessor CreateSystemAccessor(string teamKey, params string[] systemScopes)
        => BuildAccessor(teamKey, [], systemScopes);

    private static IHttpContextAccessor BuildAccessor(string teamKey, string[] scopes, string[] systemScopes)
    {
        var claims = new List<Claim>();
        if (teamKey != null) claims.Add(new Claim(TeamClaimTypes.TeamKey, teamKey));
        foreach (var scope in scopes)
            claims.Add(new Claim(TeamClaimTypes.Scope, scope));
        foreach (var scope in systemScopes)
            claims.Add(new Claim(TeamClaimTypes.SystemScope, scope));

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var context = new DefaultHttpContext { User = principal };

        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(context);
        return accessor;
    }

    private static IScopedTestService CreateProxy(string teamKey, params string[] scopes)
    {
        var target = Substitute.For<IScopedTestService>();
        target.ReadMethod(Arg.Any<string>()).Returns("read-ok");
        target.DownloadMethod(Arg.Any<string>()).Returns("download-ok");
        target.DeleteMethod(Arg.Any<string>()).Returns("delete-ok");
        target.UnprotectedMethod(Arg.Any<string>()).Returns("unprotected-ok");

        var accessor = CreateAccessor(teamKey, scopes);
        return ScopeProxy<IScopedTestService>.Create(target, accessor, ServiceScopeKind.Team);
    }

    private static ISystemScopedTestService CreateSystemProxy(string teamKey, params string[] systemScopes)
    {
        var target = Substitute.For<ISystemScopedTestService>();
        target.ReadMethod().Returns("read-ok");

        var accessor = CreateSystemAccessor(teamKey, systemScopes);
        return ScopeProxy<ISystemScopedTestService>.Create(target, accessor, ServiceScopeKind.System);
    }

    [Fact]
    public void With_Required_Scope_Succeeds()
    {
        var proxy = CreateProxy("team-1", "doc:read");
        Assert.Equal("read-ok", proxy.ReadMethod("team-1"));
    }

    [Fact]
    public void With_Multiple_Scopes_Succeeds()
    {
        var proxy = CreateProxy("team-1", "doc:read", "doc:download", "doc:delete");
        Assert.Equal("read-ok", proxy.ReadMethod("team-1"));
        Assert.Equal("download-ok", proxy.DownloadMethod("team-1"));
        Assert.Equal("delete-ok", proxy.DeleteMethod("team-1"));
    }

    [Fact]
    public void Without_Required_Scope_Throws()
    {
        var proxy = CreateProxy("team-1", "doc:read");
        Assert.Throws<UnauthorizedAccessException>(() => proxy.DownloadMethod("team-1"));
    }

    [Fact]
    public void Without_Any_Scopes_Throws()
    {
        var proxy = CreateProxy("team-1");
        Assert.Throws<UnauthorizedAccessException>(() => proxy.ReadMethod("team-1"));
    }

    [Fact]
    public void Without_TeamKey_Throws()
    {
        var proxy = CreateProxy(null, "doc:read");
        Assert.Throws<UnauthorizedAccessException>(() => proxy.ReadMethod("team-1"));
    }

    [Fact]
    public void Missing_Attribute_Throws_InvalidOperation()
    {
        var proxy = CreateProxy("team-1", "doc:read");
        Assert.Throws<InvalidOperationException>(() => proxy.UnprotectedMethod("team-1"));
    }

    // ---- The defect this feature exists to close ----

    [Fact]
    public void Scope_Held_For_One_Team_Does_Not_Authorize_Another()
    {
        var proxy = CreateProxy("team-a", "doc:delete");

        Assert.Throws<UnauthorizedAccessException>(() => proxy.DeleteMethod("team-b"));
    }

    [Fact]
    public void Scope_Held_For_One_Team_Still_Authorizes_That_Team()
    {
        var proxy = CreateProxy("team-a", "doc:delete");

        Assert.Equal("delete-ok", proxy.DeleteMethod("team-a"));
    }

    [Fact]
    public void TeamService_Call_Naming_No_Team_Throws()
    {
        var proxy = CreateProxy("team-a", "doc:delete");

        Assert.Throws<UnauthorizedAccessException>(() => proxy.DeleteMethod(null));
    }

    // ---- System services are not team-bound ----

    [Fact]
    public void SystemService_Succeeds_With_No_Team_Selected()
    {
        var proxy = CreateSystemProxy(null, "system:read");

        Assert.Equal("read-ok", proxy.ReadMethod());
    }

    [Fact]
    public void SystemService_Without_Scope_Throws()
    {
        var proxy = CreateSystemProxy(null);

        Assert.Throws<UnauthorizedAccessException>(() => proxy.ReadMethod());
    }
}
