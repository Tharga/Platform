using Tharga.Toolkit.Password;

namespace Tharga.Team.Service.Tests;

public class SystemApiKeyAdministrationServiceTests
{
    private readonly IApiKeyRepository _repository = Substitute.For<IApiKeyRepository>();
    private readonly IApiKeyService _apiKeyService = Substitute.For<IApiKeyService>();
    private readonly ApiKeyAdministrationService _sut;

    public SystemApiKeyAdministrationServiceTests()
    {
        _sut = new ApiKeyAdministrationService(_repository, _apiKeyService);
        _apiKeyService.BuildApiKey(Arg.Any<string>(), Arg.Any<Func<string>>()).Returns("sys-key");
        _apiKeyService.Encrypt("sys-key").Returns("sys-hash");
    }

    [Fact]
    public async Task CreateSystemKeyAsync_Persists_Key_Without_TeamKey()
    {
        _repository.AddAsync(Arg.Any<ApiKeyEntity>()).Returns(ci => Task.FromResult(ci.Arg<ApiKeyEntity>()));

        var result = await _sut.CreateSystemKeyAsync("mcp", new[] { "mcp:mongodb:admin" });

        Assert.Null(result.TeamKey);
        Assert.Equal("mcp", result.Name);
        Assert.Equal(new[] { "mcp:mongodb:admin" }, result.SystemScopes);
        await _repository.Received(1).AddAsync(Arg.Is<ApiKeyEntity>(e => e.TeamKey == null && e.SystemScopes != null));
    }

    [Fact]
    public async Task CreateSystemKeyAsync_Sets_CreatedBy()
    {
        _repository.AddAsync(Arg.Any<ApiKeyEntity>()).Returns(ci => Task.FromResult(ci.Arg<ApiKeyEntity>()));

        var result = await _sut.CreateSystemKeyAsync("mcp", Array.Empty<string>(), createdBy: "daniel@example.com");

        Assert.Equal("daniel@example.com", result.CreatedBy);
    }

    [Fact]
    public async Task GetSystemKeysAsync_Returns_Only_Keys_Without_TeamKey()
    {
        var team = CreateEntity("team-key", "h1", teamKey: "team-1");
        var sys = CreateSystemEntity("sys-key", "h2");
        _repository.GetAsync().Returns(ToAsyncEnumerable(team, sys));

        var keys = await _sut.GetSystemKeysAsync().ToArrayAsync(TestContext.Current.CancellationToken);

        Assert.Single(keys);
        Assert.Null(keys[0].TeamKey);
    }

    [Fact]
    public async Task RefreshSystemKeyAsync_Rejects_TeamKey()
    {
        var team = CreateEntity("team-key", "h1", teamKey: "team-1");
        _repository.GetAsync("team-key").Returns(Task.FromResult(team));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.RefreshSystemKeyAsync("team-key"));
    }

    [Fact]
    public async Task LockSystemKeyAsync_Rejects_TeamKey()
    {
        var team = CreateEntity("team-key", "h1", teamKey: "team-1");
        _repository.GetAsync("team-key").Returns(Task.FromResult(team));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.LockSystemKeyAsync("team-key"));
    }

    [Fact]
    public async Task DeleteSystemKeyAsync_Rejects_TeamKey()
    {
        var team = CreateEntity("team-key", "h1", teamKey: "team-1");
        _repository.GetAsync("team-key").Returns(Task.FromResult(team));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.DeleteSystemKeyAsync("team-key"));
    }

    [Fact]
    public async Task LockKeyAsync_Team_Variant_Rejects_SystemKey()
    {
        var sys = CreateSystemEntity("sys-key", "h1");
        _repository.GetAsync("sys-key").Returns(Task.FromResult(sys));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.LockKeyAsync("team-1", "sys-key"));
    }

    [Fact]
    public async Task DeleteKeyAsync_Team_Variant_Rejects_SystemKey()
    {
        var sys = CreateSystemEntity("sys-key", "h1");
        _repository.GetAsync("sys-key").Returns(Task.FromResult(sys));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.DeleteKeyAsync("team-1", "sys-key"));
    }

    [Fact]
    public async Task RefreshSystemKeyAsync_Preserves_Scopes_And_CreatedBy()
    {
        var sys = CreateSystemEntity("sys-key", "h1", scopes: new[] { "mcp:discover" }, createdBy: "alice");
        _repository.GetAsync("sys-key").Returns(Task.FromResult(sys));

        await _sut.RefreshSystemKeyAsync("sys-key");

        await _repository.Received(1).UpdateAsync("sys-key", Arg.Is<ApiKeyEntity>(e =>
            e.TeamKey == null &&
            e.SystemScopes.Length == 1 && e.SystemScopes[0] == "mcp:discover" &&
            e.CreatedBy == "alice"));
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

    private static ApiKeyEntity CreateSystemEntity(string key, string hash, string[] scopes = null, string createdBy = null)
    {
        return new ApiKeyEntity
        {
            Id = global::MongoDB.Bson.ObjectId.GenerateNewId(),
            Key = key,
            Name = "SystemTest",
            ApiKeyHash = hash,
            TeamKey = null,
            SystemScopes = scopes,
            CreatedBy = createdBy,
        };
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(params T[] items)
    {
        foreach (var item in items) yield return item;
        await Task.CompletedTask;
    }
}
