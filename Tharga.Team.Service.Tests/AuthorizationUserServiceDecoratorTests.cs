using System.Security.Claims;
using Tharga.Team;

namespace Tharga.Team.Service.Tests;

/// <summary>
/// Authorization matrix for <see cref="AuthorizationUserServiceDecorator"/>: self-service operations
/// (current user, invitation name seeding) pass through for any caller; setting a display name is
/// allowed on the caller's own record; everything that reads or mutates other users' records —
/// enumeration, by-key reads, activity/directory writes, deletion — requires <c>users:manage</c> and
/// is denied before the inner service is touched.
/// </summary>
public class AuthorizationUserServiceDecoratorTests
{
    private static (AuthorizationUserServiceDecorator Sut, IUserService Inner) Build(ClaimsPrincipal principal)
    {
        var inner = Substitute.For<IUserService>();
        var accessor = Substitute.For<ITeamPrincipalAccessor>();
        accessor.GetCurrentAsync().Returns(new ValueTask<ClaimsPrincipal>(principal));
        return (new AuthorizationUserServiceDecorator(inner, new TeamAuthorizer(accessor)), inner);
    }

    private static ClaimsPrincipal WithScopes(params string[] scopes)
        => new(new ClaimsIdentity(scopes.Select(s => new Claim(TeamClaimTypes.Scope, s)), "Test"));

    private static ClaimsPrincipal Anonymous() => new(new ClaimsIdentity());

    private sealed record TestUser : IUser
    {
        public string Key { get; init; }
        public string Identity { get; init; }
        public string EMail { get; init; }
    }

    // ---- Self-service pass-through ----

    [Fact]
    public async Task GetCurrentUser_Anonymous_PassesThrough()
    {
        var (sut, inner) = Build(Anonymous());
        await sut.GetCurrentUserAsync();
        await inner.Received(1).GetCurrentUserAsync();
    }

    [Fact]
    public async Task SeedUserName_WithoutScope_PassesThrough()
    {
        var (sut, inner) = Build(Anonymous());
        await sut.SeedUserNameAsync("u-1", "Alice");
        await inner.Received(1).SeedUserNameAsync("u-1", "Alice");
    }

    // ---- SetUserNameAsync: self or users:manage ----

    [Fact]
    public async Task SetUserName_OwnRecord_WithoutScope_Delegates()
    {
        var (sut, inner) = Build(Anonymous());
        inner.GetCurrentUserAsync().Returns(new TestUser { Key = "u-me" });

        await sut.SetUserNameAsync("u-me", "New Name");

        await inner.Received(1).SetUserNameAsync("u-me", "New Name");
    }

    [Fact]
    public async Task SetUserName_OtherRecord_WithoutScope_Throws()
    {
        var (sut, inner) = Build(Anonymous());
        inner.GetCurrentUserAsync().Returns(new TestUser { Key = "u-me" });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.SetUserNameAsync("u-other", "X"));
        await inner.DidNotReceiveWithAnyArgs().SetUserNameAsync(default, default);
    }

    [Fact]
    public async Task SetUserName_OtherRecord_WithScope_Delegates()
    {
        var (sut, inner) = Build(WithScopes(SystemUserScopes.Manage));
        inner.GetCurrentUserAsync().Returns(new TestUser { Key = "u-me" });

        await sut.SetUserNameAsync("u-other", "X");

        await inner.Received(1).SetUserNameAsync("u-other", "X");
    }

    // ---- Administration requires users:manage ----

    [Fact]
    public async Task GetAsync_WithScope_Streams()
    {
        var (sut, inner) = Build(WithScopes(SystemUserScopes.Manage));
        inner.GetAsync().Returns(new IUser[] { new TestUser { Key = "u-1" } }.ToAsyncEnumerable());

        var users = await sut.GetAsync().ToListAsync();

        Assert.Single(users);
    }

    [Fact]
    public async Task GetAsync_WithoutScope_ThrowsBeforeEnumerating()
    {
        var (sut, inner) = Build(WithScopes(TeamScopes.Manage));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await sut.GetAsync().ToListAsync());
        _ = inner.DidNotReceiveWithAnyArgs().GetAsync();
    }

    [Fact]
    public async Task GetUserByKey_WithoutScope_Throws()
    {
        var (sut, inner) = Build(Anonymous());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.GetUserByKeyAsync("u-1"));
        await inner.DidNotReceiveWithAnyArgs().GetUserByKeyAsync(default);
    }

    [Fact]
    public async Task GetUserByKey_WithScope_Delegates()
    {
        var (sut, inner) = Build(WithScopes(SystemUserScopes.Manage));

        await sut.GetUserByKeyAsync("u-1");

        await inner.Received(1).GetUserByKeyAsync("u-1");
    }

    [Fact]
    public async Task SetUserLastSeen_WithoutScope_Throws()
    {
        var (sut, inner) = Build(Anonymous());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.SetUserLastSeenAsync("u-1", DateTime.UtcNow));
        await inner.DidNotReceiveWithAnyArgs().SetUserLastSeenAsync(default, default);
    }

    [Fact]
    public async Task SetUserDirectoryId_WithoutScope_Throws()
    {
        var (sut, inner) = Build(Anonymous());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.SetUserDirectoryIdAsync("u-1", "oid"));
        await inner.DidNotReceiveWithAnyArgs().SetUserDirectoryIdAsync(default, default);
    }

    [Fact]
    public async Task DeleteUser_WithoutScope_Throws()
    {
        var (sut, inner) = Build(Anonymous());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.DeleteUserAsync("u-1"));
        await inner.DidNotReceiveWithAnyArgs().DeleteUserAsync(default);
    }

    [Fact]
    public async Task DeleteUser_WithScope_Delegates()
    {
        var (sut, inner) = Build(WithScopes(SystemUserScopes.Manage));

        await sut.DeleteUserAsync("u-1");

        await inner.Received(1).DeleteUserAsync("u-1");
    }
}
