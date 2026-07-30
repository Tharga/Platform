using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Tharga.Team.Mcp;
using Tharga.Team;

namespace Tharga.Team.Mcp.Tests;

public class McpScopeCheckerTests
{
    [Fact]
    public void Has_True_WhenScopeClaimPresent()
    {
        var sut = CreateChecker(new Claim(TeamClaimTypes.SystemScope, "mcp:mongodb:read"));

        Assert.True(sut.Has("mcp:mongodb:read"));
    }

    [Fact]
    public void Has_False_WhenScopeMissing()
    {
        var sut = CreateChecker(new Claim(TeamClaimTypes.SystemScope, "other:scope"));

        Assert.False(sut.Has("mcp:mongodb:read"));
    }

    [Fact]
    public void Has_False_WhenNoHttpContext()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext)null);
        var sut = new McpScopeChecker(accessor);

        Assert.False(sut.Has("mcp:mongodb:read"));
    }

    [Fact]
    public void Require_Throws_WhenScopeMissing()
    {
        var sut = CreateChecker();

        var ex = Assert.Throws<UnauthorizedAccessException>(() => sut.Require("mcp:mongodb:admin"));
        Assert.Contains("mcp:mongodb:admin", ex.Message);
    }

    [Fact]
    public void Require_DoesNotThrow_WhenScopePresent()
    {
        var sut = CreateChecker(new Claim(TeamClaimTypes.SystemScope, "mcp:mongodb:admin"));

        sut.Require("mcp:mongodb:admin"); // should not throw
    }

    /// <summary>
    /// The defect this closes: <c>AddTeam</c> registers <c>mcp:discover</c> into the <b>team</b> registry
    /// at <see cref="AccessLevel.Viewer"/>, so holders receive it as a <c>Scope</c> claim — which the
    /// checker did not read. The scope was unsatisfiable through the only route that grants it.
    /// </summary>
    [Fact]
    public void Has_True_WhenTeamScopeHeldForTheSelectedTeam()
    {
        var sut = CreateChecker(
            new Claim(TeamClaimTypes.TeamKey, "team-1"),
            new Claim(TeamClaimTypes.Scope, McpScopes.Discover));

        Assert.True(sut.Has(McpScopes.Discover));
    }

    /// <summary>
    /// A team scope claim only means anything alongside a team context. Claims transformation emits
    /// <c>Scope</c> for the selected team only, so the pairing is the binding — but a principal carrying
    /// the scope with no <c>TeamKey</c> has no team the grant could have been issued for, and must not
    /// authorize. This is what keeps the fix from degenerating into a bare <c>HasClaim</c>.
    /// </summary>
    [Fact]
    public void Has_False_WhenTeamScopeHeldButNoTeamSelected()
    {
        var sut = CreateChecker(new Claim(TeamClaimTypes.Scope, McpScopes.Discover));

        Assert.False(sut.Has(McpScopes.Discover));
    }

    /// <summary>A team context without the scope is still a denial — the pairing is required, not either half.</summary>
    [Fact]
    public void Has_False_WhenTeamSelectedButScopeNotHeld()
    {
        var sut = CreateChecker(
            new Claim(TeamClaimTypes.TeamKey, "team-1"),
            new Claim(TeamClaimTypes.Scope, "some:other"));

        Assert.False(sut.Has(McpScopes.Discover));
    }

    /// <summary>A system grant is team-independent by design, so it authorizes with no team selected.</summary>
    [Fact]
    public void Has_True_WhenSystemScopeHeldAndNoTeamSelected()
    {
        var sut = CreateChecker(new Claim(TeamClaimTypes.SystemScope, McpScopes.Discover));

        Assert.True(sut.Has(McpScopes.Discover));
    }

    private static McpScopeChecker CreateChecker(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        return new McpScopeChecker(accessor);
    }
}
