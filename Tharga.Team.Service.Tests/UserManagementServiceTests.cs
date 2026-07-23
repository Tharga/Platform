using Tharga.Team;

namespace Tharga.Team.Service.Tests;

/// <summary>
/// Behavior of <see cref="UserManagementService"/>: verification (with email-fallback relink), deletion
/// (all-team removal + record delete always complete; directory delete is opt-in and its failure is
/// reported, never rolled back), and the directory-only diff (matched by directory id, then email).
/// </summary>
public class UserManagementServiceTests
{
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly ITeamService _teamService = Substitute.For<ITeamService>();
    private readonly IUserDirectoryService _directory = Substitute.For<IUserDirectoryService>();

    private static TestUser User(string key, string email = null, string directoryId = null)
        => new() { Key = key, Identity = $"id-{key}", EMail = email, DirectoryId = directoryId };

    private UserManagementService BuildWithDirectory() => new(_userService, _teamService, _directory);
    private UserManagementService BuildWithoutDirectory() => new(_userService, _teamService);

    // ---- VerifyUserAsync ----

    [Fact]
    public async Task Verify_NoDirectoryService_Throws()
    {
        var sut = BuildWithoutDirectory();
        await Assert.ThrowsAsync<NotSupportedException>(() => sut.VerifyUserAsync("u-1"));
    }

