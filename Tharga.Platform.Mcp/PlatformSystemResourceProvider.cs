using System.Text.Json;
using Tharga.Mcp;
using Tharga.Team;
using Tharga.Team.Service.Audit;

namespace Tharga.Platform.Mcp;

/// <summary>
/// Read-only MCP resource provider that surfaces system-scope Platform data for diagnostic use.
/// Only available to callers with the Developer role (see <see cref="IMcpContext.IsDeveloper"/>).
/// Registered by <c>AddPlatform</c> when <see cref="McpPlatformOptions.ExposeSystemResources"/> is true.
/// </summary>
public sealed class PlatformSystemResourceProvider : IMcpResourceProvider
{
    private readonly IApiKeyAdministrationService _apiKeyAdministrationService;
    private readonly ITenantRoleRegistry _tenantRoleRegistry;
    private readonly CompositeAuditLogger _auditLogger;

    public const string SystemKeysUri = "platform://system/apikeys";
    public const string RolesUri = "platform://system/roles";
    public const string AuditUri = "platform://system/audit";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
    };

    public PlatformSystemResourceProvider(
        IApiKeyAdministrationService apiKeyAdministrationService = null,
        ITenantRoleRegistry tenantRoleRegistry = null,
        CompositeAuditLogger auditLogger = null)
    {
        _apiKeyAdministrationService = apiKeyAdministrationService;
        _tenantRoleRegistry = tenantRoleRegistry;
        _auditLogger = auditLogger;
    }

    public McpScope Scope => McpScope.System;

    public Task<IReadOnlyList<McpResourceDescriptor>> ListResourcesAsync(IMcpContext context, CancellationToken cancellationToken)
    {
        if (context?.IsDeveloper != true)
            return Task.FromResult<IReadOnlyList<McpResourceDescriptor>>(Array.Empty<McpResourceDescriptor>());

        var list = new List<McpResourceDescriptor>();

        if (_apiKeyAdministrationService != null)
        {
            list.Add(new McpResourceDescriptor
            {
                Uri = SystemKeysUri,
                Name = "System API Keys",
                Description = "Cross-tenant system API keys (not bound to a team). Raw key values are redacted.",
                MimeType = "application/json",
            });
        }

        if (_tenantRoleRegistry != null)
        {
            list.Add(new McpResourceDescriptor
            {
                Uri = RolesUri,
                Name = "Tenant Roles",
                Description = "Registered tenant roles and their granted scopes.",
                MimeType = "application/json",
            });
        }

        if (_auditLogger != null)
        {
            list.Add(new McpResourceDescriptor
            {
                Uri = AuditUri,
                Name = "Audit Log",
                Description = "Most recent ~100 audit entries from the last 7 days.",
                MimeType = "application/json",
            });
        }

        return Task.FromResult<IReadOnlyList<McpResourceDescriptor>>(list);
    }

    public async Task<McpResourceContent> ReadResourceAsync(string uri, IMcpContext context, CancellationToken cancellationToken)
    {
        if (context?.IsDeveloper != true)
            throw new UnauthorizedAccessException("System resources require the Developer role.");

        return uri switch
        {
            SystemKeysUri => await ReadSystemKeysAsync(cancellationToken),
            RolesUri => ReadRoles(),
            AuditUri => await ReadAuditAsync(),
            _ => throw new InvalidOperationException($"Unknown resource URI '{uri}'."),
        };
    }

    private async Task<McpResourceContent> ReadSystemKeysAsync(CancellationToken cancellationToken)
    {
        if (_apiKeyAdministrationService == null)
            throw new InvalidOperationException("IApiKeyAdministrationService is not registered.");

        var keys = new List<object>();
        await foreach (var key in _apiKeyAdministrationService.GetSystemKeysAsync().WithCancellation(cancellationToken))
        {
            keys.Add(new
            {
                key.Key,
                key.Name,
                SystemScopes = key.SystemScopes ?? Array.Empty<string>(),
                key.ExpiryDate,
                key.CreatedAt,
                key.CreatedBy,
            });
        }

        return new McpResourceContent
        {
            Uri = SystemKeysUri,
            Text = JsonSerializer.Serialize(new { items = keys }, _jsonOptions),
            MimeType = "application/json",
        };
    }

    private McpResourceContent ReadRoles()
    {
        if (_tenantRoleRegistry == null)
            throw new InvalidOperationException("ITenantRoleRegistry is not registered.");

        var items = _tenantRoleRegistry.All.Select(r => new
        {
            r.Name,
            r.Scopes,
        });

        return new McpResourceContent
        {
            Uri = RolesUri,
            Text = JsonSerializer.Serialize(new { items }, _jsonOptions),
            MimeType = "application/json",
        };
    }

    private async Task<McpResourceContent> ReadAuditAsync()
    {
        if (_auditLogger == null)
            throw new InvalidOperationException("CompositeAuditLogger is not registered.");

        var query = new AuditQuery
        {
            From = DateTime.UtcNow.AddDays(-7),
            Take = 100,
            SortDescending = true,
        };

        var result = await _auditLogger.QueryAsync(query);

        return new McpResourceContent
        {
            Uri = AuditUri,
            Text = JsonSerializer.Serialize(new
            {
                total = result.TotalCount,
                items = result.Items,
            }, _jsonOptions),
            MimeType = "application/json",
        };
    }
}
