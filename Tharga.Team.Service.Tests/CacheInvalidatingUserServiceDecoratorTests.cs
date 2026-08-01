using System.Security.Claims;
using Tharga.Team;
using Tharga.Team.Service;

namespace Tharga.Team.Service.Tests;

/// <summary>
/// The cache belongs to the toolkit and the writes do not: a host overriding a persistence member
/// replaces the path that invalidated, so the write commits and every later read is served stale. These
/// pin that the decorator invalidates whoever implemented the write.
/// </summary>
public class CacheInvalidatingUserServiceDecoratorTests
{
    private sealed record FakeUser(string Key, string Identity, string Name) : IUser
    {
        public string EMail => null;
        public string DirectoryId => null;
        public string Icon => null;
        public DateTime? LastSeen => null;
    }

    /// <summary>
    /// Stands in for a host that overrode persistence and therefore never invalidates. Records what the
    /// decorator asked it to drop.
    /// </summary>
    private sealed class HostStore : IUserService, IUserCacheInvalidator
    {
        public List<string> Invalidated { get; } = [];
        public IUser Current { get; set; }

        public void InvalidateUserByKey(string userKey) => Invalidated.Add(userKey);

        public Task<IUser> GetCurrentUserAsync(ClaimsPrincipal claimsPrincipal = null) => Task.FromResult(Current);
        public IAsyncEnumerable<IUser> GetAsync() => AsyncEnumerable.Empty<IUser>();
        public Task SeedUserNameAsync(string userKey, string name) => Task.CompletedTask;
        public Task SetUserNameAsync(string userKey, string name) => Task.CompletedTask;
        public Task SetUserDirectoryIdAsync(string userKey, string directoryId) => Task.CompletedTask;
        public Task SetUserIconAsync(string userKey, byte[] data, string contentType) => Task.CompletedTask;
        public Task ClearUserIconAsync(string userKey) => Task.CompletedTask;
        public Task DeleteUserAsync(string userKey) => Task.CompletedTask;
        public Task SetOwnIconAsync(byte[] data, string contentType) => Task.CompletedTask;
        public Task ClearOwnIconAsync() => Task.CompletedTask;
    }

    /// <summary>A store written from scratch, with no cache to drop.</summary>
    private sealed class StoreWithoutCache : IUserService
    {
        public Task<IUser> GetCurrentUserAsync(ClaimsPrincipal claimsPrincipal = null) => Task.FromResult<IUser>(null);
        public IAsyncEnumerable<IUser> GetAsync() => AsyncEnumerable.Empty<IUser>();
        public Task SeedUserNameAsync(string userKey, string name) => Task.CompletedTask;
        public Task SetUserNameAsync(string userKey, string name) => Task.CompletedTask;
    }

    /// <summary>The exact case PlutusWave hit: a rename that commits and then reads back stale.</summary>
    [Fact]
    public async Task SetUserName_InvalidatesEvenWhenTheHostOverrodePersistence()
    {
        var host = new HostStore();
        var sut = new CacheInvalidatingUserServiceDecorator(host);

        await sut.SetUserNameAsync("user-1", "New Name");

        Assert.Equal(["user-1"], host.Invalidated);
    }

    [Theory]
    [InlineData("seed")]
    [InlineData("directory")]
    [InlineData("icon")]
    [InlineData("clear-icon")]
    [InlineData("delete")]
    public async Task EveryKeyedMutation_Invalidates(string operation)
    {
        var host = new HostStore();
        var sut = new CacheInvalidatingUserServiceDecorator(host);

        Task call = operation switch
        {
            "seed" => sut.SeedUserNameAsync("user-1", "n"),
            "directory" => sut.SetUserDirectoryIdAsync("user-1", "oid"),
            "icon" => sut.SetUserIconAsync("user-1", [1], "image/png"),
            "clear-icon" => sut.ClearUserIconAsync("user-1"),
            _ => sut.DeleteUserAsync("user-1")
        };
        await call;

        Assert.Equal(["user-1"], host.Invalidated);
    }

    /// <summary>The self-service icon members take no key, so the caller is resolved to find one.</summary>
    [Fact]
    public async Task OwnIcon_InvalidatesTheCaller()
    {
        var host = new HostStore { Current = new FakeUser("user-7", "id-7", "Me") };
        var sut = new CacheInvalidatingUserServiceDecorator(host);

        await sut.SetOwnIconAsync([1], "image/png");
        await sut.ClearOwnIconAsync();

        Assert.Equal(["user-7", "user-7"], host.Invalidated);
    }

    /// <summary>
    /// Runs on every authenticated resolve (throttled). Invalidating here would empty the cache
    /// continuously and defeat the thing this decorator exists to keep correct.
    /// </summary>
    [Fact]
    public async Task SetUserLastSeen_DoesNotInvalidate()
    {
        var host = new HostStore();
        var sut = new CacheInvalidatingUserServiceDecorator(host);

        await sut.SetUserLastSeenAsync("user-1", DateTime.UtcNow);

        Assert.Empty(host.Invalidated);
    }

    /// <summary>
    /// A failed write changed nothing. Dropping a valid entry for it would trade a stale read for a
    /// needless one.
    /// </summary>
    [Fact]
    public async Task AThrowingWrite_DoesNotInvalidate()
    {
        var host = new ThrowingStore();
        var sut = new CacheInvalidatingUserServiceDecorator(host);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SetUserNameAsync("user-1", "n"));

        Assert.Empty(host.Invalidated);
    }

    /// <summary>A store with no cache is passed through, not rejected.</summary>
    [Fact]
    public async Task StoreWithoutCache_IsAPassThrough()
    {
        var sut = new CacheInvalidatingUserServiceDecorator(new StoreWithoutCache());

        await sut.SetUserNameAsync("user-1", "n");
    }

    private sealed class ThrowingStore : HostStoreBase
    {
        public override Task SetUserNameAsync(string userKey, string name)
            => throw new InvalidOperationException("write failed");
    }

    private class HostStoreBase : IUserService, IUserCacheInvalidator
    {
        public List<string> Invalidated { get; } = [];
        public void InvalidateUserByKey(string userKey) => Invalidated.Add(userKey);
        public Task<IUser> GetCurrentUserAsync(ClaimsPrincipal claimsPrincipal = null) => Task.FromResult<IUser>(null);
        public IAsyncEnumerable<IUser> GetAsync() => AsyncEnumerable.Empty<IUser>();
        public Task SeedUserNameAsync(string userKey, string name) => Task.CompletedTask;
        public virtual Task SetUserNameAsync(string userKey, string name) => Task.CompletedTask;
    }
}
