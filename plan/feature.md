# Feature: ApiKeyView — surface per-key scope overrides

GitHub issue: [Tharga/Platform#71](https://github.com/Tharga/Platform/issues/71)

## Goal

Operators currently have no UI affordance to recognise or modify per-key `ScopeOverrides` on team API keys. The entity field exists and is honoured by the auth pipeline, but `ApiKeyView` (rendered on `/apikey`) neither shows nor edits it. Downstream this means narrow-scope keys (e.g. Quilt4Net.Server's Value Group keys minted with a single `valuegroup:read` scope) look indistinguishable from any other team key in the admin grid.

This feature adds an opt-in UI surface for viewing and editing `ScopeOverrides` per row, plus the service-layer plumbing required to persist edits.

## Scope

Three layers:

### 1. Service layer (`Tharga.Team.Service`)

- Add `Task SetScopeOverridesAsync(string teamKey, string key, string[] scopes)` to `IApiKeyAdministrationService` and `IApiKeyManagementService`. The management interface declares `[RequireScope(ApiKeyScopes.Manage)]` for parity with the rest of its surface. Implementation reads the entity, verifies team ownership, applies `entity with { ScopeOverrides = scopes }`, and persists via the existing `IApiKeyRepository.UpdateAsync`.
- Extend `CreateKeyAsync` on both interfaces with an optional `string[] scopeOverrides = null` parameter. Default `null` keeps the existing behaviour (no overrides on new keys); existing callers compile unchanged. `BuildKey` in `ApiKeyAdministrationService` is updated to set the field on the new entity.

### 2. Blazor layer (`Tharga.Team.Blazor.Features.Api.ApiKeyView`)

- New `[Parameter] public bool ShowScopeOverrides { get; set; }`. Default `false`. Mirrors the existing `ShowAuditLogButton` opt-in pattern so consumers can enable scope management on `/developer/apikey` without affecting `/apikey`, or vice versa.
- When `ShowScopeOverrides && _scopeRegistry != null && _options.AdvancedMode`:
  - **Display column.** A new "Scopes" column showing the comma-separated `ScopeOverrides` values for each row. The existing effective-scopes info tooltip stays.
  - **Create card field.** A `RadzenDropDown TValue="IEnumerable<string>"` with `Multiple="true"`, populated from `_scopeRegistry.All`. Selected values flow into the new `CreateKeyAsync` parameter.
  - **Edit-Scopes dialog per row.** A new icon button (e.g. `tune`) on each row opens a modal hosting a multi-select with the current values pre-checked. Save calls `SetScopeOverridesAsync`; cancel closes without persisting. Reuses Radzen `DialogService.OpenAsync` (the same primitive `OpenAuditLogAsync` already uses).
- No change to the existing effective-scopes info tooltip — it already shows the override/access-level/role provenance.

### 3. Documentation

- `Tharga.Team.Blazor/README.md` mentions the new parameter under the `ApiKeyView` section if there is one (verify in step 6).
- `docs/implementation-guide.md` clarifies that `ShowScopeOverrides` (the existing `ThargaBlazorOptions` flag) gates the **team-member** UI, while the new `[Parameter]` on `ApiKeyView` is independent — these are intentionally separate per-component controls.

## Behaviour

- **No data migration.** Existing keys have `ScopeOverrides == null`. The display column renders empty for those rows; nothing else changes.
- **No new scopes registered.** Mutation is gated by the existing `ApiKeyScopes.Manage` scope on `IApiKeyManagementService`.
- **No effect when `ShowScopeOverrides = false`.** Existing callers see no UI difference; defaults preserved.
- **Decorator pass-through.** `AuditingApiKeyServiceDecorator` *does* exist (correction from initial scoping). The new `SetScopeOverridesAsync` gets an audit-logging wrapper with action `"set-scope-overrides"`; the extended `CreateKeyAsync` keeps its existing audit wrapper with the new parameter forwarded through.

## Acceptance criteria

1. `IApiKeyAdministrationService` exposes `Task SetScopeOverridesAsync(string teamKey, string key, string[] scopes)`. `IApiKeyManagementService` mirrors it with `[RequireScope(ApiKeyScopes.Manage)]`.
2. `IApiKeyAdministrationService.CreateKeyAsync` and `IApiKeyManagementService.CreateKeyAsync` accept an optional `string[] scopeOverrides = null` parameter. Existing call sites compile unchanged.
3. `ApiKeyView` has `[Parameter] public bool ShowScopeOverrides { get; set; }` defaulting to `false`. With it on, the grid surfaces a Scopes column, the create card has a multi-select, and each row gets an Edit-Scopes icon button.
4. Edit-Scopes dialog calls `IApiKeyManagementService.SetScopeOverridesAsync` and refreshes the grid on save.
5. New unit tests cover: `SetScopeOverridesAsync` updates the entity and verifies team ownership; `CreateKeyAsync` with `scopeOverrides` sets the field on the persisted entity; `ApiKeyView.ShowScopeOverrides` parameter exists with `[Parameter]` attribute and bool type (reflection smoke test, matching `TeamComponentMemberNameEditTests` pattern).
6. `dotnet build -c Release` clean. `dotnet test -c Release` green.

## Done condition

PR opened from `feature/apikey-scope-overrides-ui` → `master`, all CI checks green, user has confirmed.

## Out of scope

- Editing `Roles` from the UI — the grid currently shows roles but has no edit affordance; out of this feature's scope.
- Inline-edit (vs dialog) for ScopeOverrides — rejected in planning (multi-select grid cells are cramped).
- `ScopeOverrides` on system keys — they already use `SystemScopes` (different field, set at creation only via `CreateSystemKeyAsync`). No UI change there.
- Validation that selected scopes are registered with the `IScopeRegistry` — the picker is populated from the registry, so out-of-band values aren't expected. Server-side validation can be added later if a non-UI caller (CLI, API) needs guardrails.
- Audit logging of `SetScopeOverridesAsync` invocations — no existing `AuditingApiKeyServiceDecorator` exists; we don't introduce one in this feature. The audit log already records the call via `ScopeProxy`.
- Framework package bumps (Microsoft.AspNetCore.* 9.0.x → 10.0.x on net9.0) — deferred, separate PR.
