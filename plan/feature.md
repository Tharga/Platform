# Feature: system-api-keys

**Originating branch:** master
**Date started:** 2026-04-20

## Goal

Introduce a second kind of API key — **system keys** — that are not bound to a team. Use cases: MCP gatekeeper credentials, CI/CD and monitoring callers, cross-team admin tooling.

The existing team-scoped key flow stays untouched. System keys live alongside it.

## Scope

### Storage
- Both team and system keys live in the **same** MongoDB collection (`ApiKeyRepositoryCollection`) — no new collection
- `ApiKeyEntity.TeamKey` stays `string`; the domain enforcement of non-null is relaxed so it can be `null` for system keys. MongoDB requires no schema migration
- New `SystemScopes` field on `ApiKeyEntity` to carry the explicit scope set granted at creation time (system keys aren't resolved through AccessLevel + team-role registry)
- New `CreatedBy` field on `ApiKeyEntity` — populated automatically from the current authenticated user at creation time. Applies to both team and system keys (useful in both contexts)

### Service layer
- Extend `IApiKeyAdministrationService` with system variants:
  - `CreateSystemKeyAsync(name, string[] scopes, DateTime? expiryDate)`
  - `GetSystemKeysAsync()` — IAsyncEnumerable
  - Existing `GetByApiKeyAsync`, `LockKeyAsync(teamKey:null, key)`, `DeleteKeyAsync(teamKey:null, key)`, `RefreshKeyAsync(teamKey:null, key)` already work if we accept null teamKey
  - Ownership check in internal verification: if `TeamKey == null` in storage, caller must not pass a non-null teamKey (and vice versa)
- New `IApiKeyManagementService` surface for the admin UI with `[RequireScope(ApiKeyScopes.SystemManage)]`

### Authentication
- `ApiKeyAuthenticationHandler` already doesn't require TeamKey at lookup time. Extend the claim-population path:
  - If `entity.TeamKey == null` → do NOT emit `TeamClaimTypes.TeamKey`; emit a new `TeamClaimTypes.IsSystemKey = "true"` claim
  - Scopes come from `entity.SystemScopes` directly, not resolved through `IScopeRegistry.GetEffectiveScopes(accessLevel, roles, overrides)`
- New scheme + policy:
  - `SystemApiKeyConstants.SchemeName = "SystemApiKeyScheme"` (can share handler — scheme discriminates at registration time or at decision time)
  - `SystemApiKeyConstants.PolicyName = "SystemApiKeyPolicy"` that requires the `IsSystemKey` claim
- Keep the existing `ApiKeyPolicy` strictly for team-scoped keys (requires TeamKey claim)

### Scope model
- New scope constant: `ApiKeyScopes.SystemManage = "apikey:system-manage"` — used on `[RequireScope]` attributes at the service/API layer
- **UI-level gating** uses the `Roles.Developer` role directly. System keys exist outside team context, so gating the admin surface by a team scope is awkward. Consumers can still map `apikey:system-manage` to the Developer role via `AddThargaTenantRoles` if they want finer control at the service layer.

### Admin UI
- New `<SystemApiKeyView />` component in `Tharga.Team.Blazor` — parallels `ApiKeyView` but:
  - Does not depend on `ITeamStateService`
  - Uses the system variant of `IApiKeyManagementService`
  - Shows a scope picker (multi-select from `IScopeRegistry`) instead of access level / roles / overrides
  - Rendered inside `<AuthorizeView Roles="@Roles.Developer">` — only developers see it
  - Shows `Name`, `CreatedAt`, `CreatedBy` columns
- Keep `<ApiKeyView />` unchanged except for showing the new `CreatedBy` column (small change)

### Audit
- System key invocations flow through `CompositeAuditLogger` as today. Tag with the key's friendly `Name` as `CallerIdentity` (same as team keys). No `TeamKey` in the audit entry when the call is system-scoped.
- Extend `AuditingApiKeyServiceDecorator` to cover the new `CreateSystemKeyAsync` / `GetSystemKeysAsync` methods
- Add metadata on `AuditEntry.Metadata`: `{ "ApiKeyType": "System", "CreatedBy": "<user>" }` for system-key-authenticated calls so the log is attributable even without a team
- Admin UI: the existing `<AuditLogView />` already renders metadata — no changes needed

## Out of scope

- Combining the two UI surfaces into a single `<ApiKeyView Scope="System|Team" />` (considered and rejected — cleaner to keep them separate)
- Rotating all consumers' existing admin UI registration to opt into system keys — consumers opt in explicitly by adding `<SystemApiKeyView />` to a Developer-only page

## Acceptance criteria

- [ ] `ApiKeyEntity.TeamKey` is nullable in the domain model
- [ ] `ApiKeyEntity.SystemScopes` field carries the explicit scope list for system keys
- [ ] `ApiKeyEntity.CreatedBy` field populated from the current authenticated user on creation
- [ ] `AuditingApiKeyServiceDecorator` covers the new system-key methods
- [ ] System-key auth calls produce audit entries with `ApiKeyType=System` and `CreatedBy` metadata, no `TeamKey`
- [ ] `IApiKeyAdministrationService` has `CreateSystemKeyAsync` + `GetSystemKeysAsync`
- [ ] System-variant methods on `IApiKeyManagementService` gated by `apikey:system-manage`
- [ ] `ApiKeyAuthenticationHandler` populates system keys correctly (no TeamKey claim, scopes from `SystemScopes`)
- [ ] `SystemApiKeyPolicy` registered — protects endpoints that should only accept system keys
- [ ] `<SystemApiKeyView />` renders, creates, refreshes, locks, and deletes system keys
- [ ] Existing `<ApiKeyView />` and team-key flow unchanged
- [ ] All existing tests pass; new tests cover system-key create, list, auth, policy rejection (team key hitting system endpoint and vice versa), scope validation
- [ ] README / docs updated with a short "system keys" section and the PlutusWave/MCP use case

## Done condition

All acceptance criteria met, all tests pass, user confirms the feature is complete.
