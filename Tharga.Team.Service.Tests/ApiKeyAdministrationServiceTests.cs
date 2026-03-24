using Tharga.Toolkit.Password;

namespace Tharga.Team.Service.Tests;

public class ApiKeyAdministrationServiceTests
{
    private readonly IApiKeyRepository _repository = Substitute.For<IApiKeyRepository>();
    private readonly IApiKeyService _apiKeyService = Substitute.For<IApiKeyService>();
    private readonly ApiKeyAdministrationService _sut;

    public ApiKeyAdministrationServiceTests()
    {
        _sut = new ApiKeyAdministrationService(_repository, _apiKeyService);
    }

    [Fact]
    public async Task GetByApiKeyAsync_With_Matching_Hash_Returns_Key()
    {
        var entity = CreateEntity("key-1", "hash-1", "team-1");
        _repository.GetAsync().Returns(ToAsyncEnumerable(entity));
        _apiKeyService.Verify("raw-key", "hash-1").Returns(true);

        var result = await _sut.GetByApiKeyAsync("raw-key");

        Assert.NotNull(result);
        Assert.Equal("team-1", result.TeamKey);
    }

    [Fact]
    public async Task GetByApiKeyAsync_With_No_Match_Returns_Null()
    {
        var entity = CreateEntity("key-1", "hash-1", "team-1");
        _repository.GetAsync().Returns(ToAsyncEnumerable(entity));
        _apiKeyService.Verify("wrong-key", "hash-1").Returns(false);

        var result = await _sut.GetByApiKeyAsync("wrong-key");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetKeysAsync_Returns_Existing_Keys()
    {
        var entity1 = CreateEntity("key-1", "hash-1", "team-1");
        var entity2 = CreateEntity("key-2", "hash-2", "team-1");
        _repository.GetAsync().Returns(ToAsyncEnumerable(entity1, entity2));

        var keys = await _sut.GetKeysAsync("team-1").ToArrayAsync();

        Assert.Equal(2, keys.Length);
    }

    [Fact]
    public async Task GetKeysAsync_AutoCreates_When_Fewer_Than_Two()
    {
        _repository.GetAsync().Returns(ToAsyncEnumerable<ApiKeyEntity>());
        _apiKeyService.BuildApiKey(Arg.Any<string>(), Arg.Any<Func<string>>()).Returns("new-key");
        _apiKeyService.Encrypt("new-key").Returns("new-hash");
        _repository.AddAsync(Arg.Any<ApiKeyEntity>()).Returns(ci => Task.FromResult(ci.Arg<ApiKeyEntity>()));

        var keys = await _sut.GetKeysAsync("team-1").ToArrayAsync();

        Assert.Equal(2, keys.Length);
        await _repository.Received(2).AddAsync(Arg.Any<ApiKeyEntity>());
    }

    [Fact]
    public async Task RefreshKeyAsync_Updates_Repository()
    {
        var existing = CreateEntity("key-1", "old-hash", "team-1");
        _repository.GetAsync("key-1").Returns(Task.FromResult(existing));
        _apiKeyService.BuildApiKey(Arg.Any<string>(), Arg.Any<Func<string>>()).Returns("refreshed-key");
        _apiKeyService.Encrypt("refreshed-key").Returns("refreshed-hash");

        await _sut.RefreshKeyAsync("team-1", "key-1");

        await _repository.Received(1).UpdateAsync("key-1", Arg.Is<ApiKeyEntity>(e => e.ApiKeyHash == "refreshed-hash"));
    }

    [Fact]
    public async Task LockKeyAsync_Verifies_Team_And_Delegates_To_Repository()
    {
        var entity = CreateEntity("key-1", "hash-1", "team-1");
        _repository.GetAsync("key-1").Returns(Task.FromResult(entity));

        await _sut.LockKeyAsync("team-1", "key-1");

        await _repository.Received(1).LockKeyAsync("key-1");
    }

    [Fact]
    public async Task LockKeyAsync_Wrong_Team_Throws()
    {
        var entity = CreateEntity("key-1", "hash-1", "team-1");
        _repository.GetAsync("key-1").Returns(Task.FromResult(entity));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.LockKeyAsync("team-2", "key-1"));
    }

    [Fact]
    public async Task DeleteKeyAsync_Wrong_Team_Throws()
    {
        var entity = CreateEntity("key-1", "hash-1", "team-1");
        _repository.GetAsync("key-1").Returns(Task.FromResult(entity));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.DeleteKeyAsync("team-2", "key-1"));
    }

    private static ApiKeyEntity CreateEntity(string key, string hash, string teamKey)
    {
        return new ApiKeyEntity
        {
            Id = global::MongoDB.Bson.ObjectId.GenerateNewId(),
            Key = key,
            Name = "Test",
            ApiKeyHash = hash,
            TeamKey = teamKey,
        };
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(params T[] items)
    {
        foreach (var item in items)
        {
            yield return item;
        }
        await Task.CompletedTask;
    }
}
