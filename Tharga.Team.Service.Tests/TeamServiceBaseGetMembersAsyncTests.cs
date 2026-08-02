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

    /// <summary>
    /// <b>The distinction that caused a shipped bug.</b> <c>GetMembersAsync</c> returns the roster in
    /// every <see cref="MembershipState"/> and is the only portable way to see one. What
    /// <c>GetTeamMemberAsync</c> reports for an invitee is decided by the host's store — the MongoDB one
    /// filters them out, this test double does not — so no caller may depend on it either way.
    /// </summary>
    /// <remarks>
    /// Suspending a member shipped using the first when it needed the second, and against the MongoDB
    /// store refused an invitee with "is not a member of team" — untrue, since they are on the roster,
    /// and unhelpful, since it describes a different problem. That the two disagree is not the defect;
    /// depending on the one whose answer the host controls is.
    /// </remarks>
    [Fact]
    public async Task GetTeamMemberAsync_AndGetMembersAsync_DisagreeAboutAnInvitee_OnPurpose()
    {
        var sut = new TestTeamService(_userService);
        sut.AddTeam("team-2", "Test Team",
            new TestMember { Key = "active", AccessLevel = AccessLevel.User, State = MembershipState.Member },
            new TestMember { Key = "invitee", AccessLevel = AccessLevel.User, State = MembershipState.Invited });

        var roster = new List<ITeamMember>();
        await foreach (var member in sut.GetMembersAsync("team-2")) roster.Add(member);

        // Guaranteed by the toolkit: the roster carries every state.
        Assert.Contains(roster, m => m.Key == "invitee");
        Assert.Contains(roster, m => m.Key == "active");

        // Not guaranteed: what GetTeamMemberAsync says about the invitee is the host store's business.
        // This double does not filter, so it returns them; the MongoDB store filters, so it does not.
        // The test asserts only the part that holds everywhere -- an active member always resolves.
        Assert.NotNull(await sut.GetTeamMemberAsync("team-2", "active"));
    }
}
