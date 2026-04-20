# Feature: mcp-platform-data

**Originating branch:** master
**Date started:** 2026-04-20

## Goal

Phase 5 of the MCP master plan — system-scope slice only. Expose **cross-tenant** team / API-key / role data via MCP as read-only resources for diagnostic use by a Developer. User-scope (`platform://me`) and Team-scope (caller's teams) providers are deferred.

Mutations are out of scope.

## Scope

### New provider: `PlatformSystemResourceProvider` (in Tharga.Platform.Mcp)

`IMcpResourceProvider` with `Scope = McpScope.System`. Returns `application/json` content.

Resources (actually implementable today):
- `platform://system/apikeys` — all **system** API keys (redacted) from `IApiKeyAdministrationService.GetSystemKeysAsync()`
- `platform://system/roles` — registered tenant roles from `ITenantRoleRegistry`
- `platform://system/audit` — most recent ~100 audit entries from the last 7 days via `CompositeAuditLogger.QueryAsync()`

Deferred to a follow-up:
- `platform://system/teams` — cross-tenant team listing requires a new `ITeamService.GetAllTeamsAsync()` method and matching base-class / repository implementation. Out of scope for this session.
- Cross-tenant **team** API keys — same blocker (need to enumerate teams first)

### Registration

Opt-in — consumers who want just the bridge don't get data resources auto-enabled.

```csharp
builder.Services.AddThargaMcp(mcp =>
{
    mcp.AddMcpPlatform(o =>
    {
        o.ExposeSystemResources = true;
    });
});
```

Default: `ExposeSystemResources = false`.

### Scope enforcement

Provider checks `IMcpContext.IsDeveloper == true` on every call:
- `ListResourcesAsync` without Developer → empty list
- `ReadResourceAsync` without Developer → `UnauthorizedAccessException`

### Sensitive data redaction

- API key `ApiKey` raw values always null in responses
- `ApiKeyHash` never exposed
- Everything else (identity, email, team metadata) is visible

## Out of scope (follow-ups)

- `PlatformUserResourceProvider` — `platform://me`
- `PlatformTeamResourceProvider` — `platform://teams`, `.../members`, `.../apikeys`
- Mutations / tools — not in Phase 5

## Acceptance criteria

- [ ] `McpPlatformOptions.ExposeSystemResources` (default false) controls provider registration
- [ ] `PlatformSystemResourceProvider.Scope == McpScope.System`
- [ ] Non-developer callers get empty `ListResourcesAsync` and `UnauthorizedAccessException` from `ReadResourceAsync`
- [ ] Developer callers get the three resources (audit only when `CompositeAuditLogger` is registered)
- [ ] Raw API key values / hashes never appear in any response
- [ ] Unit tests cover happy path + rejection
- [ ] Existing tests still pass
- [ ] README section added to `Tharga.Platform.Mcp`

## Done condition

All acceptance criteria met, all tests pass, user confirms the feature is complete.
