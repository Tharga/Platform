namespace Tharga.Team.Service.Tests;

public class SetInvitationResponseSeedTests
{
    private readonly IUserService _userService;
    private readonly TestTeamService _sut;

    public SetInvitationResponseSeedTests()
    {
        _userService = Substitute.For<IUserService>();
        var user = Substitute.For<IUser>();
        user.Key.Returns("invitee-key");
        user.EMail.Returns("invitee@example.com");
        _userService.GetCurrentUserAsync().Returns(user);

        _sut = new TestTeamService(_userService);
    }

    [Fact]
    public async Task SetInvitationResponseAsync_Accept_WithMemberName_CallsSeedUserNameAsync()
    {
        _sut.AddTeam("team-1", "Test Team",
            new TestMember
            {
                Key = "tmp-key",
                Name = "Alice from Invite",
                Invitation = new Invitation { EMail = "invitee@example.com", InviteKey = "inv-1", InviteTime = DateTime.UtcNow },
                State = MembershipState.Invited,
                AccessLevel = AccessLevel.User
            });

        await _sut.SetInvitationResponseAsync("team-1", "invitee-key", "inv-1", true);

        await _userService.Received(1).SeedUserNameAsync("invitee-key", "Alice from Invite");
    }

    [Fact]
    public async Task SetInvitationResponseAsync_Accept_WithEmptyMemberName_DoesNotCallSeedUserNameAsync()
    {
        _sut.AddTeam("team-2", "Test Team",
            new TestMember
            {
                Key = "tmp-key-2",
                Name = null,
                Invitation = new Invitation { EMail = "invitee@example.com", InviteKey = "inv-2", InviteTime = DateTime.UtcNow },
                State = MembershipState.Invited,
                AccessLevel = AccessLevel.User
            });

        await _sut.SetInvitationResponseAsync("team-2", "invitee-key", "inv-2", true);

        await _userService.DidNotReceive().SeedUserNameAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task SetInvitationResponseAsync_Accept_WithWhitespaceMemberName_DoesNotCallSeedUserNameAsync()
    {
        _sut.AddTeam("team-3", "Test Team",
            new TestMember
            {
                Key = "tmp-key-3",
                Name = "   ",
                Invitation = new Invitation { EMail = "invitee@example.com", InviteKey = "inv-3", InviteTime = DateTime.UtcNow },
                State = MembershipState.Invited,
                AccessLevel = AccessLevel.User
            });

        await _sut.SetInvitationResponseAsync("team-3", "invitee-key", "inv-3", true);

        await _userService.DidNotReceive().SeedUserNameAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task SetInvitationResponseAsync_Reject_DoesNotCallSeedUserNameAsync()
    {
        _sut.AddTeam("team-4", "Test Team",
            new TestMember
            {
                Key = "tmp-key-4",
                Name = "Alice from Invite",
                Invitation = new Invitation { EMail = "invitee@example.com", InviteKey = "inv-4", InviteTime = DateTime.UtcNow },
                State = MembershipState.Invited,
                AccessLevel = AccessLevel.User
            });

        await _sut.SetInvitationResponseAsync("team-4", "invitee-key", "inv-4", false);

        await _userService.DidNotReceive().SeedUserNameAsync(Arg.Any<string>(), Arg.Any<string>());
    }
}
