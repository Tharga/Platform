namespace Tharga.Team.Service.Tests;

/// <summary>
/// Suspending a member's access to one team: the team-scoped sibling of disabling a user.
/// </summary>
/// <remarks>
/// The member keeps their membership, access level, roles and history, and still sees the team in the
/// selector — they simply hold no scopes in it. That last part is enforced in
/// <c>TeamMembershipClaimsBuilder</c>, not here; these tests cover the two refusals and the write.
/// <para>
/// <b>Both refusals mirror guards this class already applies.</b> They live in the service rather than
/// only in <c>TeamComponent</c> because the UI is not the only caller — and because the delete guard
/// already had to learn that lesson once.
/// </para>
/// </remarks>
public class MemberSuspensionTests
{
    private const string TeamKey = "team-1";

    private static (TestTeamService Sut, IUserService UserService) Build(string callerKey, params TestMember[] members)
    {
        // The caller substitute is built first, never inside Returns() — NSubstitute cannot configure one
        // substitute from within another's Returns call, and does not fail until the test runs.
        var caller = User(callerKey);
        var userService = Substitute.For<IUserService>();
        userService.GetCurrentUserAsync().Returns(caller);
        userService.GetCurrentUserAsync(Arg.Any<System.Security.Claims.ClaimsPrincipal>()).Returns(caller);

        var sut = new TestTeamService(userService);
        sut.AddTeam(TeamKey, "Test Team", members);
        return (sut, userService);
    }

    private static IUser User(string key)
    {
        if (key == null) return null;
        var user = Substitute.For<IUser>();
        user.Key.Returns(key);
        return user;
    }

    private static TestMember Member(string key, AccessLevel accessLevel = AccessLevel.User, DateTime? suspendedAt = null)
        => new() { Key = key, AccessLevel = accessLevel, State = MembershipState.Member, SuspendedAt = suspendedAt };

    private static TestMember Invited(string key)
        => new() { Key = key, AccessLevel = AccessLevel.User, State = MembershipState.Invited };

    [Fact]
    public async Task Suspending_RecordsWhenAndByWhom()
    {
        var (sut, _) = Build("admin", Member("admin", AccessLevel.Administrator), Member("u1"));

        await sut.SetMemberSuspendedAsync(TeamKey, "u1", suspended: true);

        var call = Assert.Single(sut.SuspendCalls);
        Assert.Equal((TeamKey, "u1"), (call.TeamKey, call.UserKey));
        Assert.NotNull(call.SuspendedAt);
        Assert.Equal("admin", call.SuspendedBy);
    }

    /// <summary>Restoring clears both, so a restored member carries no stale trace of the old decision.</summary>
    [Fact]
    public async Task Restoring_ClearsBoth()
    {
        var (sut, _) = Build("admin", Member("admin", AccessLevel.Administrator), Member("u1", suspendedAt: DateTime.UtcNow));

        await sut.SetMemberSuspendedAsync(TeamKey, "u1", suspended: false);

        var call = Assert.Single(sut.SuspendCalls);
        Assert.Null(call.SuspendedAt);
        Assert.Null(call.SuspendedBy);
    }

    /// <summary>
    /// The Owner cannot be suspended, for the same reason they cannot leave or be demoted: it would
    /// leave a team whose ownership nobody can transfer, because transfer requires the caller to be the
    /// owner.
    /// </summary>
    [Fact]
    public async Task SuspendingTheOwner_IsRefused()
    {
        var (sut, _) = Build("admin", Member("admin", AccessLevel.Administrator), Member("owner", AccessLevel.Owner));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SetMemberSuspendedAsync(TeamKey, "owner", suspended: true));

