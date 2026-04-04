using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using Tharga.Team;
using Tharga.Team.Blazor.Framework;

namespace Tharga.Team.Blazor.Tests;

public class TeamServerClaimsTransformationTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    private readonly Mock<ITeamService> _teamService = new();
    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<IScopeRegistry> _scopeRegistry = new();
    private readonly Mock<IOptions<ThargaBlazorOptions>> _options = new();

    public TeamServerClaimsTransformationTests()
    {
        _options.Setup(o => o.Value).Returns(new ThargaBlazorOptions());
    }

    private TeamServerClaimsTransformation CreateSut() =>
        new(_httpContextAccessor.Object, _teamService.Object, _userService.Object, _options.Object, _scopeRegistry.Object);

    private void SetupCookie(string teamKey)
    {
        var cookies = new Mock<IRequestCookieCollection>();
        cookies.Setup(c => c.TryGetValue("selected_team_id", out teamKey)).Returns(true);
        var request = new Mock<HttpRequest>();
        request.Setup(r => r.Cookies).Returns(cookies.Object);
        var context = new Mock<HttpContext>();
        context.Setup(c => c.Request).Returns(request.Object);
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(context.Object);
    }

    private void SetupNoCookie()
    {
        var cookies = new Mock<IRequestCookieCollection>();
        string val = null;
        cookies.Setup(c => c.TryGetValue("selected_team_id", out val)).Returns(false);
        var request = new Mock<HttpRequest>();
        request.Setup(r => r.Cookies).Returns(cookies.Object);
        var context = new Mock<HttpContext>();
        context.Setup(c => c.Request).Returns(request.Object);
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(context.Object);
    }

    private static ClaimsPrincipal CreateAuthenticatedPrincipal(params Claim[] extraClaims)
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, "testuser") };
        claims.AddRange(extraClaims);
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    private static ClaimsPrincipal CreateUnauthenticatedPrincipal() =>
        new(new ClaimsIdentity());

    [Fact]
    public async Task UnauthenticatedUser_ReturnsUnchanged()
    {
        var principal = CreateUnauthenticatedPrincipal();
        var sut = CreateSut();

        var result = await sut.TransformAsync(principal);

        Assert.DoesNotContain(result.Claims, c => c.Type == TeamClaimTypes.TeamKey);
    }

    [Fact]
    public async Task NoCookie_ReturnsUnchanged()
    {
        SetupNoCookie();
        var principal = CreateAuthenticatedPrincipal();
        var sut = CreateSut();

        var result = await sut.TransformAsync(principal);

        Assert.DoesNotContain(result.Claims, c => c.Type == TeamClaimTypes.TeamKey);
    }

    [Fact]
    public async Task NoHttpContext_ReturnsUnchanged()
    {
        _httpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext)null);
        var principal = CreateAuthenticatedPrincipal();
        var sut = CreateSut();

        var result = await sut.TransformAsync(principal);

        Assert.DoesNotContain(result.Claims, c => c.Type == TeamClaimTypes.TeamKey);
    }

    [Fact]
    public async Task AlreadyEnriched_SkipsReEnrichment()
    {
        SetupCookie("team-1");
        var principal = CreateAuthenticatedPrincipal(new Claim("team_id", "team-1"));
        var sut = CreateSut();

        var result = await sut.TransformAsync(principal);

        // Should not call team service since claims already present
        _teamService.Verify(t => t.GetTeamMemberAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task WithCookieAndMember_AddsTeamClaims()
    {
        SetupCookie("team-1");
        var principal = CreateAuthenticatedPrincipal();
        var user = Mock.Of<IUser>(u => u.Key == "user-1");
        _userService.Setup(u => u.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        _teamService.Setup(t => t.GetTeamMemberAsync("team-1", "user-1"))
            .ReturnsAsync(Mock.Of<ITeamMember>(m =>
                m.AccessLevel == AccessLevel.Administrator &&
                m.TenantRoles == Array.Empty<string>() &&
                m.ScopeOverrides == Array.Empty<string>()));
        _scopeRegistry.Setup(s => s.GetEffectiveScopes(
                AccessLevel.Administrator, It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
            .Returns(new[] { "team:read", "team:manage" });

        var sut = CreateSut();
        var result = await sut.TransformAsync(principal);

        Assert.Contains(result.Claims, c => c.Type == TeamClaimTypes.TeamKey && c.Value == "team-1");
        Assert.Contains(result.Claims, c => c.Type == ClaimTypes.Role && c.Value == "TeamMember");
        Assert.Contains(result.Claims, c => c.Type == ClaimTypes.Role && c.Value == "TeamAdministrator");
        Assert.Contains(result.Claims, c => c.Type == TeamClaimTypes.AccessLevel && c.Value == "Administrator");
        Assert.Contains(result.Claims, c => c.Type == TeamClaimTypes.Scope && c.Value == "team:read");
        Assert.Contains(result.Claims, c => c.Type == TeamClaimTypes.Scope && c.Value == "team:manage");
    }

    [Fact]
    public async Task WithCookieButNoMember_OnlyAddsMarkerClaim()
    {
        SetupCookie("team-1");
        var principal = CreateAuthenticatedPrincipal();
        var user = Mock.Of<IUser>(u => u.Key == "user-1");
        _userService.Setup(u => u.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        _teamService.Setup(t => t.GetTeamMemberAsync("team-1", "user-1"))
            .ReturnsAsync((ITeamMember)null);

        var sut = CreateSut();
        var result = await sut.TransformAsync(principal);

        // Marker claim added but no team claims
        Assert.Contains(result.Claims, c => c.Type == "team_id" && c.Value == "team-1");
        Assert.DoesNotContain(result.Claims, c => c.Type == TeamClaimTypes.TeamKey);
    }

    [Fact]
    public async Task WithCookieAndNoScopeRegistry_SkipsScopes()
    {
        SetupCookie("team-1");
        var principal = CreateAuthenticatedPrincipal();
        var user = Mock.Of<IUser>(u => u.Key == "user-1");
        _userService.Setup(u => u.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        _teamService.Setup(t => t.GetTeamMemberAsync("team-1", "user-1"))
            .ReturnsAsync(Mock.Of<ITeamMember>(m =>
                m.AccessLevel == AccessLevel.Viewer &&
                m.TenantRoles == Array.Empty<string>() &&
                m.ScopeOverrides == Array.Empty<string>()));

        var sut = new TeamServerClaimsTransformation(
            _httpContextAccessor.Object, _teamService.Object, _userService.Object, _options.Object, scopeRegistry: null);
        var result = await sut.TransformAsync(principal);

        Assert.Contains(result.Claims, c => c.Type == TeamClaimTypes.TeamKey);
        Assert.DoesNotContain(result.Claims, c => c.Type == TeamClaimTypes.Scope);
    }

    [Fact]
    public async Task ClaimsEnricher_RunsBeforeMemberLookup()
    {
        SetupCookie("team-1");
        var principal = CreateAuthenticatedPrincipal();
        var user = Mock.Of<IUser>(u => u.Key == "user-1");
        _userService.Setup(u => u.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        _teamService.Setup(t => t.GetTeamMemberAsync("team-1", "user-1"))
            .ReturnsAsync((ITeamMember)null);
        _teamService.Setup(t => t.GetConsentedTeamsAsync(It.IsAny<string[]>()))
            .Returns(AsyncEnumerable.Empty<ITeam>());

        var enricher = new Mock<ITeamClaimsEnricher>();
        enricher.Setup(e => e.EnrichAsync(It.IsAny<ClaimsIdentity>()))
            .Callback<ClaimsIdentity>(id => id.AddClaim(new System.Security.Claims.Claim(ClaimTypes.Role, "Developer")));

        var sut = new TeamServerClaimsTransformation(
            _httpContextAccessor.Object, _teamService.Object, _userService.Object, _options.Object,
            _scopeRegistry.Object, enricher.Object);

        var result = await sut.TransformAsync(principal);

        enricher.Verify(e => e.EnrichAsync(It.IsAny<ClaimsIdentity>()), Times.Once);
        Assert.Contains(result.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Developer");
    }

    [Fact]
    public async Task DuplicateClaims_NotAdded()
    {
        SetupCookie("team-1");
        var principal = CreateAuthenticatedPrincipal();
        var user = Mock.Of<IUser>(u => u.Key == "user-1");
        _userService.Setup(u => u.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        _teamService.Setup(t => t.GetTeamMemberAsync("team-1", "user-1"))
            .ReturnsAsync(Mock.Of<ITeamMember>(m =>
                m.AccessLevel == AccessLevel.Administrator &&
                m.TenantRoles == Array.Empty<string>() &&
                m.ScopeOverrides == Array.Empty<string>()));
        _scopeRegistry.Setup(s => s.GetEffectiveScopes(
                AccessLevel.Administrator, It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
            .Returns(new[] { "team:read" });

        var sut = CreateSut();

        // Transform twice — re-entrance guard should prevent duplicates
        var result = await sut.TransformAsync(principal);
        result = await sut.TransformAsync(result);

        var teamKeyClaims = result.Claims.Where(c => c.Type == TeamClaimTypes.TeamKey).ToArray();
        Assert.Single(teamKeyClaims);
    }
}
