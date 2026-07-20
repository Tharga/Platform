using System.Security.Claims;
using Tharga.Team;
using Tharga.Team.Service;

namespace Tharga.Team.Service.Tests;

/// <summary>
/// Cross-team discovery (<c>ITeamService.GetAllTeamsAsync</c>) gated on the
/// <see cref="SystemTeamScopes.Read"/> system scope, plus the non-breaking default on
/// <see cref="TeamServiceBase"/> for services that don't support it.
/// </summary>
public class CrossTeamListingTests
{
    private static (AuthorizationTeamServiceDecorator sut, ITeamService inner) Build(ClaimsPrincipal principal)
    {
        var inner = Substitute.For<ITeamService>();
        var accessor = Substitute.For<ITeamPrincipalAccessor>();
        accessor.GetCurrentAsync().Returns(new ValueTask<ClaimsPrincipal>(principal));
        var sut = new AuthorizationTeamServiceDecorator(inner, new TeamAuthorizer(accessor), new TeamLifecycleOptions { AllowTeamCreation = true });
        return (sut, inner);
    }

    private static ClaimsPrincipal Principal(string teamKey, params string[] scopes)
    {
        var claims = new List<Claim>();
        if (teamKey != null) claims.Add(new Claim(TeamClaimTypes.TeamKey, teamKey));
        foreach (var s in scopes) claims.Add(new Claim(TeamClaimTypes.Scope, s));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static async IAsyncEnumerable<T> Stream<T>(params T[] items)
    {
        await Task.CompletedTask;
        foreach (var item in items) yield return item;
    }

    private static async Task<int> CountAsync(IAsyncEnumerable<ITeam> source)
    {
        var count = 0;
        await foreach (var _ in source) count++;
        return count;
    }

    [Fact]
    public async Task GetAllTeams_WithTeamsRead_Delegates()
    {
        var (sut, inner) = Build(Principal("T1", SystemTeamScopes.Read));
        inner.GetAllTeamsAsync().Returns(Stream(Substitute.For<ITeam>(), Substitute.For<ITeam>()));

        Assert.Equal(2, await CountAsync(sut.GetAllTeamsAsync()));
    }

    [Fact]
    public async Task GetAllTeams_WithoutTeamsRead_Throws()
    {
        var (sut, inner) = Build(Principal("T1"));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await CountAsync(sut.GetAllTeamsAsync()));
        inner.DidNotReceive().GetAllTeamsAsync();
    }

    /// <summary>
    /// The in-team manage scope must not stand in for the system scope — that conflation is the whole
    /// bug class this feature has to avoid.
    /// </summary>
    [Fact]
    public async Task GetAllTeams_TeamManageIsNotEnough_Throws()
    {
        var (sut, _) = Build(Principal("T1", TeamScopes.Manage));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await CountAsync(sut.GetAllTeamsAsync()));
    }

    [Fact]
    public async Task GetAllTeams_TeamsDeleteIsNotEnough_Throws()
    {
        var (sut, _) = Build(Principal("T1", SystemTeamScopes.Delete));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await CountAsync(sut.GetAllTeamsAsync()));
    }

    [Fact]
    public async Task GetAllTeams_Unauthenticated_Throws()
    {
        var (sut, _) = Build(new ClaimsPrincipal(new ClaimsIdentity()));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await CountAsync(sut.GetAllTeamsAsync()));
    }

    /// <summary>
    /// A service deriving from <see cref="TeamServiceBase"/> that predates this feature still compiles
    /// (the member is virtual, not abstract) and yields nothing rather than throwing.
    /// </summary>
    [Fact]
    public async Task TeamServiceBase_DefaultGetAllTeams_IsEmpty()
    {
        var userService = Substitute.For<IUserService>();
        var sut = new TestTeamService(userService);

        Assert.Equal(0, await CountAsync(sut.GetAllTeamsAsync()));
    }
}
