using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Tharga.Platform.Mcp;
using Tharga.Team;

namespace Tharga.Platform.Mcp.Tests;

public class HttpContextMcpContextAccessorTests
{
    [Fact]
    public void Current_Null_WhenNoHttpContext()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext)null);
        var sut = new HttpContextMcpContextAccessor(accessor, Options.Create(new McpPlatformOptions()));

        Assert.Null(sut.Current);
    }

    [Fact]
    public void Current_ExposesClaims()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-1"),
            new Claim(TeamClaimTypes.TeamKey, "team-1"),
            new Claim(ClaimTypes.Role, "Developer"),
        }, "TestAuth");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var sut = new HttpContextMcpContextAccessor(accessor, Options.Create(new McpPlatformOptions()));

        var ctx = sut.Current;

        Assert.NotNull(ctx);
        Assert.Equal("user-1", ctx.UserId);
        Assert.Equal("team-1", ctx.TeamId);
        Assert.True(ctx.IsDeveloper);
    }

    [Fact]
    public void Setter_IsNoOp()
    {
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity("TestAuth")) };
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var sut = new HttpContextMcpContextAccessor(accessor, Options.Create(new McpPlatformOptions()));

        // Setter should not throw even though it's a no-op
        sut.Current = null;
    }
}
