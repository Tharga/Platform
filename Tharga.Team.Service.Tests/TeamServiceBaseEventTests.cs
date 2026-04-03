namespace Tharga.Team.Service.Tests;

public class TeamServiceBaseEventTests
{
    private readonly TestTeamService _sut;

    public TeamServiceBaseEventTests()
    {
        var userService = Substitute.For<IUserService>();
        var user = Substitute.For<IUser>();
        user.Key.Returns("user-1");
        user.EMail.Returns("test@example.com");
        userService.GetCurrentUserAsync().Returns(user);

        _sut = new TestTeamService(userService);
        _sut.AddTeam("team-1", "Test Team",
            new TestMember { Key = "user-1", AccessLevel = AccessLevel.Owner, State = MembershipState.Member },
            new TestMember { Key = "user-2", AccessLevel = AccessLevel.User, State = MembershipState.Member });
    }

    [Fact]
    public async Task AddMemberAsync_FiresTeamsListChangedEvent()
    {
        var fired = false;
        _sut.TeamsListChangedEvent += (_, _) => fired = true;

        await _sut.AddMemberAsync("team-1", new InviteUserModel { Email = "new@example.com" });

        Assert.True(fired);
    }

    [Fact]
    public async Task SetMemberRoleAsync_FiresTeamsListChangedEvent()
    {
        var fired = false;
        _sut.TeamsListChangedEvent += (_, _) => fired = true;

        await _sut.SetMemberRoleAsync("team-1", "user-2", AccessLevel.Administrator);

        Assert.True(fired);
    }

    [Fact]
    public async Task SetMemberTenantRolesAsync_FiresTeamsListChangedEvent()
    {
        var fired = false;
        _sut.TeamsListChangedEvent += (_, _) => fired = true;

        await _sut.SetMemberTenantRolesAsync("team-1", "user-2", new[] { "Editor" });

        Assert.True(fired);
    }

    [Fact]
    public async Task SetMemberScopeOverridesAsync_FiresTeamsListChangedEvent()
    {
        var fired = false;
        _sut.TeamsListChangedEvent += (_, _) => fired = true;

        await _sut.SetMemberScopeOverridesAsync("team-1", "user-2", new[] { "feature:read" });

        Assert.True(fired);
    }

    [Fact]
    public async Task SetInvitationResponseAsync_Reject_FiresTeamsListChangedEvent()
    {
        var fired = false;
        _sut.TeamsListChangedEvent += (_, _) => fired = true;

        await _sut.SetInvitationResponseAsync("team-1", "user-2", "invite-key", false);

        Assert.True(fired);
    }

    [Fact]
    public async Task SetTeamConsentAsync_FiresTeamsListChangedEvent()
    {
        var fired = false;
        _sut.TeamsListChangedEvent += (_, _) => fired = true;

        await _sut.SetTeamConsentAsync("team-1", new[] { "Developer" });

        Assert.True(fired);
    }

    [Fact]
    public async Task CreateTeamAsync_FiresTeamsListChangedEvent()
    {
        var fired = false;
        _sut.TeamsListChangedEvent += (_, _) => fired = true;

        await _sut.CreateTeamAsync("Test Team");

        Assert.True(fired);
    }

    [Fact]
    public async Task RemoveMemberAsync_FiresTeamsListChangedEvent()
    {
        var fired = false;
        _sut.TeamsListChangedEvent += (_, _) => fired = true;

        await _sut.RemoveMemberAsync("team-1", "user-2");

        Assert.True(fired);
    }

    [Fact]
    public async Task SetInvitationResponseAsync_Accept_FiresTeamsListChangedEvent()
    {
        var fired = false;
        _sut.TeamsListChangedEvent += (_, _) => fired = true;

        await _sut.SetInvitationResponseAsync("team-1", "user-2", "invite-key", true);

        Assert.True(fired);
    }

    [Fact]
    public async Task TransferOwnershipAsync_FiresTeamsListChangedEvent()
    {
        var fired = false;
        _sut.TeamsListChangedEvent += (_, _) => fired = true;

        await _sut.TransferOwnershipAsync<TestMember>("team-1", "user-2");

        Assert.True(fired);
    }
}
