namespace Tharga.Team.Service.Tests;

public class SetMemberNameTests
{
    private readonly TestTeamService _sut;

    public SetMemberNameTests()
    {
        var userService = Substitute.For<IUserService>();
        var user = Substitute.For<IUser>();
        user.Key.Returns("user-1");
        userService.GetCurrentUserAsync().Returns(user);

        _sut = new TestTeamService(userService);
        _sut.AddTeam("team-1", "Test Team",
            new TestMember { Key = "user-1", AccessLevel = AccessLevel.Owner, State = MembershipState.Member },
            new TestMember { Key = "user-2", Name = "Old Name", AccessLevel = AccessLevel.User, State = MembershipState.Member });
    }

    [Fact]
    public async Task SetMemberNameAsync_Calls_Protected_Method_With_New_Name()
    {
        await _sut.SetMemberNameAsync("team-1", "user-2", "New Name");

        Assert.Equal(1, _sut.SetMemberNameCallCount);
        Assert.Equal("team-1", _sut.LastSetMemberName_TeamKey);
        Assert.Equal("user-2", _sut.LastSetMemberName_UserKey);
        Assert.Equal("New Name", _sut.LastSetMemberName_Name);
    }

    [Fact]
    public async Task SetMemberNameAsync_Passes_Null_Through_To_Clear_The_Override()
    {
        await _sut.SetMemberNameAsync("team-1", "user-2", null);

        Assert.Equal(1, _sut.SetMemberNameCallCount);
        Assert.Null(_sut.LastSetMemberName_Name);
    }

    [Fact]
    public async Task SetMemberNameAsync_Fires_TeamsListChangedEvent()
    {
        var fired = false;
        _sut.TeamsListChangedEvent += (_, _) => fired = true;

        await _sut.SetMemberNameAsync("team-1", "user-2", "Updated");

        Assert.True(fired);
    }
}
