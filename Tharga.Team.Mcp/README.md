# Tharga Team Mcp
[![NuGet](https://img.shields.io/nuget/v/Tharga.Team.Mcp)](https://www.nuget.org/packages/Tharga.Team.Mcp)
![Nuget](https://img.shields.io/nuget/dt/Tharga.Team.Mcp)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Team bridge for [Tharga.Mcp](https://www.nuget.org/packages/Tharga.Mcp). Connects MCP tool and resource invocations to Tharga.Team's authentication, scope enforcement, and audit logging.

> **Renamed from `Tharga.Platform.Mcp`.** NuGet package IDs cannot be renamed, so this is a new ID and the
> old one is deprecated. Migrating is a deliberate step with two breaking changes: `mcp.AddPlatform()` is now
> `mcp.AddTeam()`, and **every resource URI moved from `platform://` to `team://`** — update any MCP client
> that references them by string.

## What it does

- Populates `IMcpContext` from the authenticated `HttpContext.User` (works with both OIDC and API Key authentication)
- Enforces provider scope class: `McpScope.System` requires `Roles.Developer`, `McpScope.Team` requires team membership
- Emits audit log entries for every MCP tool invocation — success and failure
- Registers built-in `mcp:*` scopes in Team's scope registry
- Requires authentication on the MCP endpoint — anonymous requests are rejected

## Quick start

```csharp
builder.Services.AddThargaMcp(mcp =>
{
    mcp.AddTeam();        // bridge to Team auth/scopes/audit
    // ... other provider packages (e.g. mcp.AddMongoDB())
});

app.UseThargaMcp();
```

## User and team resources

Always-on resource providers that surface the authenticated caller's own data. Both providers self-gate on the principal's claims, so anonymous and system-only callers see no resources.

### User scope (`McpScope.User`)

| URI | Contents |
|-----|----------|
| `team://me` | The caller's `IUser` (`key`, `identity`, `name`, `email`) and a `memberships` array — for each team the caller is in, its `teamKey`, `teamName`, plus the caller's `accessLevel` and membership `state`. |

Listed when the principal carries a `NameIdentifier` (or equivalent) claim. Read fails with `UnauthorizedAccessException` if `IUserService.GetCurrentUserAsync` returns null.

### Team scope (`McpScope.Team`)

| URI | Contents |
|-----|----------|
| `team://team` | Metadata for the caller's *current* team (from the `TeamKey` claim): `key`, `name`, `icon`, `consentedRoles`. |
| `team://team/members` | Members of the current team: `key`, `name`, `accessLevel`, `state`, `tenantRoles`, `scopeOverrides`, and an `invited` flag. |
| `team://team/apikeys` | API keys for the current team. Raw key values are redacted (the `apiKey` property is omitted entirely). Listed only when `IApiKeyAdministrationService` is registered. |

Listed only when the principal carries a `TeamKey` claim. Read fails with `UnauthorizedAccessException` if no team is selected. Cross-tenant team listing (reading other teams) is intentionally not supported here — that's a future system-scope provider once `ITeamService.GetAllTeamsAsync` is added.

## System-scope diagnostic resources (opt-in)

Expose read-only diagnostic data under `team://system/*` for callers with the Developer role. Non-developers see no resources and get `UnauthorizedAccessException` on read.

```csharp
builder.Services.AddThargaMcp(mcp =>
{
    mcp.AddTeam(o =>
    {
        o.ExposeSystemResources = true;
    });
});
```

Available resources (listed only when the matching dependency is registered):

| URI | Contents |
|-----|----------|
| `team://system/apikeys` | System API keys (not bound to a team). Raw key values are redacted. |
| `team://system/roles` | Tenant roles registered via `AddThargaTenantRoles` |
| `team://system/audit` | Most recent ~100 audit entries from the last 7 days |

Per-team API-key listings now ship under `team://team/apikeys` (see "Team scope" above). Cross-tenant team listings remain deferred — they require a new `ITeamService.GetAllTeamsAsync` method.

## Related packages

| Package | Description |
|---------|-------------|
| [Tharga.Mcp](https://www.nuget.org/packages/Tharga.Mcp) | MCP foundation (contracts, transport) |
| [Tharga.Team.Service](https://www.nuget.org/packages/Tharga.Team.Service) | Team scope/audit primitives |
