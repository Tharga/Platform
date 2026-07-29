using Tharga.Team.Blazor.Features.User;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// The facts the Teams grid derives from a member list. Each exists because the grid previously could not
/// answer the question an operator arrives with: is this team still in use, who owns it, and is that
/// member count real or a pile of unanswered invitations.
/// </summary>
public class TeamActivityTests
{
    private static TeamMemberInfo Member(
        string name = "m",
        AccessLevel accessLevel = AccessLevel.User,
        MembershipState? state = MembershipState.Member,
        DateTime? lastSeen = null)
        => new() { Key = name, Name = name, AccessLevel = accessLevel, State = state, LastSeen = lastSeen };

    private static readonly DateTime Early = new(2026, 1, 1);
    private static readonly DateTime Late = new(2026, 6, 1);

    [Fact]
    public void LastUsed_TakesTheMostRecentMember()
    {
        var members = new[] { Member(lastSeen: Early), Member(lastSeen: Late), Member(lastSeen: null) };
        Assert.Equal(Late, TeamActivity.LastUsed(members));
    }

    [Fact]
    public void LastUsed_WhenNoMemberEverUsedIt_IsNull()
    {
        Assert.Null(TeamActivity.LastUsed([Member(), Member()]));
    }

    [Fact]
    public void LastUsed_WithNoMembers_IsNull()
    {
        Assert.Null(TeamActivity.LastUsed([]));
        Assert.Null(TeamActivity.LastUsed(null));
    }

    [Fact]
    public void Owner_IsTheMemberAtOwnerLevel()
    {
        var owner = Member("boss", AccessLevel.Owner);
        var members = new[] { Member("a"), owner, Member("b", AccessLevel.Administrator) };
        Assert.Same(owner, TeamActivity.Owner(members));
    }

    /// <summary>An ownerless team is a data defect; it must surface as null rather than pick a stand-in.</summary>
    [Fact]
    public void Owner_WhenNoneHoldsOwner_IsNull()
    {
        Assert.Null(TeamActivity.Owner([Member("a"), Member("b", AccessLevel.Administrator)]));
    }

    [Fact]
    public void Owner_WithNoMembers_IsNull()
    {
        Assert.Null(TeamActivity.Owner([]));
        Assert.Null(TeamActivity.Owner(null));
    }

    [Fact]
    public void CountByState_SeparatesAcceptedFromInvited()
    {
        var members = new[]
        {
            Member("a"),
            Member("b"),
            Member("c", state: MembershipState.Invited),
            Member("d", state: MembershipState.Invited),
            Member("e", state: MembershipState.Invited)
        };

        Assert.Equal((2, 3), TeamActivity.CountByState(members));
    }

    /// <summary>
    /// A null state predates the field and means an ordinary member; counting it as neither would
    /// under-report every team created before the state was introduced.
    /// </summary>
    [Fact]
    public void CountByState_TreatsNullStateAsActive()
    {
        Assert.Equal((1, 0), TeamActivity.CountByState([Member(state: null)]));
    }

    /// <summary>A rejected invitation is neither a member nor an outstanding invitation.</summary>
    [Fact]
    public void CountByState_ExcludesRejected()
    {
        var members = new[] { Member("a"), Member("b", state: MembershipState.Rejected) };
        Assert.Equal((1, 0), TeamActivity.CountByState(members));
    }

    [Fact]
    public void CountByState_WithNoMembers_IsZero()
    {
        Assert.Equal((0, 0), TeamActivity.CountByState([]));
        Assert.Equal((0, 0), TeamActivity.CountByState(null));
    }
}
