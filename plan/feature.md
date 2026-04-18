# Feature: mcp-platform-bridge

**Originating branch:** master
**Date started:** 2026-04-19

## Goal

Bridge Tharga.Platform auth/scopes/audit to Tharga.Mcp. Deliver the `Tharga.Platform.Mcp` package (Phase 1 of the MCP master plan) so MCP tool invocations flow through the existing Platform security stack with no parallel permission system.

## Scope

New package: **Tharga.Platform.Mcp**

- References `Tharga.Mcp` (foundation) and `Tharga.Team.Blazor` or `Tharga.Team.Service` (for auth/scope/audit primitives)
- `AddMcpPlatform()` extension on `IThargaMcpBuilder` — registers:
  - Platform-backed `IMcpContext` built from `HttpContext.User` + team claims
  - `IMcpContextAccessor` that resolves from the current `HttpContext`
  - Scope enforcement: MCP tool/resource calls go through `ScopeProxy` / `AccessLevelProxy` so the same audit log captures them as regular service calls
  - Endpoint authorization: `McpScope.System` requires `Roles.Developer`; `McpScope.Team` requires team membership
- New scope category `mcp:*` registered in Platform's scope registry with sensible defaults
- Works with Phase 0's collapsed single-endpoint design — scope enforcement happens on the provider's declared `IMcpProvider.Scope`, not via separate endpoints

## Architecture notes

- Phase 0 decision (2026-04-18): single `/mcp` endpoint. Platform bridge enforces scope per provider call, not per endpoint.
- No cyclic dependencies: `Tharga.Platform.Mcp` → `Tharga.Mcp` + `Tharga.Team.*`. The Platform packages have no reference back.
- Both Platform auth schemes work transparently: OIDC/cookie (web users) and API Key (`X-API-KEY` header). Both populate `HttpContext.User` with the same claim types (TeamKey, AccessLevel, Scope, role), so the bridge reads them uniformly.
- MCP endpoint requires authentication — anonymous requests are rejected at the HTTP layer before reaching providers.

## Acceptance criteria

- [ ] New `Tharga.Platform.Mcp` package builds and packs
- [ ] `AddMcpPlatform()` registers an `IMcpContext`/`IMcpContextAccessor` that reads user, team, developer role from the current `HttpContext`
- [ ] Tool invocations go through `ScopeProxy` / `AccessLevelProxy` so audit entries appear identically to regular service calls
- [ ] `McpScope.System` calls are rejected when the caller lacks `Roles.Developer`
- [ ] `McpScope.Team` calls are rejected when the caller has no team claim
- [ ] Built-in `mcp:*` scopes registered in Platform's scope registry
- [ ] MCP endpoint requires authentication (rejects anonymous requests)
- [ ] Works with both OIDC and API Key authentication schemes
- [ ] Unit tests cover each enforcement path (developer gate, team gate, audit logging, anonymous rejection, API-key auth path)
- [ ] All existing tests still pass
- [ ] Package published via the existing Release workflow

## Done condition

All acceptance criteria met, all tests pass, user confirms the feature is complete. Phase 1 status updated to Done in the master plan.
