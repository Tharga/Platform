using Microsoft.Extensions.Options;
using Tharga.Mcp;
using Tharga.Platform.Mcp;
using Tharga.Team;
using Tharga.Team.Service.Audit;

namespace Tharga.Platform.Mcp.Tests;

public class PlatformSystemResourceProviderTests
{
    private readonly IApiKeyAdministrationService _apiKeyService = Substitute.For<IApiKeyAdministrationService>();
    private readonly ITenantRoleRegistry _roleRegistry = Substitute.For<ITenantRoleRegistry>();
    private readonly CompositeAuditLogger _auditLogger;

    public PlatformSystemResourceProviderTests()
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
        var sut = new PlatformSystemResourceProvider(_apiKeyService, _roleRegistry, _auditLogger);

        var result = await sut.ListResourcesAsync(MakeContext(isDeveloper: false), default);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ListResourcesAsync_Developer_ReturnsAllAvailableResources()
    {
        var sut = new PlatformSystemResourceProvider(_apiKeyService, _roleRegistry, _auditLogger);

        var result = await sut.ListResourcesAsync(MakeContext(isDeveloper: true), default);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, r => r.Uri == PlatformSystemResourceProvider.SystemKeysUri);
        Assert.Contains(result, r => r.Uri == PlatformSystemResourceProvider.RolesUri);
        Assert.Contains(result, r => r.Uri == PlatformSystemResourceProvider.AuditUri);
    }

    [Fact]
    public async Task ListResourcesAsync_OmitsAuditWhenAuditLoggerNotRegistered()
    {
        var sut = new PlatformSystemResourceProvider(_apiKeyService, _roleRegistry, auditLogger: null);

        var result = await sut.ListResourcesAsync(MakeContext(isDeveloper: true), default);

        Assert.DoesNotContain(result, r => r.Uri == PlatformSystemResourceProvider.AuditUri);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ReadResourceAsync_NonDeveloper_Throws()
    {
        var sut = new PlatformSystemResourceProvider(_apiKeyService, _roleRegistry, _auditLogger);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.ReadResourceAsync(PlatformSystemResourceProvider.RolesUri, MakeContext(isDeveloper: false), default));
    }

    [Fact]
    public async Task ReadResourceAsync_UnknownUri_Throws()
    {
        var sut = new PlatformSystemResourceProvider(_apiKeyService, _roleRegistry, _auditLogger);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ReadResourceAsync("platform://system/unknown", MakeContext(isDeveloper: true), default));
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

        var sut = new PlatformSystemResourceProvider(_apiKeyService, _roleRegistry, _auditLogger);

        var content = await sut.ReadResourceAsync(PlatformSystemResourceProvider.SystemKeysUri, MakeContext(isDeveloper: true), default);

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

        var sut = new PlatformSystemResourceProvider(_apiKeyService, _roleRegistry, _auditLogger);

        var content = await sut.ReadResourceAsync(PlatformSystemResourceProvider.RolesUri, MakeContext(isDeveloper: true), default);

        Assert.Contains("Editor", content.Text);
        Assert.Contains("feature:read", content.Text);
    }

    [Fact]
    public async Task ReadResourceAsync_Audit_ReturnsQueryResult()
    {
        var sut = new PlatformSystemResourceProvider(_apiKeyService, _roleRegistry, _auditLogger);

        var content = await sut.ReadResourceAsync(PlatformSystemResourceProvider.AuditUri, MakeContext(isDeveloper: true), default);

        Assert.NotNull(content.Text);
        Assert.Contains("items", content.Text);
        Assert.Equal("application/json", content.MimeType);
    }

    [Fact]
    public void Scope_IsSystem()
    {
        var sut = new PlatformSystemResourceProvider(_apiKeyService, _roleRegistry, _auditLogger);
        Assert.Equal(McpScope.System, sut.Scope);
    }
}
