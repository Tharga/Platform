namespace Tharga.Team.Service.Tests;

/// <summary>
/// Verifies that <see cref="TeamServiceBase"/> handles a null current user gracefully:
/// read paths return empty, side-effect paths throw <see cref="UnauthorizedAccessException"/>.
/// </summary>
public class UnauthenticatedTeamServiceTests
{
    private readonly TestTeamService _sut;

    public UnauthenticatedTeamServiceTests()
    {
        var userService = Substitute.For<IUserService>();
        userService.GetCurrentUserAsync().Returns((IUser)null);

        _sut = new TestTeamService(userService);
        _sut.AddTeam("team-1", "Test Team",
            new TestMember { Key = "user-1", AccessLevel = AccessLevel.Owner, State = MembershipState.Member },
            new TestMember { Key = "user-2", AccessLevel = AccessLevel.User, State = MembershipState.Member });
    }

    [Fact]
    public async Task GetTeamsAsync_Unauthenticated_ReturnsEmpty()
    {
        var teams = new List<ITeam>();
        await foreach (var t in _sut.GetTeamsAsync()) teams.Add(t);

        Assert.Empty(teams);
    }

    [Fact]
    public async Task GetTeamsAsync_Generic_Unauthenticated_ReturnsEmpty()
    {
        var teams = new List<ITeam<TestMember>>();
        await foreach (var t in _sut.GetTeamsAsync<TestMember>()) teams.Add(t);

        Assert.Empty(teams);
    }

    [Fact]
    public async Task CreateTeamAsync_Unauthenticated_Throws()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.CreateTeamAsync("New Team"));
    }

    [Fact]
    public async Task RemoveMemberAsync_Unauthenticated_Throws()
    {
        // user-2 is a non-owner; attempting to remove them as an unauthenticated caller
        // should hit the RequireCurrentUserAsync guard before any DB write.
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.RemoveMemberAsync("team-1", "user-2"));
    }

    [Fact]
    public async Task SetMemberLastSeenAsync_Unauthenticated_DoesNotThrow()
    {
        var ex = await Record.ExceptionAsync(() => _sut.SetMemberLastSeenAsync("team-1"));
        Assert.Null(ex);
    }

    [Fact]
    public async Task TransferOwnershipAsync_Unauthenticated_Throws()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.TransferOwnershipAsync<TestMember>("team-1", "user-1"));
    }
}
