using Tharga.Team;

namespace Tharga.Team.Service.Tests;

/// <summary>
/// The rules behind <see cref="SystemTeamScopes.AssignOwner"/>. They are the entire safety argument for
/// an operation that hands out <c>Owner</c> with no sitting owner's consent, so they are pinned here
/// rather than left inside a service method.
/// </summary>
public class TeamOwnershipTests
{
    private sealed record Member(string Key, AccessLevel AccessLevel) : ITeamMember
    {
        public string Name => null;
        public string[] TenantRoles => null;
        public string[] ScopeOverrides => null;
        public MembershipState? State => MembershipState.Member;
        public Invitation Invitation => null;
        public DateTime? LastSeen => null;
    }

    private static Member[] Healthy =>
    [
        new("owner-1", AccessLevel.Owner),
        new("admin-1", AccessLevel.Administrator)
    ];

    private static Member[] Ownerless =>
    [
        new("admin-1", AccessLevel.Administrator),
        new("user-1", AccessLevel.User)
    ];

    [Fact]
    public void IsOwnerless_TeamWithAnOwner_IsFalse()
    {
        Assert.False(TeamOwnership.IsOwnerless(Healthy));
    }

    [Fact]
    public void IsOwnerless_TeamWithoutAnOwner_IsTrue()
    {
        Assert.True(TeamOwnership.IsOwnerless(Ownerless));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsOwnerless_NoRoster_IsTrue(bool useNull)
    {
        Assert.True(TeamOwnership.IsOwnerless(useNull ? null : []));
    }

    /// <summary>The repair case: an ownerless team promoting one of its own members.</summary>
    [Fact]
    public void CanAssign_ExistingMemberOfAnOwnerlessTeam_IsAllowed()
    {
        Assert.True(TeamOwnership.CanAssign(Ownerless, "admin-1"));
    }

    /// <summary>
    /// The condition that keeps this a repair rather than a takeover. With a sitting owner there is
    /// somebody to escalate past, which is exactly what <c>SetMemberRoleAsync</c> refuses to allow.
    /// </summary>
    [Fact]
    public void CanAssign_TeamThatAlreadyHasAnOwner_IsRefused()
    {
        Assert.False(TeamOwnership.CanAssign(Healthy, "admin-1"));
    }

    /// <summary>
    /// The condition that keeps a repair from introducing an outsider. The caller is not a member of
    /// this team, so without it they could add anyone — including themselves.
    /// </summary>
    [Fact]
    public void CanAssign_SomeoneWhoIsNotAMember_IsRefused()
    {
        Assert.False(TeamOwnership.CanAssign(Ownerless, "stranger"));
    }

    /// <summary>An ownerless team with nobody in it has nobody to promote.</summary>
    [Fact]
    public void CanAssign_EmptyTeam_IsRefused()
    {
        Assert.False(TeamOwnership.CanAssign([], "admin-1"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CanAssign_NoCandidate_IsRefused(string candidate)
    {
        Assert.False(TeamOwnership.CanAssign(Ownerless, candidate));
    }

    /// <summary>A null entry in the roster must not be mistaken for a member or crash the check.</summary>
    [Fact]
    public void CanAssign_RosterWithNulls_IsTolerated()
    {
        ITeamMember[] roster = [null, new Member("admin-1", AccessLevel.Administrator), null];

        Assert.True(TeamOwnership.CanAssign(roster, "admin-1"));
        Assert.False(TeamOwnership.CanAssign(roster, "stranger"));
    }
}
