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
    mcp.AddPlatform();        // bridge to Platform auth/scopes/audit
    // ... other provider packages (e.g. mcp.AddMongoDB())
});

app.UseThargaMcp();
```

## User and team resources

Always-on resource providers that surface the authenticated caller's own data. Both providers self-gate on the principal's claims, so anonymous and system-only callers see no resources.

### User scope (`McpScope.User`)

| URI | Contents |
|-----|----------|
| `platform://me` | The caller's `IUser` (`key`, `identity`, `name`, `email`) and a `memberships` array — for each team the caller is in, its `teamKey`, `teamName`, plus the caller's `accessLevel` and membership `state`. |

Listed when the principal carries a `NameIdentifier` (or equivalent) claim. Read fails with `UnauthorizedAccessException` if `IUserService.GetCurrentUserAsync` returns null.

### Team scope (`McpScope.Team`)

| URI | Contents |
|-----|----------|
| `platform://team` | Metadata for the caller's *current* team (from the `TeamKey` claim): `key`, `name`, `icon`, `consentedRoles`. |
| `platform://team/members` | Members of the current team: `key`, `name`, `accessLevel`, `state`, `tenantRoles`, `scopeOverrides`, and an `invited` flag. |
| `platform://team/apikeys` | API keys for the current team. Raw key values are redacted (the `apiKey` property is omitted entirely). Listed only when `IApiKeyAdministrationService` is registered. |

Listed only when the principal carries a `TeamKey` claim. Read fails with `UnauthorizedAccessException` if no team is selected. Cross-tenant team listing (reading other teams) is intentionally not supported here — that's a future system-scope provider once `ITeamService.GetAllTeamsAsync` is added.

## System-scope diagnostic resources (opt-in)

Expose read-only diagnostic data under `platform://system/*` for callers with the Developer role. Non-developers see no resources and get `UnauthorizedAccessException` on read.

```csharp
builder.Services.AddThargaMcp(mcp =>
{
    mcp.AddPlatform(o =>
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

Per-team API-key listings now ship under `platform://team/apikeys` (see "Team scope" above). Cross-tenant team listings remain deferred — they require a new `ITeamService.GetAllTeamsAsync` method.

## Related packages

| Package | Description |
|---------|-------------|
| [Tharga.Mcp](https://www.nuget.org/packages/Tharga.Mcp) | MCP foundation (contracts, transport) |
| [Tharga.Team.Service](https://www.nuget.org/packages/Tharga.Team.Service) | Platform scope/audit primitives |
