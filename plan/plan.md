# Plan: mcp-platform-bridge

## Steps

### Package setup
1. [x] Create new `Tharga.Platform.Mcp` project (net9.0;net10.0, matches other packages)
2. [x] Add project refs to `Tharga.Team.Service` (has ScopeProxy/AccessLevelProxy/audit) and package ref to `Tharga.Mcp`
3. [x] Add to solution, update csproj metadata (Description, etc.)
4. [ ] Update release.yml / build workflow to pack the new package

### Core bridge
5. [x] Create `PlatformMcpContext` implementing `IMcpContext` — reads UserId, TeamId, IsDeveloper from a `ClaimsPrincipal`
6. [x] Create `HttpContextMcpContextAccessor` implementing `IMcpContextAccessor` — builds context from `IHttpContextAccessor` on demand
7. [x] Create `McpPlatformOptions` for consumer configuration (DeveloperRole with default "Developer")

### Registration
8. [x] Create `McpPlatformBuilderExtensions.AddMcpPlatform(this IThargaMcpBuilder builder)`: replaces IMcpContextAccessor, registers IMcpScopeChecker, registers mcp:* scopes
9. [x] Add `MapMcpPlatform()` that maps MCP and applies `RequireAuthorization()` when `ThargaMcpOptions.RequireAuth == true`

### Scope enforcement
10. [x] Create `IMcpScopeChecker` + implementation for imperative scope checks inside tool methods
11. [x] Register `mcp:discover` scope with `AccessLevel.Viewer`

Note: per-tool scope-class enforcement (User/Team/System) deferred — the Phase 0 foundation uses the SDK's attribute-based tool discovery (`[McpServerTool]`), which doesn't surface `IMcpProvider.Scope` through invocation. Tools enforce this themselves via `IMcpScopeChecker` and reading `IMcpContext.IsDeveloper`/`TeamId`.

### Tests (Tharga.Platform.Mcp.Tests)
12. [x] Test: `PlatformMcpContext` reads claims correctly (user/team/developer/anonymous) — 7 tests
13. [x] Test: `McpScopeChecker.Require` throws when scope missing — 5 tests
14. [x] Test: `HttpContextMcpContextAccessor` derives context from HttpContext — 3 tests
15. [x] Test: `AddMcpPlatform` registers context accessor, scope checker, mcp:* scopes, honors custom DeveloperRole — 4 tests

### Verify & ship
16. [x] Full build + test suite passes — 208 tests (19 new + 189 existing)
17. [ ] Update README.md with Tharga.Platform.Mcp section and the getting-started snippet
18. [ ] Update release workflow to pack the new package
19. [ ] Commit, push, create PR to master
