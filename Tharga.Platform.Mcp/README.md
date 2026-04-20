# Tharga Platform Mcp
[![NuGet](https://img.shields.io/nuget/v/Tharga.Platform.Mcp)](https://www.nuget.org/packages/Tharga.Platform.Mcp)
![Nuget](https://img.shields.io/nuget/dt/Tharga.Platform.Mcp)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Platform bridge for [Tharga.Mcp](https://www.nuget.org/packages/Tharga.Mcp). Connects MCP tool and resource invocations to Tharga.Platform's authentication, scope enforcement, and audit logging.

## What it does

- Populates `IMcpContext` from the authenticated `HttpContext.User` (works with both OIDC and API Key authentication)
- Enforces provider scope class: `McpScope.System` requires `Roles.Developer`, `McpScope.Team` requires team membership
- Emits audit log entries for every MCP tool invocation — success and failure
- Registers built-in `mcp:*` scopes in Platform's scope registry
- Requires authentication on the MCP endpoint — anonymous requests are rejected

## Quick start

```csharp
builder.Services.AddThargaMcp(mcp =>
{
    mcp.AddMcpPlatform();        // bridge to Platform auth/scopes/audit
    // ... other provider packages (e.g. mcp.AddMongoDB())
});

app.UseThargaMcp();
```

## System-scope diagnostic resources (opt-in)

Expose read-only diagnostic data under `platform://system/*` for callers with the Developer role. Non-developers see no resources and get `UnauthorizedAccessException` on read.

```csharp
builder.Services.AddThargaMcp(mcp =>
{
    mcp.AddMcpPlatform(o =>
    {
        o.ExposeSystemResources = true;
    });
});
```

Available resources (listed only when the matching dependency is registered):

| URI | Contents |
|-----|----------|
| `platform://system/apikeys` | System API keys (not bound to a team). Raw key values are redacted. |
| `platform://system/roles` | Tenant roles registered via `AddThargaTenantRoles` |
| `platform://system/audit` | Most recent ~100 audit entries from the last 7 days |

Cross-tenant team listings and per-team API-key listings are deferred — they require a new `ITeamService` method and are tracked separately.

## Related packages

| Package | Description |
|---------|-------------|
| [Tharga.Mcp](https://www.nuget.org/packages/Tharga.Mcp) | MCP foundation (contracts, transport) |
| [Tharga.Team.Service](https://www.nuget.org/packages/Tharga.Team.Service) | Platform scope/audit primitives |
