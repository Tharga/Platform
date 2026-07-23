using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Tharga.Team;

namespace Tharga.Team.Service.Tests;

/// <summary>
/// User icons: the built-in <see cref="GravatarIconSource"/> (users with email → Gravatar; teams/no-email
/// → null) and the self-service <see cref="UserServiceBase.SetOwnIconAsync"/> / <c>ClearOwnIconAsync</c>
/// orchestration (store → persist reference → delete previous; NotSupported without a store; Unauthorized
/// without a current user).
/// </summary>
public class UserIconTests
{
    // ---- GravatarIconSource ----

    [Fact]
    public async Task Gravatar_UserWithEmail_ReturnsGravatarUrl()
    {
        var subject = new IconSubject { Kind = IconKind.User, Key = "u1", EMail = "Test@Example.com" };
        var image = await new GravatarIconSource().ResolveAsync(subject);

        Assert.NotNull(image);
        // md5 of the trimmed lower-cased email.
        Assert.Contains("gravatar.com/avatar/55502f40dc8b7c769880b10874abc9d0", image.Url);
    }

    [Fact]
    public async Task Gravatar_Team_ReturnsNull()
    {
        var subject = new IconSubject { Kind = IconKind.Team, Key = "t1", EMail = "team@example.com" };
        Assert.Null(await new GravatarIconSource().ResolveAsync(subject));
    }

    [Fact]
    public async Task Gravatar_UserWithoutEmail_ReturnsNull()
    {
        var subject = new IconSubject { Kind = IconKind.User, Key = "u1" };
        Assert.Null(await new GravatarIconSource().ResolveAsync(subject));
    }

    // ---- Self-service orchestration ----

    private sealed record TestUser : IUser
    {
        public string Key { get; init; }
        public string Identity { get; init; }
        public string EMail { get; init; }
        public string Icon { get; init; }
    }

    private sealed class FakeAsp(string identity) : AuthenticationStateProvider
    {
        private readonly ClaimsPrincipal _principal = new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, identity)], "test"));
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(_principal));
    }

    private sealed class IconTestUserService : UserServiceBase
    {
        private readonly IUser _current;
        public string SetReference { get; private set; }
        public bool SetReferenceCalled { get; private set; }

        public IconTestUserService(IIconStore store, IUser current, string identity)
            : base(new FakeAsp(identity), iconStore: store)
        {
            _current = current;
        }

        protected override TimeSpan? LastSeenStampInterval => null;
        protected override Task<IUser> GetUserAsync(ClaimsPrincipal claimsPrincipal) => Task.FromResult(_current);
        protected override async IAsyncEnumerable<IUser> GetAllAsync() { yield break; }

        protected override Task SetUserIconReferenceAsync(string userKey, string reference)
        {
            SetReferenceCalled = true;
            SetReference = reference;
            return Task.CompletedTask;
        }
    }

    private static (IconTestUserService Sut, IIconStore Store) Build(IUser current)
    {
        var identity = $"id-{Guid.NewGuid():N}";
        current = ((TestUser)current) with { Identity = identity };
        var store = Substitute.For<IIconStore>();
        return (new IconTestUserService(store, current, identity), store);
    }

    [Fact]
    public async Task SetOwnIcon_StoresPersistsReference_DeletesPrevious()
    {
        var (sut, store) = Build(new TestUser { Key = "u1", EMail = "a@b.c", Icon = "old-ref" });
        store.SaveAsync(IconKind.User, "u1", Arg.Any<byte[]>(), "image/png").Returns("new-ref");

        await sut.SetOwnIconAsync([1, 2], "image/png");

        Assert.Equal("new-ref", sut.SetReference);
        await store.Received(1).SaveAsync(IconKind.User, "u1", Arg.Any<byte[]>(), "image/png");
        await store.Received(1).DeleteAsync("old-ref");
    }

    [Fact]
    public async Task SetOwnIcon_NoPrevious_DoesNotDelete()
    {
        var (sut, store) = Build(new TestUser { Key = "u1", EMail = "a@b.c", Icon = null });
        store.SaveAsync(IconKind.User, "u1", Arg.Any<byte[]>(), "image/png").Returns("new-ref");

        await sut.SetOwnIconAsync([1], "image/png");

        Assert.Equal("new-ref", sut.SetReference);
        await store.DidNotReceiveWithAnyArgs().DeleteAsync(default);
    }

    [Fact]
    public async Task ClearOwnIcon_WithIcon_ClearsAndDeletes()
    {
        var (sut, store) = Build(new TestUser { Key = "u1", EMail = "a@b.c", Icon = "ref-1" });

        await sut.ClearOwnIconAsync();

        Assert.True(sut.SetReferenceCalled);
        Assert.Null(sut.SetReference);
        await store.Received(1).DeleteAsync("ref-1");
    }

    [Fact]
    public async Task ClearOwnIcon_NoIcon_NoOp()
    {
        var (sut, store) = Build(new TestUser { Key = "u1", EMail = "a@b.c", Icon = null });

        await sut.ClearOwnIconAsync();

        Assert.False(sut.SetReferenceCalled);
        await store.DidNotReceiveWithAnyArgs().DeleteAsync(default);
    }

    [Fact]
    public async Task SetOwnIcon_NoStore_ThrowsNotSupported()
    {
        var identity = $"id-{Guid.NewGuid():N}";
        var sut = new IconTestUserService(null, new TestUser { Key = "u1", Identity = identity }, identity);
        await Assert.ThrowsAsync<NotSupportedException>(() => sut.SetOwnIconAsync([1], "image/png"));
    }

    [Fact]
    public async Task SetOwnIcon_NoCurrentUser_ThrowsUnauthorized()
    {
        var identity = $"id-{Guid.NewGuid():N}";
        var sut = new IconTestUserService(Substitute.For<IIconStore>(), null, identity);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.SetOwnIconAsync([1], "image/png"));
    }
}
