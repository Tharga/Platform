namespace Tharga.Team.Service.Tests;

public class LeaveTeamTests
{
    private readonly IUserService _userService;
    private readonly IUser _currentUser;

    public LeaveTeamTests()
    {
        _userService = Substitute.For<IUserService>();
        _currentUser = Substitute.For<IUser>();
        _currentUser.Key.Returns("user-1");
        _currentUser.EMail.Returns("owner@example.com");
        _userService.GetCurrentUserAsync().Returns(_currentUser);
    }

    [Fact]
    public async Task RegularUser_CanLeaveTeam()
    {
        _currentUser.Key.Returns("user-2");
        var sut = CreateService(
            new TestMember { Key = "user-1", AccessLevel = AccessLevel.Owner, State = MembershipState.Member },
            new TestMember { Key = "user-2", AccessLevel = AccessLevel.User, State = MembershipState.Member });

        await sut.RemoveMemberAsync("team-1", "user-2");
        // No exception = success
    }

    [Fact]
    public async Task Owner_CannotLeaveTeam()
    {
        var sut = CreateService(
            new TestMember { Key = "user-1", AccessLevel = AccessLevel.Owner, State = MembershipState.Member },
            new TestMember { Key = "user-2", AccessLevel = AccessLevel.User, State = MembershipState.Member });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.RemoveMemberAsync("team-1", "user-1"));

        Assert.Contains("Transfer ownership", ex.Message);
    }

    [Fact]
    public async Task LastAdmin_CannotLeaveTeam()
    {
        _currentUser.Key.Returns("user-2");
        var sut = CreateService(
            new TestMember { Key = "user-1", AccessLevel = AccessLevel.Owner, State = MembershipState.Member },
            new TestMember { Key = "user-2", AccessLevel = AccessLevel.Administrator, State = MembershipState.Member },
            new TestMember { Key = "user-3", AccessLevel = AccessLevel.User, State = MembershipState.Member });

        // user-2 is the only admin (besides the owner) — but owner counts as admin-or-above
        // So there IS another admin-or-above (the owner). This should succeed.
        await sut.RemoveMemberAsync("team-1", "user-2");
    }

    [Fact]
    public async Task Admin_CanLeaveWhenOtherAdminExists()
    {
        _currentUser.Key.Returns("user-2");
        var sut = CreateService(
            new TestMember { Key = "user-1", AccessLevel = AccessLevel.Owner, State = MembershipState.Member },
            new TestMember { Key = "user-2", AccessLevel = AccessLevel.Administrator, State = MembershipState.Member },
            new TestMember { Key = "user-3", AccessLevel = AccessLevel.Administrator, State = MembershipState.Member });

        await sut.RemoveMemberAsync("team-1", "user-2");
        // No exception = success
    }

    [Fact]
    public async Task OwnerCanRemoveOtherMember()
    {
        var sut = CreateService(
            new TestMember { Key = "user-1", AccessLevel = AccessLevel.Owner, State = MembershipState.Member },
            new TestMember { Key = "user-2", AccessLevel = AccessLevel.User, State = MembershipState.Member });

        // Owner removing another member (not self) should always work
        await sut.RemoveMemberAsync("team-1", "user-2");
    }

    [Fact]
    public async Task TransferOwnership_Success()
    {
        var sut = CreateService(
            new TestMember { Key = "user-1", AccessLevel = AccessLevel.Owner, State = MembershipState.Member },
            new TestMember { Key = "user-2", AccessLevel = AccessLevel.User, State = MembershipState.Member });

        await sut.TransferOwnershipAsync<TestMember>("team-1", "user-2");
        // No exception = success
    }

    [Fact]
    public async Task TransferOwnership_NonOwner_Throws()
    {
        _currentUser.Key.Returns("user-2");
        var sut = CreateService(
            new TestMember { Key = "user-1", AccessLevel = AccessLevel.Owner, State = MembershipState.Member },
            new TestMember { Key = "user-2", AccessLevel = AccessLevel.Administrator, State = MembershipState.Member });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.TransferOwnershipAsync<TestMember>("team-1", "user-1"));

        Assert.Contains("Only the current owner", ex.Message);
    }

    [Fact]
    public async Task TransferOwnership_ToSelf_Throws()
    {
        var sut = CreateService(
            new TestMember { Key = "user-1", AccessLevel = AccessLevel.Owner, State = MembershipState.Member },
            new TestMember { Key = "user-2", AccessLevel = AccessLevel.User, State = MembershipState.Member });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.TransferOwnershipAsync<TestMember>("team-1", "user-1"));

        Assert.Contains("Cannot transfer ownership to yourself", ex.Message);
    }

    [Fact]
    public async Task TransferOwnership_ToNonMember_Throws()
    {
        var sut = CreateService(
            new TestMember { Key = "user-1", AccessLevel = AccessLevel.Owner, State = MembershipState.Member });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.TransferOwnershipAsync<TestMember>("team-1", "user-99"));

        Assert.Contains("not a member", ex.Message);
    }

    private TestTeamService CreateService(params TestMember[] members)
    {
        var sut = new TestTeamService(_userService);
        sut.AddTeam("team-1", "Test Team", members);
        return sut;
    }
}
