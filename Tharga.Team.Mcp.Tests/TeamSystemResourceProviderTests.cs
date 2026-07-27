using Microsoft.Extensions.Options;
using Tharga.Mcp;
using Tharga.Team.Mcp;
using Tharga.Team;
using Tharga.Team.Service.Audit;

namespace Tharga.Team.Mcp.Tests;

public class TeamSystemResourceProviderTests
{
    private readonly IApiKeyAdministrationService _apiKeyService = Substitute.For<IApiKeyAdministrationService>();
    private readonly ITenantRoleRegistry _roleRegistry = Substitute.For<ITenantRoleRegistry>();
    private readonly CompositeAuditLogger _auditLogger;

    public TeamSystemResourceProviderTests()
    {
        _auditLogger = new CompositeAuditLogger(
            Enumerable.Empty<IAuditLogger>(),
            Options.Create(new AuditOptions()));
    }

    private IMcpContext MakeContext(bool isDeveloper)
    {
        var ctx = Substitute.For<IMcpContext>();
        ctx.IsDeveloper.Returns(isDeveloper);
        ctx.Scope.Returns(McpScope.System);
        return ctx;
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(params T[] items)
    {
        foreach (var item in items) yield return item;
        await Task.CompletedTask;
    }

    [Fact]
    public async Task ListResourcesAsync_NonDeveloper_ReturnsEmpty()
    {
        var sut = new TeamSystemResourceProvider(_apiKeyService, _roleRegistry, _auditLogger);

        var result = await sut.ListResourcesAsync(MakeContext(isDeveloper: false), TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ListResourcesAsync_Developer_ReturnsAllAvailableResources()
    {
        var sut = new TeamSystemResourceProvider(_apiKeyService, _roleRegistry, _auditLogger);

        var result = await sut.ListResourcesAsync(MakeContext(isDeveloper: true), TestContext.Current.CancellationToken);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, r => r.Uri == TeamSystemResourceProvider.SystemKeysUri);
        Assert.Contains(result, r => r.Uri == TeamSystemResourceProvider.RolesUri);
        Assert.Contains(result, r => r.Uri == TeamSystemResourceProvider.AuditUri);
    }

    [Fact]
    public async Task ListResourcesAsync_OmitsAuditWhenAuditLoggerNotRegistered()
    {
        var sut = new TeamSystemResourceProvider(_apiKeyService, _roleRegistry, auditLogger: null);

        var result = await sut.ListResourcesAsync(MakeContext(isDeveloper: true), TestContext.Current.CancellationToken);

        Assert.DoesNotContain(result, r => r.Uri == TeamSystemResourceProvider.AuditUri);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ReadResourceAsync_NonDeveloper_Throws()
    {
        var sut = new TeamSystemResourceProvider(_apiKeyService, _roleRegistry, _auditLogger);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.ReadResourceAsync(TeamSystemResourceProvider.RolesUri, MakeContext(isDeveloper: false), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReadResourceAsync_UnknownUri_Throws()
    {
        var sut = new TeamSystemResourceProvider(_apiKeyService, _roleRegistry, _auditLogger);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ReadResourceAsync("team://system/unknown", MakeContext(isDeveloper: true), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReadResourceAsync_SystemKeys_RedactsRawApiKeyAndHash()
    {
        var key = Substitute.For<IApiKey>();
        key.Key.Returns("key-1");
        key.Name.Returns("mcp-gate");
        key.ApiKey.Returns("SHOULD_NOT_BE_EXPOSED");
        key.SystemScopes.Returns(new[] { "mcp:discover" });
        key.CreatedBy.Returns("daniel");
        _apiKeyService.GetSystemKeysAsync().Returns(ToAsyncEnumerable(key));

        var sut = new TeamSystemResourceProvider(_apiKeyService, _roleRegistry, _auditLogger);

        var content = await sut.ReadResourceAsync(TeamSystemResourceProvider.SystemKeysUri, MakeContext(isDeveloper: true), TestContext.Current.CancellationToken);

        Assert.NotNull(content.Text);
        Assert.Contains("mcp-gate", content.Text);
        Assert.Contains("daniel", content.Text);
        Assert.DoesNotContain("SHOULD_NOT_BE_EXPOSED", content.Text);
        Assert.DoesNotContain("ApiKeyHash", content.Text);
        Assert.Equal("application/json", content.MimeType);
    }

    [Fact]
    public async Task ReadResourceAsync_Roles_ReturnsRoleNames()
    {
        var role = new TenantRoleDefinition("Editor", new[] { "feature:read", "feature:write" });
        _roleRegistry.All.Returns(new[] { role });

        var sut = new TeamSystemResourceProvider(_apiKeyService, _roleRegistry, _auditLogger);

        var content = await sut.ReadResourceAsync(TeamSystemResourceProvider.RolesUri, MakeContext(isDeveloper: true), TestContext.Current.CancellationToken);

        Assert.Contains("Editor", content.Text);
        Assert.Contains("feature:read", content.Text);
    }

    [Fact]
    public async Task ReadResourceAsync_Audit_ReturnsQueryResult()
    {
        var sut = new TeamSystemResourceProvider(_apiKeyService, _roleRegistry, _auditLogger);

        var content = await sut.ReadResourceAsync(TeamSystemResourceProvider.AuditUri, MakeContext(isDeveloper: true), TestContext.Current.CancellationToken);

        Assert.NotNull(content.Text);
        Assert.Contains("items", content.Text);
        Assert.Equal("application/json", content.MimeType);
    }

    [Fact]
    public void Scope_IsSystem()
    {
        var sut = new TeamSystemResourceProvider(_apiKeyService, _roleRegistry, _auditLogger);
        Assert.Equal(McpScope.System, sut.Scope);
    }
}
