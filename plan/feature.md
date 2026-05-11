# Feature: MCP user-scope and team-scope resources

Tracks `Requests.md` → *"Tharga.Platform — MCP / MCP Provider for Team/User data"* (Status: Partial). System slice shipped via PR #51 (2026-04-20); this feature extends the bridge to user-scope and team-scope.

## Goal

Surface the most useful per-caller Platform data over MCP so an LLM agent operating on behalf of an authenticated user can answer questions like "what teams am I a member of?" or "show the members of my current team" without the consumer having to wire its own MCP provider.

Two new resource providers in `Tharga.Platform.Mcp`, registered automatically by `AddPlatform()` when the required dependencies are present:

- **`PlatformUserResourceProvider`** (`McpScope.User`): exposes `platform://me`.
- **`PlatformTeamResourceProvider`** (`McpScope.Team`): exposes `platform://team`, `platform://team/members`, `platform://team/apikeys` — each one rooted at the caller's *current* team (from the `TeamKey` claim). No cross-tenant enumeration.

The system-scope cross-tenant team listing (`platform://system/teams` / `platform://teams/{teamKey}/...`) is **out of scope** for this feature — it requires a new `ITeamService.GetAllTeamsAsync()` method and is tracked as a separate follow-up.

## Scope

Three pieces:

1. **`PlatformUserResourceProvider`** — single resource `platform://me` listing the caller's `IUser` (UserId, Name, EMail, Identity) and a memberships array built from `ITeamService.GetTeamsAsync()` (already current-user-scoped).
2. **`PlatformTeamResourceProvider`** — three resources rooted at the caller's current team:
   - `platform://team` — team metadata: Key, Name, Icon, ConsentedRoles.
   - `platform://team/members` — members of the current team (Key, Name, AccessLevel, State, TenantRoles, ScopeOverrides, Invitation presence flag).
   - `platform://team/apikeys` — `IApiKeyAdministrationService.GetKeysAsync(teamKey)` results with raw `ApiKey` values redacted (the same redaction pattern the system slice uses).
3. **New `ITeamService.GetMembersAsync(string teamKey)`** returning `IAsyncEnumerable<ITeamMember>`. Default implementation in `TeamServiceBase` reads the typed team via the existing protected non-generic `GetTeamAsync(teamKey)` and extracts `Members` via a one-line reflection helper. Consumers subclassing `TeamServiceBase` get this for free; consumers implementing `ITeamService` directly need to add it (rare).

## Behaviour

- Both providers are registered unconditionally by `AddPlatform()` — no new opt-in option. They are scope-gated by `McpScope.User` / `McpScope.Team`, which is enforced by the dispatcher's hierarchy filter; an anonymous or System-only caller doesn't see them.
- `PlatformUserResourceProvider.ReadResourceAsync("platform://me")` requires an authenticated user (the dispatcher should have already ensured this; we add a defensive null-check that throws `UnauthorizedAccessException` if missing).
- `PlatformTeamResourceProvider` requires a `TeamKey` claim on the principal. If the caller doesn't have one (no current team selected), `ListResourcesAsync` returns an empty list and `ReadResourceAsync` throws `UnauthorizedAccessException` with a "No team selected" message.
- All resource payloads are JSON, MimeType `application/json`, using the same indented-JSON pattern as `PlatformSystemResourceProvider`.

## Acceptance criteria

1. `AddPlatform()` registers `PlatformUserResourceProvider` and `PlatformTeamResourceProvider`. Existing system-scope provider registration is unchanged.
2. New `Task<IReadOnlyList<ITeamMember>> GetMembersAsync(string teamKey)` (or `IAsyncEnumerable` shape — confirm during step 1) on `ITeamService` with a default `TeamServiceBase` implementation that uses reflection on the non-generic `GetTeamAsync` result. The reflection happens *once* inside `TeamServiceBase`; callers get a typed enumerable.
3. `platform://me` returns JSON shaped `{ user: { key, identity, name, email }, memberships: [{ teamKey, teamName, accessLevel, state }] }`.
4. `platform://team` returns `{ key, name, icon, consentedRoles }` for the caller's current team.
5. `platform://team/members` returns `{ teamKey, items: [{ key, name, accessLevel, state, tenantRoles, scopeOverrides, invited }] }`.
6. `platform://team/apikeys` returns `{ teamKey, items: [{ key, name, accessLevel, expiryDate, createdAt, createdBy }] }` — raw `ApiKey` values redacted (omitted from output entirely, matching the system slice's pattern).
7. Unit tests cover: provider listing returns empty for missing principal/TeamKey; provider read throws `UnauthorizedAccessException` for missing prereqs; happy-path read returns expected JSON shape; `TeamServiceBase.GetMembersAsync` returns the typed enumerable.
8. `Tharga.Platform.Mcp/README.md` updated with a "User and team resources" section.
9. `dotnet build -c Release` clean; `dotnet test -c Release` green.

## Done condition

PR opened from `feature/mcp-user-and-team-scope` → `master`, all CI checks green, user has confirmed.

## Out of scope

- `platform://system/teams` cross-tenant team enumeration — requires a new `ITeamService.GetAllTeamsAsync()` method. Filed as a future follow-up; the current MCP partial entry in `Requests.md` stays open for that piece (downgraded to "cross-tenant enumeration only").
- User-level "personal" API keys — Tharga.Team doesn't model these; only team and system keys exist.
- Tool providers (`IMcpToolProvider`) — this feature is read-only resources only. Mutating operations (create team, change role, etc.) are intentionally not exposed.
- New scope constants in `McpScopes` — the existing `mcp:discover` covers listing; no per-resource read scope is added because the providers self-gate on principal/TeamKey presence.
