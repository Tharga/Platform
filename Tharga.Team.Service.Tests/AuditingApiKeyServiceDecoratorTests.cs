using Microsoft.AspNetCore.Http;
using Tharga.Team.Service.Audit;

namespace Tharga.Team.Service.Tests;

public class AuditingApiKeyServiceDecoratorTests
{
    private readonly IApiKeyAdministrationService _inner;
    private readonly FakeAuditBackend _backend;
    private readonly AuditingApiKeyServiceDecorator _sut;

    public AuditingApiKeyServiceDecoratorTests()
    {
        _inner = Substitute.For<IApiKeyAdministrationService>();
        _inner.CreateKeyAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AccessLevel>(), Arg.Any<string[]>(), Arg.Any<DateTime?>())
            .Returns(Substitute.For<IApiKey>());
        _inner.RefreshKeyAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Substitute.For<IApiKey>());

        var (logger, backend) = FakeAuditLoggerFactory.Create();
        _backend = backend;
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _sut = new AuditingApiKeyServiceDecorator(_inner, logger, httpContextAccessor);
    }

    [Fact]
    public async Task CreateKeyAsync_LogsAuditEntry()
    {
        await _sut.CreateKeyAsync("team-1", "My Key", AccessLevel.User);

        var entry = Assert.Single(_backend.Entries);
        Assert.Equal("apikey", entry.Feature);
        Assert.Equal("create", entry.Action);
        Assert.Equal("team-1", entry.TeamKey);
        Assert.True(entry.Success);
    }

    [Fact]
    public async Task RefreshKeyAsync_LogsAuditEntry()
    {
        await _sut.RefreshKeyAsync("team-1", "key-1");

        var entry = Assert.Single(_backend.Entries);
        Assert.Equal("refresh", entry.Action);
        Assert.Equal("team-1", entry.TeamKey);
    }

    [Fact]
    public async Task LockKeyAsync_LogsAuditEntry()
    {
        await _sut.LockKeyAsync("team-1", "key-1");

        var entry = Assert.Single(_backend.Entries);
        Assert.Equal("lock", entry.Action);
        Assert.Equal("team-1", entry.TeamKey);
    }

    [Fact]
    public async Task DeleteKeyAsync_LogsAuditEntry()
    {
        await _sut.DeleteKeyAsync("team-1", "key-1");

        var entry = Assert.Single(_backend.Entries);
        Assert.Equal("delete", entry.Action);
        Assert.Equal("team-1", entry.TeamKey);
    }

    [Fact]
    public async Task ReadOperations_DoNotLog()
    {
        await _sut.GetByApiKeyAsync("some-key");

        Assert.Empty(_backend.Entries);
    }

    [Fact]
    public async Task FailedOperation_LogsWithErrorMessage()
    {
        _inner.When(x => x.DeleteKeyAsync(Arg.Any<string>(), Arg.Any<string>()))
            .Do(_ => throw new UnauthorizedAccessException("Not authorized"));

        try { await _sut.DeleteKeyAsync("team-1", "key-1"); } catch { }

        var entry = Assert.Single(_backend.Entries);
        Assert.False(entry.Success);
        Assert.Contains("Not authorized", entry.ErrorMessage);
    }
}
