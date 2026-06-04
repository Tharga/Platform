using Tharga.Team;

namespace Tharga.Team.Service.Tests;

public class ApiKeyLifecycleDecoratorTests
{
    private readonly IApiKeyAdministrationService _inner = Substitute.For<IApiKeyAdministrationService>();
    private readonly RecordingHandler _handler = new();

    private ApiKeyLifecycleDecorator CreateSut(params IApiKeyLifecycleHandler[] handlers)
        => new(_inner, handlers.Length == 0 ? [_handler] : handlers);

    private static IApiKey Key(string id = "key-1", string token = "raw-token", string team = "team-1",
        string name = "My Key", IReadOnlyList<Tag> tags = null)
    {
        var k = Substitute.For<IApiKey>();
        k.Key.Returns(id);
        k.ApiKey.Returns(token);
        k.TeamKey.Returns(team);
        k.Name.Returns(name);
        k.Tags.Returns(tags ?? [new Tag("Type", "demo")]);
        return k;
    }

    [Fact]
    public async Task CreateKeyAsync_Fires_Created_With_Token_And_Identity()
    {
        var key = Key();
        _inner.CreateKeyAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AccessLevel>(), Arg.Any<string[]>(),
            Arg.Any<string[]>(), Arg.Any<DateTime?>(), Arg.Any<IReadOnlyList<Tag>>(), Arg.Any<string>())
            .Returns(key);

        await CreateSut().CreateKeyAsync("team-1", "My Key", AccessLevel.User);

        var ctx = Assert.Single(_handler.Calls);
        Assert.Equal(ApiKeyLifecycleReason.Created, ctx.Reason);
        Assert.Equal("key-1", ctx.ApiKeyId);
        Assert.Equal("raw-token", ctx.PrivateToken);
        Assert.Equal("team-1", ctx.TeamKey);
        Assert.False(ctx.IsSystemKey);
        Assert.Equal("My Key", ctx.Name);
        Assert.Equal("Type", Assert.Single(ctx.Tags).Key);
    }

    [Fact]
    public async Task RefreshKeyAsync_Fires_Recycled_With_Token()
    {
        var key = Key(token: "new-token");
        _inner.RefreshKeyAsync("team-1", "key-1").Returns(key);

        await CreateSut().RefreshKeyAsync("team-1", "key-1");

        var ctx = Assert.Single(_handler.Calls);
        Assert.Equal(ApiKeyLifecycleReason.Recycled, ctx.Reason);
        Assert.Equal("new-token", ctx.PrivateToken);
    }

    [Fact]
    public async Task DeleteKeyAsync_Fires_Deleted_Without_Token()
    {
        await CreateSut().DeleteKeyAsync("team-1", "key-1");

        var ctx = Assert.Single(_handler.Calls);
        Assert.Equal(ApiKeyLifecycleReason.Deleted, ctx.Reason);
        Assert.Equal("key-1", ctx.ApiKeyId);
        Assert.Equal("team-1", ctx.TeamKey);
        Assert.False(ctx.IsSystemKey);
        Assert.Null(ctx.PrivateToken);
    }

    [Fact]
    public async Task CreateSystemKeyAsync_Fires_Created_As_System()
    {
        var key = Key(team: null);
        _inner.CreateSystemKeyAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<DateTime?>(), Arg.Any<string>())
            .Returns(key);

        await CreateSut().CreateSystemKeyAsync("Sys", ["sys:read"]);

        var ctx = Assert.Single(_handler.Calls);
        Assert.Equal(ApiKeyLifecycleReason.Created, ctx.Reason);
        Assert.True(ctx.IsSystemKey);
        Assert.Null(ctx.TeamKey);
        Assert.Equal("raw-token", ctx.PrivateToken);
    }

    [Fact]
    public async Task RefreshSystemKeyAsync_Fires_Recycled_As_System()
    {
        var key = Key(team: null);
        _inner.RefreshSystemKeyAsync("key-1").Returns(key);

        await CreateSut().RefreshSystemKeyAsync("key-1");

        var ctx = Assert.Single(_handler.Calls);
        Assert.Equal(ApiKeyLifecycleReason.Recycled, ctx.Reason);
        Assert.True(ctx.IsSystemKey);
    }

    [Fact]
    public async Task DeleteSystemKeyAsync_Fires_Deleted_As_System()
    {
        await CreateSut().DeleteSystemKeyAsync("key-1");

        var ctx = Assert.Single(_handler.Calls);
        Assert.Equal(ApiKeyLifecycleReason.Deleted, ctx.Reason);
        Assert.True(ctx.IsSystemKey);
        Assert.Null(ctx.TeamKey);
        Assert.Null(ctx.PrivateToken);
    }

    [Fact]
    public async Task Read_Lock_And_Set_Operations_Do_Not_Fire()
    {
        var sut = CreateSut();
        await sut.GetByApiKeyAsync("raw");
        await sut.LockKeyAsync("team-1", "key-1");
        await sut.LockSystemKeyAsync("key-1");
        await sut.SetScopeOverridesAsync("team-1", "key-1", ["doc:read"]);
        await sut.SetRolesAsync("team-1", "key-1", ["Editor"]);

        Assert.Empty(_handler.Calls);
    }

    [Fact]
    public async Task Handler_Exception_Propagates_From_CreateKeyAsync()
    {
        var key = Key();
        _inner.CreateKeyAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AccessLevel>(), Arg.Any<string[]>(),
            Arg.Any<string[]>(), Arg.Any<DateTime?>(), Arg.Any<IReadOnlyList<Tag>>(), Arg.Any<string>())
            .Returns(key);
        _handler.OnCall = _ => throw new InvalidOperationException("store failed");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateSut().CreateKeyAsync("team-1", "My Key", AccessLevel.User));
    }

    [Fact]
    public async Task All_Registered_Handlers_Are_Invoked()
    {
        var key = Key();
        _inner.RefreshKeyAsync("team-1", "key-1").Returns(key);
        var h2 = new RecordingHandler();

        await CreateSut(_handler, h2).RefreshKeyAsync("team-1", "key-1");

        Assert.Single(_handler.Calls);
        Assert.Single(h2.Calls);
    }

    private sealed class RecordingHandler : IApiKeyLifecycleHandler
    {
        public readonly List<ApiKeyLifecycleContext> Calls = [];
        public Func<ApiKeyLifecycleContext, Task> OnCall;

        public Task OnApiKeyLifecycleAsync(ApiKeyLifecycleContext context)
        {
            Calls.Add(context);
            return OnCall?.Invoke(context) ?? Task.CompletedTask;
        }
    }
}
