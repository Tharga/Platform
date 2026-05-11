namespace Tharga.Team.Service.Tests;

/// <summary>
/// Tests for the resilient-member-lookup pattern applied across <see cref="TeamServiceBase"/> call sites
/// (issue Tharga/Platform#64). Each fixture seeds a team containing two rows with the same key —
/// previously these threw <c>ThrowMoreThanOneMatchException</c>; the resilient pick must complete
/// successfully without throwing.
/// </summary>
public class ResilientLookupCallSiteTests
{
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly IUser _currentUser = Substitute.For<IUser>();

    public ResilientLookupCallSiteTests()
    {
        _currentUser.Key.Returns("user-1");
        _currentUser.EMail.Returns("owner@example.com");
        _userService.GetCurrentUserAsync().Returns(_currentUser);
    }

    [Fact]
    public async Task RemoveMemberAsync_TwoRowsForSameKey_DoesNotThrow()
    {
        var sut = CreateService(
            new TestMember { Key = "user-1", AccessLevel = AccessLevel.Owner, State = MembershipState.Member },
            new TestMember { Key = "dup", AccessLevel = AccessLevel.User, State = MembershipState.Member },
            new TestMember { Key = "dup", AccessLevel = AccessLevel.User, State = MembershipState.Member });

        await sut.RemoveMemberAsync("team-1", "dup");
        // No exception = success. (Previously: ThrowMoreThanOneMatchException at TeamServiceBase.cs:123.)
    }

    [Fact]
    public async Task TransferOwnershipAsync_DuplicateCurrentOwnerRow_DoesNotThrow()
    {
        var sut = CreateService(
            new TestMember { Key = "user-1", AccessLevel = AccessLevel.Owner, State = MembershipState.Member },
            new TestMember { Key = "user-1", AccessLevel = AccessLevel.Owner, State = MembershipState.Member },
            new TestMember { Key = "user-2", AccessLevel = AccessLevel.User, State = MembershipState.Member });

        await sut.TransferOwnershipAsync<TestMember>("team-1", "user-2");
        // No exception. The duplicate user-1 row no longer trips .SingleOrDefault.
    }

    [Fact]
    public async Task TransferOwnershipAsync_DuplicateNewOwnerRow_DoesNotThrow()
    {
        var sut = CreateService(
            new TestMember { Key = "user-1", AccessLevel = AccessLevel.Owner, State = MembershipState.Member },
            new TestMember { Key = "user-2", AccessLevel = AccessLevel.User, State = MembershipState.Member },
            new TestMember { Key = "user-2", AccessLevel = AccessLevel.User, State = MembershipState.Member });

        await sut.TransferOwnershipAsync<TestMember>("team-1", "user-2");
        // No exception. The duplicate user-2 row no longer trips .SingleOrDefault.
    }

    private TestTeamService CreateService(params TestMember[] members)
    {
        var sut = new TestTeamService(_userService);
        sut.AddTeam("team-1", "Test Team", members);
        return sut;
    }
}