    [Fact]
    public async Task Verify_UnknownUser_Throws()
    {
        _userService.GetUserByKeyAsync("u-gone").Returns((IUser)null);
        var sut = BuildWithDirectory();
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.VerifyUserAsync("u-gone"));
    }

    [Fact]
    public async Task Verify_Found_ReturnsResult()
    {
        var user = User("u-1", directoryId: "oid-1");
        _userService.GetUserByKeyAsync("u-1").Returns(user);
        _directory.VerifyUserAsync(user, Arg.Any<CancellationToken>())
            .Returns(new DirectoryVerificationResult(DirectoryUserStatus.Found, "oid-1"));

        var result = await BuildWithDirectory().VerifyUserAsync("u-1");

        Assert.Equal(DirectoryUserStatus.Found, result.Status);
        await _userService.DidNotReceiveWithAnyArgs().SetUserDirectoryIdAsync(default, default);
    }

    [Fact]
    public async Task Verify_EmailFallbackResolved_RelinksDirectoryId()
    {
        var user = User("u-1", email: "a@b.c");
        _userService.GetUserByKeyAsync("u-1").Returns(user);
        _directory.VerifyUserAsync(user, Arg.Any<CancellationToken>())
            .Returns(new DirectoryVerificationResult(DirectoryUserStatus.Found, "oid-found"));

        await BuildWithDirectory().VerifyUserAsync("u-1");

        await _userService.Received(1).SetUserDirectoryIdAsync("u-1", "oid-found");
    }

    [Fact]
    public async Task Verify_NotFound_DoesNotRelink()
    {
        var user = User("u-1", email: "a@b.c");
        _userService.GetUserByKeyAsync("u-1").Returns(user);
        _directory.VerifyUserAsync(user, Arg.Any<CancellationToken>())
            .Returns(new DirectoryVerificationResult(DirectoryUserStatus.NotFound));

        var result = await BuildWithDirectory().VerifyUserAsync("u-1");

        Assert.Equal(DirectoryUserStatus.NotFound, result.Status);
        await _userService.DidNotReceiveWithAnyArgs().SetUserDirectoryIdAsync(default, default);
    }

    // ---- VerifyAllAsync ----

    [Fact]
    public async Task VerifyAll_StreamsResultForEveryUser()
    {
        var users = new IUser[] { User("u-1", directoryId: "oid-1"), User("u-2", email: "b@b.c") };
        _userService.GetAsync().Returns(users.ToAsyncEnumerable());
        _directory.VerifyUserAsync(users[0], Arg.Any<CancellationToken>())
            .Returns(new DirectoryVerificationResult(DirectoryUserStatus.Found, "oid-1"));
        _directory.VerifyUserAsync(users[1], Arg.Any<CancellationToken>())
            .Returns(new DirectoryVerificationResult(DirectoryUserStatus.NotFound));

        var results = await BuildWithDirectory().VerifyAllAsync().ToListAsync();

        Assert.Equal(2, results.Count);
        Assert.Equal("u-1", results[0].UserKey);
        Assert.Equal(DirectoryUserStatus.Found, results[0].Result.Status);
        Assert.Equal("u-2", results[1].UserKey);
        Assert.Equal(DirectoryUserStatus.NotFound, results[1].Result.Status);
    }

    // ---- DeleteUserAsync ----

    [Fact]
    public async Task Delete_LocalOnly_RemovesTeamsAndRecord()
    {
        var user = User("u-1");
        _userService.GetUserByKeyAsync("u-1").Returns(user);
        _teamService.RemoveUserFromAllTeamsAsync("u-1").Returns(3);

        var result = await BuildWithDirectory().DeleteUserAsync("u-1");

        Assert.False(result.DirectoryDeleted);
        Assert.Null(result.DirectoryError);
        Assert.Equal(3, result.RemovedTeamCount);
        await _teamService.Received(1).RemoveUserFromAllTeamsAsync("u-1");
        await _userService.Received(1).DeleteUserAsync("u-1");
        await _directory.DidNotReceiveWithAnyArgs().DeleteUserAsync(default, default);
    }

    [Fact]
    public async Task Delete_UnknownUser_Throws()
    {
        _userService.GetUserByKeyAsync("u-gone").Returns((IUser)null);
        await Assert.ThrowsAsync<InvalidOperationException>(() => BuildWithDirectory().DeleteUserAsync("u-gone"));
    }

    [Fact]
    public async Task Delete_WithDirectory_LinkedUser_DeletesInDirectory()
    {
        var user = User("u-1", directoryId: "oid-1");
        _userService.GetUserByKeyAsync("u-1").Returns(user);
        _teamService.RemoveUserFromAllTeamsAsync("u-1").Returns(1);

        var result = await BuildWithDirectory().DeleteUserAsync("u-1", deleteFromDirectory: true);

        Assert.True(result.DirectoryDeleted);
        Assert.Null(result.DirectoryError);
        await _directory.Received(1).DeleteUserAsync("oid-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_WithDirectory_UnlinkedUser_ResolvesViaVerifyBeforeLocalDelete()
    {
        var user = User("u-1", email: "a@b.c");
        _userService.GetUserByKeyAsync("u-1").Returns(user);
        _directory.VerifyUserAsync(user, Arg.Any<CancellationToken>())
            .Returns(new DirectoryVerificationResult(DirectoryUserStatus.Found, "oid-resolved"));

        var result = await BuildWithDirectory().DeleteUserAsync("u-1", deleteFromDirectory: true);

        Assert.True(result.DirectoryDeleted);
        await _directory.Received(1).DeleteUserAsync("oid-resolved", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_WithDirectory_Unresolvable_DeletesLocallyAndReportsError()
    {
        var user = User("u-1", email: "a@b.c");
        _userService.GetUserByKeyAsync("u-1").Returns(user);
        _directory.VerifyUserAsync(user, Arg.Any<CancellationToken>())
            .Returns(new DirectoryVerificationResult(DirectoryUserStatus.NotLinked));

        var result = await BuildWithDirectory().DeleteUserAsync("u-1", deleteFromDirectory: true);

        Assert.False(result.DirectoryDeleted);
        Assert.NotNull(result.DirectoryError);
        await _userService.Received(1).DeleteUserAsync("u-1");
        await _directory.DidNotReceiveWithAnyArgs().DeleteUserAsync(default, default);
    }

    [Fact]
    public async Task Delete_DirectoryDeleteFails_LocalDeleteStandsAndErrorReported()
    {
        var user = User("u-1", directoryId: "oid-1");
        _userService.GetUserByKeyAsync("u-1").Returns(user);
        _directory.DeleteUserAsync("oid-1", Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("graph says no"));

        var result = await BuildWithDirectory().DeleteUserAsync("u-1", deleteFromDirectory: true);

        Assert.False(result.DirectoryDeleted);
        Assert.Equal("graph says no", result.DirectoryError);
        await _userService.Received(1).DeleteUserAsync("u-1");
    }

    [Fact]
    public async Task Delete_WithDirectoryRequested_NoDirectoryService_DeletesLocallyAndReportsError()
    {
        var user = User("u-1");
        _userService.GetUserByKeyAsync("u-1").Returns(user);

        var result = await BuildWithoutDirectory().DeleteUserAsync("u-1", deleteFromDirectory: true);

        Assert.False(result.DirectoryDeleted);
        Assert.NotNull(result.DirectoryError);
        await _userService.Received(1).DeleteUserAsync("u-1");
    }

    // ---- GetDirectoryOnlyUsersAsync ----

    [Fact]
    public async Task DirectoryOnly_NoDirectoryService_Throws()
    {
        var sut = BuildWithoutDirectory();
        await Assert.ThrowsAsync<NotSupportedException>(async () => await sut.GetDirectoryOnlyUsersAsync().ToListAsync());
    }

    [Fact]
    public async Task DirectoryOnly_FiltersByDirectoryIdAndEmailFallback()
    {
        var locals = new IUser[]
        {
            User("u-1", directoryId: "oid-linked"),
            User("u-2", email: "Legacy@Example.com")
        };
        _userService.GetAsync().Returns(locals.ToAsyncEnumerable());
        var directoryUsers = new[]
        {
            new DirectoryUser("oid-linked", "Linked", "linked@example.com", true),
            new DirectoryUser("oid-legacy", "Legacy", "legacy@example.com", true),
            new DirectoryUser("oid-new", "Only In Entra", "new@example.com", false)
        };
        _directory.GetUsersAsync(Arg.Any<CancellationToken>()).Returns(directoryUsers.ToAsyncEnumerable());

        var result = await BuildWithDirectory().GetDirectoryOnlyUsersAsync().ToListAsync();

        var only = Assert.Single(result);
        Assert.Equal("oid-new", only.DirectoryId);
    }

    private sealed record TestUser : IUser
    {
        public string Key { get; init; }
        public string Identity { get; init; }
        public string EMail { get; init; }
        public string Name { get; init; }
        public string DirectoryId { get; init; }
    }
}
