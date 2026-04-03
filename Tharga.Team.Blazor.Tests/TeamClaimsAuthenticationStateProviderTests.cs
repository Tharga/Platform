using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using Tharga.Team;
using Tharga.Team.Blazor.Framework;

namespace Tharga.Team.Blazor.Tests;

public class TeamClaimsAuthenticationStateProviderTests
{
    private readonly Mock<AuthenticationStateProvider> _inner = new();
    private readonly Mock<ITeamService> _teamService = new();
    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<IScopeRegistry> _scopeRegistry = new();
    private readonly Mock<ILocalStorageService> _localStorage = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();

    private TeamClaimsAuthenticationStateProvider CreateSut() =>
        new(_inner.Object, _teamService.Object, _userService.Object, _localStorage.Object, _httpContextAccessor.Object, _scopeRegistry.Object);

    private void SetupInnerAuthState(ClaimsPrincipal principal)
    {
        _inner.Setup(p => p.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(principal));
    }

    private static ClaimsPrincipal CreateAuthenticatedPrincipal(params Claim[] extraClaims)
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, "testuser") };
        claims.AddRange(extraClaims);
        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    private static ClaimsPrincipal CreateUnauthenticatedPrincipal()
    {
        var identity = new ClaimsIdentity();
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public async Task UnauthenticatedUser_ReturnsUnchangedState()
    {
        var principal = CreateUnauthenticatedPrincipal();
        SetupInnerAuthState(principal);
        var sut = CreateSut();

        var result = await sut.GetAuthenticationStateAsync();

        Assert.False(result.User.Identity?.IsAuthenticated);
        Assert.DoesNotContain(result.User.Claims, c => c.Type == TeamClaimTypes.TeamKey);
    }

    [Fact]
    public async Task AlreadyAugmented_ReturnsUnchangedState()
    {
        var principal = CreateAuthenticatedPrincipal(new Claim("team_id", "team-1"));
        SetupInnerAuthState(principal);
        var sut = CreateSut();

        var result = await sut.GetAuthenticationStateAsync();

        _teamService.Verify(s => s.GetTeamMemberAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task NoTeamInLocalStorage_DoesNotAddTeamClaims()
    {
        var principal = CreateAuthenticatedPrincipal();
        SetupInnerAuthState(principal);
        _localStorage.Setup(s => s.GetItemAsStringAsync("SelectedTeam", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string)null);
        var sut = CreateSut();

        var result = await sut.GetAuthenticationStateAsync();

        Assert.DoesNotContain(result.User.Claims, c => c.Type == TeamClaimTypes.TeamKey);
    }

    [Fact]
    public async Task WithTeamInLocalStorage_AddsTeamKeyAndRoleClaims()
    {
        var principal = CreateAuthenticatedPrincipal();
        SetupInnerAuthState(principal);
        _localStorage.Setup(s => s.GetItemAsStringAsync("SelectedTeam", It.IsAny<CancellationToken>()))
            .ReturnsAsync("team-1");

        var user = new Mock<IUser>();
        user.Setup(u => u.Key).Returns("user-1");
        _userService.Setup(s => s.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user.Object);

        var member = new Mock<ITeamMember>();
        member.Setup(m => m.AccessLevel).Returns(AccessLevel.Administrator);
        member.Setup(m => m.TenantRoles).Returns(Array.Empty<string>());
        member.Setup(m => m.ScopeOverrides).Returns(Array.Empty<string>());
        _teamService.Setup(s => s.GetTeamMemberAsync("team-1", "user-1")).ReturnsAsync(member.Object);
        _scopeRegistry.Setup(s => s.GetEffectiveScopes(AccessLevel.Administrator, It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
            .Returns(new List<string>());

        var sut = CreateSut();
        var result = await sut.GetAuthenticationStateAsync();

        Assert.Contains(result.User.Claims, c => c.Type == TeamClaimTypes.TeamKey && c.Value == "team-1");
        Assert.Contains(result.User.Claims, c => c.Type == ClaimTypes.Role && c.Value == Roles.TeamMember);
        Assert.Contains(result.User.Claims, c => c.Type == ClaimTypes.Role && c.Value == "TeamAdministrator");
        Assert.Contains(result.User.Claims, c => c.Type == TeamClaimTypes.AccessLevel && c.Value == "Administrator");
    }

    [Fact]
    public async Task WithTeamMember_AddsScopeClaims()
    {
        var principal = CreateAuthenticatedPrincipal();
        SetupInnerAuthState(principal);
        _localStorage.Setup(s => s.GetItemAsStringAsync("SelectedTeam", It.IsAny<CancellationToken>()))
            .ReturnsAsync("team-1");

        var user = new Mock<IUser>();
        user.Setup(u => u.Key).Returns("user-1");
        _userService.Setup(s => s.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user.Object);

        var member = new Mock<ITeamMember>();
        member.Setup(m => m.AccessLevel).Returns(AccessLevel.User);
        member.Setup(m => m.TenantRoles).Returns(new[] { "editor" });
        member.Setup(m => m.ScopeOverrides).Returns(new[] { "extra-scope" });
        _teamService.Setup(s => s.GetTeamMemberAsync("team-1", "user-1")).ReturnsAsync(member.Object);
        _scopeRegistry.Setup(s => s.GetEffectiveScopes(AccessLevel.User, It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
            .Returns(new List<string> { "read", "write", "extra-scope" });

        var sut = CreateSut();
        var result = await sut.GetAuthenticationStateAsync();

        Assert.Contains(result.User.Claims, c => c.Type == TeamClaimTypes.Scope && c.Value == "read");
        Assert.Contains(result.User.Claims, c => c.Type == TeamClaimTypes.Scope && c.Value == "write");
        Assert.Contains(result.User.Claims, c => c.Type == TeamClaimTypes.Scope && c.Value == "extra-scope");
    }

    [Fact]
    public async Task MemberNotFound_DoesNotAddRoleClaims()
    {
        var principal = CreateAuthenticatedPrincipal();
        SetupInnerAuthState(principal);
        _localStorage.Setup(s => s.GetItemAsStringAsync("SelectedTeam", It.IsAny<CancellationToken>()))
            .ReturnsAsync("team-1");

        var user = new Mock<IUser>();
        user.Setup(u => u.Key).Returns("user-1");
        _userService.Setup(s => s.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user.Object);
        _teamService.Setup(s => s.GetTeamMemberAsync("team-1", "user-1")).ReturnsAsync((ITeamMember)null);

        var sut = CreateSut();
        var result = await sut.GetAuthenticationStateAsync();

        Assert.Contains(result.User.Claims, c => c.Type == "team_id" && c.Value == "team-1");
        Assert.DoesNotContain(result.User.Claims, c => c.Type == TeamClaimTypes.TeamKey);
        Assert.DoesNotContain(result.User.Claims, c => c.Type == ClaimTypes.Role && c.Value == Roles.TeamMember);
    }

    [Fact]
    public async Task WithoutScopeRegistry_SkipsScopes()
    {
        var principal = CreateAuthenticatedPrincipal();
        SetupInnerAuthState(principal);
        _localStorage.Setup(s => s.GetItemAsStringAsync("SelectedTeam", It.IsAny<CancellationToken>()))
            .ReturnsAsync("team-1");

        var user = new Mock<IUser>();
        user.Setup(u => u.Key).Returns("user-1");
        _userService.Setup(s => s.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user.Object);

        var member = new Mock<ITeamMember>();
        member.Setup(m => m.AccessLevel).Returns(AccessLevel.Owner);
        member.Setup(m => m.TenantRoles).Returns(Array.Empty<string>());
        member.Setup(m => m.ScopeOverrides).Returns(Array.Empty<string>());
        _teamService.Setup(s => s.GetTeamMemberAsync("team-1", "user-1")).ReturnsAsync(member.Object);

        var sut = new TeamClaimsAuthenticationStateProvider(
            _inner.Object, _teamService.Object, _userService.Object, _localStorage.Object, _httpContextAccessor.Object, null);
        var result = await sut.GetAuthenticationStateAsync();

        Assert.Contains(result.User.Claims, c => c.Type == TeamClaimTypes.TeamKey);
        Assert.DoesNotContain(result.User.Claims, c => c.Type == TeamClaimTypes.Scope);
    }

    [Fact]
    public async Task JsInteropFailure_ReturnsUnaugmentedState()
    {
        var principal = CreateAuthenticatedPrincipal();
        SetupInnerAuthState(principal);
        _localStorage.Setup(s => s.GetItemAsStringAsync("SelectedTeam", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("JS interop not available"));

        var sut = CreateSut();
        var result = await sut.GetAuthenticationStateAsync();

        Assert.True(result.User.Identity?.IsAuthenticated);
        Assert.DoesNotContain(result.User.Claims, c => c.Type == TeamClaimTypes.TeamKey);
    }
}
