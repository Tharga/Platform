using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Tharga.Platform.Mcp;
using Tharga.Team;

namespace Tharga.Platform.Mcp.Tests;

public class McpScopeCheckerTests
{
    [Fact]
    public void Has_True_WhenScopeClaimPresent()
    {
        var sut = CreateChecker(new Claim(TeamClaimTypes.Scope, "mcp:mongodb:read"));

        Assert.True(sut.Has("mcp:mongodb:read"));
    }

    [Fact]
    public void Has_False_WhenScopeMissing()
    {
        var sut = CreateChecker(new Claim(TeamClaimTypes.Scope, "other:scope"));

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
        var sut = CreateChecker(new Claim(TeamClaimTypes.Scope, "mcp:mongodb:admin"));

        sut.Require("mcp:mongodb:admin"); // should not throw
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
