using System.Security.Claims;
using Tharga.Mcp;
using Tharga.Platform.Mcp;
using Tharga.Team;

namespace Tharga.Platform.Mcp.Tests;

public class PlatformMcpContextTests
{
    [Fact]
    public void NullPrincipal_ReturnsAnonymousContext()
    {
        var ctx = new PlatformMcpContext(null, McpScope.User, "Developer");

        Assert.Null(ctx.UserId);
        Assert.Null(ctx.TeamId);
        Assert.False(ctx.IsDeveloper);
        Assert.Equal(McpScope.User, ctx.Scope);
    }

    [Fact]
    public void ReadsUserIdFromNameIdentifier()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
        }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var ctx = new PlatformMcpContext(principal, McpScope.User, "Developer");

        Assert.Equal("user-123", ctx.UserId);
    }

    [Fact]
    public void FallsBackToSubClaimForUserId()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("sub", "user-from-sub"),
        }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var ctx = new PlatformMcpContext(principal, McpScope.User, "Developer");

        Assert.Equal("user-from-sub", ctx.UserId);
    }

    [Fact]
    public void ReadsTeamIdFromTeamKeyClaim()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(TeamClaimTypes.TeamKey, "team-abc"),
        }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var ctx = new PlatformMcpContext(principal, McpScope.Team, "Developer");

        Assert.Equal("team-abc", ctx.TeamId);
    }

    [Fact]
    public void IsDeveloper_TrueWhenInConfiguredRole()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "Developer"),
        }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var ctx = new PlatformMcpContext(principal, McpScope.System, "Developer");

        Assert.True(ctx.IsDeveloper);
    }

    [Fact]
    public void IsDeveloper_FalseWhenMissingRole()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "User"),
        }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var ctx = new PlatformMcpContext(principal, McpScope.System, "Developer");

        Assert.False(ctx.IsDeveloper);
    }

    [Fact]
    public void IsDeveloper_RespectsCustomRoleName()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "SuperAdmin"),
        }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var ctx = new PlatformMcpContext(principal, McpScope.System, "SuperAdmin");

        Assert.True(ctx.IsDeveloper);
    }
}
