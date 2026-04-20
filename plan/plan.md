# Plan: system-api-keys

## Steps

### Domain + storage
1. [x] Add `IApiKey.SystemScopes` and `CreatedBy`; extend `ApiKeyEntity` with BSON attributes
2. [x] Add `ApiKeyScopes.SystemManage = "apikey:system-manage"` constant
3. [x] Add `TeamClaimTypes.IsSystemKey = "IsSystemKey"` constant

### Service layer
4. [x] Extend `IApiKeyAdministrationService` with `CreateSystemKeyAsync`, `GetSystemKeysAsync`, `RefreshSystemKeyAsync`, `LockSystemKeyAsync`, `DeleteSystemKeyAsync`
5. [x] Implement in `ApiKeyAdministrationService` — builds entity with `TeamKey=null`, stores `SystemScopes`, `CreatedBy`
6. [x] `VerifyTeamOwnership` rejects system keys; new `VerifySystemKey` rejects team keys
7. [x] Extend `IApiKeyManagementService` with system variants, decorated with `[RequireScope(ApiKeyScopes.SystemManage)]`
8. [x] `ApiKeyManagementService` pass-through with CreatedBy derived from `IHttpContextAccessor`

### Authentication
9. [x] `ApiKeyAuthenticationHandler` branches on `TeamKey == null`: system keys get `IsSystemKey=true` + explicit scopes; team keys unchanged
10. [x] `ApiKeyConstants.SystemPolicyName = "SystemApiKeyPolicy"` added
11. [x] `SystemApiKeyPolicy` registered: requires `IsSystemKey=true`
12. [x] `ApiKeyPolicy` tightened: rejects principals with `IsSystemKey=true`

### Admin UI
13. [x] `Tharga.Team.Blazor/Features/Api/SystemApiKeyView.razor` + `SystemApiKeyModel.cs`
14. [x] Uses scope multi-select (from `IScopeRegistry.All`)
15. [x] Gated by `<AuthorizeView Roles="@Roles.Developer">`
16. [x] Shows Name, Key, Scopes, Expiry, Created, Created By

### Audit
17. [x] `AuditingApiKeyServiceDecorator` extended to cover system-key methods with `ApiKeyType=System` metadata

### Tests
18. [x] `SystemApiKeyAdministrationServiceTests` — create/list/refresh/lock/delete, cross-kind rejection, CreatedBy persistence (9 tests)
19. [x] `SystemApiKeyAuthenticationHandlerTests` — IsSystemKey claim, scopes from SystemScopes, team-key separation (4 tests)

### Docs + close
20. [x] `Tharga.Team.Service/README.md` updated with "System keys" section
21. [x] Full test suite: 221 tests pass
22. [ ] Archive plan to Obsidian, delete plan/, final commit, push, PR