        Assert.Empty(sut.SuspendCalls);
    }

    /// <summary>
    /// An administrator who suspends themselves needs a second one to undo it, and refusing the self-case
    /// guarantees somebody is left holding <c>member:manage</c> in the team.
    /// </summary>
    [Fact]
    public async Task SuspendingYourself_IsRefused()
    {
        var (sut, _) = Build("admin", Member("admin", AccessLevel.Administrator));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SetMemberSuspendedAsync(TeamKey, "admin", suspended: true));

        Assert.Empty(sut.SuspendCalls);
    }

    /// <summary>A differently-cased key is the same member; the guard is not side-steppable by casing.</summary>
    [Fact]
    public async Task SuspendingYourself_IsRefusedWhateverTheCasing()
    {
        var (sut, _) = Build("ADMIN", Member("admin", AccessLevel.Administrator));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SetMemberSuspendedAsync(TeamKey, "admin", suspended: true));
    }

    /// <summary>
    /// Neither refusal applies to restoring. Restoring is the reversible direction, and blocking the
    /// owner case would strand the one membership an operator most needs to repair.
    /// </summary>
    [Theory]
    [InlineData("owner")]
    [InlineData("admin")]
    public async Task Restoring_IsNeverRefused(string targetKey)
    {
        var (sut, _) = Build("admin",
            Member("admin", AccessLevel.Administrator, DateTime.UtcNow),
            Member("owner", AccessLevel.Owner, DateTime.UtcNow));

        await sut.SetMemberSuspendedAsync(TeamKey, targetKey, suspended: false);

        Assert.Single(sut.SuspendCalls);
    }

    [Fact]
    public async Task SuspendingSomebodyWhoIsNotAMember_IsRefused()
    {
        var (sut, _) = Build("admin", Member("admin", AccessLevel.Administrator));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SetMemberSuspendedAsync(TeamKey, "stranger", suspended: true));

        Assert.Empty(sut.SuspendCalls);
    }

    /// <summary>
    /// The member cache must be dropped, or the claims builder keeps reading the pre-suspension member
    /// and the suspension takes effect only whenever that entry happens to age out.
    /// </summary>
    /// <remarks>
    /// <b>Keys unique to this test.</b> <c>TeamServiceBase</c>'s member cache is <c>static</c>, so it is
    /// shared by every test in the run — reusing the common keys made this assertion depend on which
    /// tests had already primed it, and it failed for that reason rather than for a real one.
    /// </remarks>
    [Fact]
    public async Task Suspending_DropsTheCachedMember()
    {
        const string cacheTeam = "team-cache-probe";
        var caller = User("cache-admin");
        var userService = Substitute.For<IUserService>();
        userService.GetCurrentUserAsync().Returns(caller);

        var sut = new TestTeamService(userService);
        sut.AddTeam(cacheTeam, "Cache Probe",
            Member("cache-admin", AccessLevel.Administrator), Member("cache-u1"));

        await sut.GetTeamMemberAsync(cacheTeam, "cache-u1");   // primes the cache
        await sut.SetMemberSuspendedAsync(cacheTeam, "cache-u1", suspended: true);
        await sut.GetTeamMemberAsync(cacheTeam, "cache-u1");   // must reach the store, not the cache

        Assert.Equal(2, sut.GetTeamMembersCallCount);
    }

    /// <summary>A store that has not implemented the hook says so rather than reporting a suspension that never happened.</summary>
    [Fact]
    public async Task AStoreWithoutTheHook_Throws()
    {
        var caller = User("admin");
        var userService = Substitute.For<IUserService>();
        userService.GetCurrentUserAsync().Returns(caller);
        var sut = new TestTeamService(userService) { SimulateNoSuspendHook = true };
        sut.AddTeam(TeamKey, "Test Team", Member("admin", AccessLevel.Administrator), Member("u1"));

        await Assert.ThrowsAsync<NotSupportedException>(
            () => sut.SetMemberSuspendedAsync(TeamKey, "u1", suspended: true));
    }

    /// <summary>
    /// An invitation that has not been accepted grants no access, so there is nothing to suspend — the
    /// action there is to withdraw the invitation.
    /// </summary>
    /// <remarks>
    /// <b>Reported from the field.</b> The first version offered this and then failed with "is not a
    /// member of team", which is both wrong — they are on the roster — and useless, because it describes
    /// a different problem. The cause was the lookup: <c>GetTeamMemberAsync</c> resolves through the
    /// store's "teams I am a member of" query, which filters <c>State == Member</c>, so an invited person
    /// comes back indistinguishable from a stranger. This reads the roster directly instead.
    /// </remarks>
    [Fact]
    public async Task SuspendingAnInvitedMember_IsRefusedAndSaysWhy()
    {
        var (sut, _) = Build("admin", Member("admin", AccessLevel.Administrator), Invited("invitee"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SetMemberSuspendedAsync(TeamKey, "invitee", suspended: true));

        Assert.Contains("has not accepted the invitation", ex.Message);
        Assert.DoesNotContain("is not a member", ex.Message);
        Assert.Empty(sut.SuspendCalls);
    }

    /// <summary>A rejected invitation is the same case — no access was ever granted.</summary>
    [Fact]
    public async Task SuspendingARejectedInvitee_IsRefused()
    {
        var (sut, _) = Build("admin",
            Member("admin", AccessLevel.Administrator),
            new TestMember { Key = "nope", AccessLevel = AccessLevel.User, State = MembershipState.Rejected });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SetMemberSuspendedAsync(TeamKey, "nope", suspended: true));

        Assert.Empty(sut.SuspendCalls);
    }

    /// <summary>
    /// Restoring is refused too. An invited member cannot be suspended, so a restore is meaningless
    /// there — and silently accepting it would write a state the member never had.
    /// </summary>
    [Fact]
    public async Task RestoringAnInvitedMember_IsAlsoRefused()
    {
        var (sut, _) = Build("admin", Member("admin", AccessLevel.Administrator), Invited("invitee"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SetMemberSuspendedAsync(TeamKey, "invitee", suspended: false));

        Assert.Empty(sut.SuspendCalls);
    }

    /// <summary>
    /// A host that leaves <c>State</c> null is not thereby locked out — null has always meant an
    /// ordinary member elsewhere in this codebase (<c>TeamActivity</c> counts <c>null or Member</c>).
    /// </summary>
    [Fact]
    public async Task AMemberWithNoStateAtAll_CanStillBeSuspended()
    {
        var (sut, _) = Build("admin",
            Member("admin", AccessLevel.Administrator),
            new TestMember { Key = "u1", AccessLevel = AccessLevel.User, State = null });

        await sut.SetMemberSuspendedAsync(TeamKey, "u1", suspended: true);

        Assert.Single(sut.SuspendCalls);
    }
}
