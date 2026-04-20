# Plan: mcp-platform-data (system slice)

## Steps

1. [x] Add `ExposeSystemResources` (default false) to `McpPlatformOptions`
2. [x] Create `PlatformSystemResourceProvider` implementing `IMcpResourceProvider`
   - Injects `IApiKeyAdministrationService`, `ITenantRoleRegistry`, `CompositeAuditLogger` (all optional)
   - `ListResourcesAsync` returns descriptors when caller is Developer, empty otherwise; audit only listed when logger is registered
   - `ReadResourceAsync` throws `UnauthorizedAccessException` for non-developer callers
   - `SystemKeys` resource redacts raw ApiKey / ApiKeyHash fields
3. [x] In `AddMcpPlatform`, register the provider via `builder.AddResourceProvider<T>()` when `ExposeSystemResources == true`
4. [x] Tests — 9 provider tests + 2 registration tests
5. [x] README: "System-scope diagnostic resources" section added
6. [x] Run full test suite — 235 tests pass
7. [ ] Archive plan, delete plan/, final commit, push, PR

Note: `ITeamService` / `ITeamRepository` don't expose a cross-tenant team listing today, so `platform://system/teams` (and per-team API-key listing) is deferred. Would require a new interface method, tracked separately.
