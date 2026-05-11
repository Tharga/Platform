namespace Tharga.Team.Service.Tests;

/// <summary>
/// Verifies the default <see cref="TeamServiceBase.GetMembersAsync"/> implementation
/// — which uses reflection internally to read the typed team's <c>Members</c> array —
/// yields the underlying members as <see cref="ITeamMember"/> without forcing the caller
/// to know the consumer-specific <c>TMember</c> type.
/// </summary>
public class TeamServiceBaseGetMembersAsyncTests
{
    private readonly IUserService _userService = Substitute.For<IUserService>();

    [Fact]
    public async Task GetMembersAsync_ReturnsAllMembers_AsITeamMember()
    {
        var sut = new TestTeamService(_userService);
        sut.AddTeam("team-1", "Test Team",
            new TestMember { Key = "u-1", AccessLevel = AccessLevel.Owner, State = MembershipState.Member },
            new TestMember { Key = "u-2", AccessLevel = AccessLevel.User, State = MembershipState.Member });

        var members = new List<ITeamMember>();
        await foreach (var member in sut.GetMembersAsync("team-1"))
        {
            members.Add(member);
        }

        Assert.Equal(2, members.Count);
        Assert.Equal("u-1", members[0].Key);
        Assert.Equal("u-2", members[1].Key);
    }

    [Fact]
    public async Task GetMembersAsync_UnknownTeam_ReturnsEmpty()
    {
        var sut = new TestTeamService(_userService);

        var members = new List<ITeamMember>();
        await foreach (var member in sut.GetMembersAsync("does-not-exist"))
        {
            members.Add(member);
        }

        Assert.Empty(members);
    }
}
