using System.Security.Claims;
using Microsoft.Extensions.Options;
using Moq;
using Tharga.Team;
using Tharga.Team.Blazor.Framework;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Direct coverage of the shared membership/consent claim builder — notably the consent-grant path, which
/// the transformation and revalidator tests exercise only in its "no access" form.
/// </summary>
public class TeamMembershipClaimsBuilderTests
{
    private const string TeamKey = "team-1";
    private const string UserKey = "user-1";

    private readonly Mock<ITeamService> _teamService = new();
    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<IScopeRegistry> _scopeRegistry = new();
    private readonly Mock<IOptions<ThargaBlazorOptions>> _options = new();

    public TeamMembershipClaimsBuilderTests()
    {
        _options.Setup(o => o.Value).Returns(new ThargaBlazorOptions());
        _userService.Setup(u => u.GetCurrentUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(Mock.Of<IUser>(u => u.Key == UserKey));
    }

    private TeamMembershipClaimsBuilder CreateSut() =>
        new(_teamService.Object, _userService.Object, _options.Object, _scopeRegistry.Object);

    private static async IAsyncEnumerable<ITeam> Async(params ITeam[] items)
    {
        await Task.CompletedTask;
        foreach (var item in items) yield return item;
    }

    private static ClaimsPrincipal PrincipalWithRoles(params string[] roles) =>
        new(new ClaimsIdentity(roles.Select(r => new Claim(ClaimTypes.Role, r)), "TestAuth"));

    [Fact]
    public async Task NoTeamKey_ReturnsEmpty()
    {
        var claims = await CreateSut().BuildAsync(PrincipalWithRoles("Support"), "");

        Assert.Empty(claims);
    }

    [Fact]
    public async Task NonMemberWithConsentedRole_GetsConsentAccessAtTheTeamsLevel()
    {
        _teamService.Setup(t => t.GetTeamMemberAsync(TeamKey, UserKey)).ReturnsAsync((ITeamMember)null);
        _teamService.Setup(t => t.GetConsentedTeamsAsync(It.Is<string[]>(r => r.Contains("Support"))))
            .Returns(Async(Mock.Of<ITeam>(t => t.Key == TeamKey && t.ConsentAccessLevel == AccessLevel.Viewer)));
        _scopeRegistry.Setup(s => s.GetEffectiveScopes(AccessLevel.Viewer, It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
            .Returns(new[] { "team:read" });

        var claims = await CreateSut().BuildAsync(PrincipalWithRoles("Support"), TeamKey);

        Assert.Contains(claims, c => c.Type == TeamClaimTypes.TeamKey && c.Value == TeamKey);
        Assert.Contains(claims, c => c.Type == ClaimTypes.Role && c.Value == Roles.TeamMember);
        Assert.Contains(claims, c => c.Type == ClaimTypes.Role && c.Value == "TeamViewer");
        Assert.Contains(claims, c => c.Type == TeamClaimTypes.AccessLevel && c.Value == "Viewer");
        Assert.Contains(claims, c => c.Type == TeamClaimTypes.Scope && c.Value == "team:read");
    }

    [Fact]
    public async Task NonMemberWithoutAConsentedTeam_ReturnsEmpty()
    {
        _teamService.Setup(t => t.GetTeamMemberAsync(TeamKey, UserKey)).ReturnsAsync((ITeamMember)null);
        _teamService.Setup(t => t.GetConsentedTeamsAsync(It.IsAny<string[]>())).Returns(Async());

        var claims = await CreateSut().BuildAsync(PrincipalWithRoles("Support"), TeamKey);

        Assert.Empty(claims);
    }
}
