using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Tharga.MongoDB;

namespace Tharga.Team.MongoDB.Tests;

/// <summary>
/// Verifies the directory/activity overrides on <see cref="UserServiceRepositoryBase{TUserEntity}"/>:
/// last-seen stamping delegates straight to the repository (no user read on the hot path), while
/// directory-id linking and deletion read the user first and no-op when the user is gone.
/// </summary>
public class UserServiceRepositoryBaseDirectoryTests
{
    private readonly IUserRepository<TestUserEntity> _repo = Substitute.For<IUserRepository<TestUserEntity>>();
    private readonly TestUserService _sut;

    public UserServiceRepositoryBaseDirectoryTests()
    {
        _sut = new TestUserService(_repo);
    }

    [Fact]
    public async Task SetUserLastSeenAsync_DelegatesToRepository()
    {
        var lastSeen = new DateTime(2026, 7, 23, 12, 0, 0, DateTimeKind.Utc);

        await _sut.SetUserLastSeenAsync("u-1", lastSeen);

        await _repo.Received(1).SetLastSeenAsync("u-1", lastSeen);
        await _repo.DidNotReceiveWithAnyArgs().GetByKeyAsync(default);
    }

    [Fact]
    public async Task SetUserLastSeenAsync_EmptyKey_NoOp()
    {
        await _sut.SetUserLastSeenAsync(null, DateTime.UtcNow);

        await _repo.DidNotReceiveWithAnyArgs().SetLastSeenAsync(default, default);
    }

    [Fact]
    public async Task SetUserDirectoryIdAsync_UserFound_Writes()
    {
        _repo.GetByKeyAsync("u-1").Returns(new TestUserEntity { Key = "u-1", Identity = "id-1" });

        await _sut.SetUserDirectoryIdAsync("u-1", "oid-1");

        await _repo.Received(1).SetDirectoryIdAsync("u-1", "oid-1");
    }

    [Fact]
    public async Task SetUserDirectoryIdAsync_UserMissing_NoWrite()
    {
        _repo.GetByKeyAsync("u-gone").Returns((TestUserEntity)null);

        await _sut.SetUserDirectoryIdAsync("u-gone", "oid-1");

        await _repo.DidNotReceiveWithAnyArgs().SetDirectoryIdAsync(default, default);
    }

    [Fact]
    public async Task DeleteUserAsync_UserFound_Deletes()
    {
        _repo.GetByKeyAsync("u-1").Returns(new TestUserEntity { Key = "u-1", Identity = "id-1" });

        await _sut.DeleteUserAsync("u-1");

        await _repo.Received(1).DeleteAsync("u-1");
    }

    [Fact]
    public async Task DeleteUserAsync_UserMissing_NoDelete()
    {
        _repo.GetByKeyAsync("u-gone").Returns((TestUserEntity)null);

        await _sut.DeleteUserAsync("u-gone");

        await _repo.DidNotReceiveWithAnyArgs().DeleteAsync(default);
    }

    public record TestUserEntity : EntityBase, IUser
    {
        public string Key { get; init; }
        public string Identity { get; init; }
        public string EMail { get; init; }
    }

    private sealed class TestUserService : UserServiceRepositoryBase<TestUserEntity>
    {
        public TestUserService(IUserRepository<TestUserEntity> repo)
            : base(Substitute.For<AuthenticationStateProvider>(), repo)
        {
        }

        protected override Task<TestUserEntity> CreateUserEntityAsync(ClaimsPrincipal claimsPrincipal, string identity)
            => Task.FromResult(new TestUserEntity { Identity = identity, Key = Guid.NewGuid().ToString() });
    }
}
