using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Tharga.Team;

namespace Tharga.Team.Service.Tests;

public interface IAsyncScopedTestService
{
    [RequireScope("doc:read")]
    Task<string> ReadAsync();

    [RequireScope("doc:write")]
    Task WriteAsync();
}

public class ScopeProxyPrincipalAccessorTests
{
    private static ClaimsPrincipal Principal(string teamKey, params string[] scopes)
    {
        var claims = new List<Claim>();
        if (teamKey != null) claims.Add(new Claim(TeamClaimTypes.TeamKey, teamKey));
        foreach (var s in scopes) claims.Add(new Claim(TeamClaimTypes.Scope, s));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private sealed class FixedPrincipalAccessor(ClaimsPrincipal principal) : ITeamPrincipalAccessor
    {
        public ValueTask<ClaimsPrincipal> GetCurrentAsync() => new(principal);
    }

    private static IAsyncScopedTestService CreateProxy(ClaimsPrincipal principal)
    {
        var target = Substitute.For<IAsyncScopedTestService>();
        target.ReadAsync().Returns(Task.FromResult("read-ok"));
        target.WriteAsync().Returns(Task.CompletedTask);
        return ScopeProxy<IAsyncScopedTestService>.Create(target, new FixedPrincipalAccessor(principal));
    }

    // The circuit case from #97: no HttpContext, principal comes from the accessor (e.g. AuthenticationStateProvider).

    [Fact]
    public async Task Circuit_With_Required_Scope_Allows_AsyncMethod()
    {
        var proxy = CreateProxy(Principal("team-1", "doc:read"));
        Assert.Equal("read-ok", await proxy.ReadAsync());
    }

    [Fact]
    public async Task Circuit_Without_Required_Scope_Denies_AsyncMethod()
    {
        var proxy = CreateProxy(Principal("team-1", "doc:read")); // has read, not write
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => proxy.WriteAsync());
    }

    [Fact]
    public async Task Circuit_Without_TeamKey_Denies()
    {
        var proxy = CreateProxy(Principal(null, "doc:read"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => proxy.ReadAsync());
    }

    [Fact]
    public async Task HttpContextTeamPrincipalAccessor_Returns_HttpContext_User()
    {
        var principal = Principal("team-1", "doc:read");
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext { User = principal });

        var resolved = await new HttpContextTeamPrincipalAccessor(accessor).GetCurrentAsync();

        Assert.Same(principal, resolved);
    }

    [Fact]
    public async Task HttpContextTeamPrincipalAccessor_Returns_Null_When_No_Context()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext)null);

        Assert.Null(await new HttpContextTeamPrincipalAccessor(accessor).GetCurrentAsync());
    }
}
