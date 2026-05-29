# Plan: ApiKeyView — surface per-key scope overrides

## Steps

- [x] **1. Service layer — `IApiKeyAdministrationService`**
  - Add `Task SetScopeOverridesAsync(string teamKey, string key, string[] scopes);` to the interface.
  - Extend the signature of `CreateKeyAsync` with `string[] scopeOverrides = null` (positioned before `expiryDate`).
  - Implementation in `ApiKeyAdministrationService`:
    - `SetScopeOverridesAsync` — load via `_repository.GetAsync(key)`, call `VerifyTeamOwnership(item, teamKey)`, build `item with { ScopeOverrides = scopes }`, persist via `_repository.UpdateAsync(key, updated)`.
    - Update `CreateKeyAsync` to forward `scopeOverrides` into `BuildKey`. Pass `null` (not `Array.Empty<string>`) when the caller didn't set anything — keeps the `BsonIgnoreIfNull` attribute on the entity in play.
    - Update private `BuildKey` to accept and assign `ScopeOverrides`.

- [x] **2. Service layer — `IApiKeyManagementService` (user-facing wrapper)**
  - Add `Task SetScopeOverridesAsync(string teamKey, string key, string[] scopes);` to the interface with `[RequireScope(ApiKeyScopes.Manage)]`.
  - Extend `CreateKeyAsync` signature with `string[] scopeOverrides = null`.
  - Update `ApiKeyManagementService` (the concrete delegation class) to forward both: `_inner.SetScopeOverridesAsync(...)` and the extended `_inner.CreateKeyAsync(...)`.

- [x] **3. Blazor layer — `ApiKeyView.razor`**
  - Add `[Parameter] public bool ShowScopeOverrides { get; set; }` next to the existing `ShowAuditLogButton` parameter.
  - In the grid template, after the existing roles/expiry/info columns and gated by `_options.AdvancedMode && _scopeRegistry != null && ShowScopeOverrides`:
    - Add a new `RadzenDataGridColumn Title="Scopes"` showing `string.Join(", ", context.ScopeOverrides ?? Array.Empty<string>())`.
    - Add a new action button `<RadzenButton Icon="tune" Click="@(_ => OpenEditScopesAsync(context))" />` inside the existing actions column. Update `GetActionsColumnWidth()` to account for the extra 40px when `ShowScopeOverrides` is on.
  - In the create card, gated by the same condition, add a `RadzenDropDown TValue="IEnumerable<string>" Multiple="true"` bound to `_newScopeOverrides`, populated from `_scopeRegistry.All.Select(s => s.Name)`. Pass it through to `_apiKeyManagementService.CreateKeyAsync(...)` and reset on success.
  - New `OpenEditScopesAsync(ApiKeyModel context)` method that opens a dialog. Dialog content is a small component or inline `RenderFragment` with a multi-select pre-populated from `context.ScopeOverrides` plus Save/Cancel buttons. On Save: call `_apiKeyManagementService.SetScopeOverridesAsync(_selectedTeam.Key, context.Key, selected.ToArray())` then `await ReloadData()`.

- [x] **4. Tests**
  - `Tharga.Team.Service.Tests/ApiKeyAdministrationServiceTests.cs` — append:
    - `SetScopeOverridesAsync_Updates_Entity` — substitute `IApiKeyRepository.GetAsync` to return a known entity for team `T-1`; assert `UpdateAsync` received an entity with the new `ScopeOverrides`.
    - `SetScopeOverridesAsync_WrongTeam_Throws` — entity belongs to T-1; calling with T-2 throws `UnauthorizedAccessException`.
    - `CreateKeyAsync_With_ScopeOverrides_Sets_Field` — assert the entity passed to `_repository.AddAsync` has the expected `ScopeOverrides` array.
  - `Tharga.Team.Blazor.Tests/ApiKeyViewScopeOverridesTests.cs` (new file) — reflection smoke test in the existing `TeamComponentMemberNameEditTests` style: assert `ApiKeyView.ShowScopeOverrides` exists, is `bool`, has `[Parameter]` attribute, and defaults to `false`.

- [x] **5. Build + full test suite**
  - `dotnet build -c Release` clean.
  - `dotnet test -c Release` green.

- [x] **6. README and docs**
  - `Tharga.Team.Blazor/README.md`: brief mention of the new `ShowScopeOverrides` parameter on `ApiKeyView` if the README documents component parameters (verify).
  - `docs/implementation-guide.md`: clarify that the existing `ThargaBlazorOptions.ShowScopeOverrides` flag gates the **team-member** UI (TeamComponent), while the new `ApiKeyView.ShowScopeOverrides` `[Parameter]` is independent and per-component.

- [x] **7. Commit + push the feature branch**
  - Conventional prefix: `feat:` — this adds new public surface.
  - Suggested message: `feat: ApiKeyView scope-overrides UI + SetScopeOverridesAsync service API`.

- [x] **8. Pause for user verification.** Plan/ stays on the feature branch; deleted in the close-out commit before the PR opens.

## Verification approach

- After step 1+2, build `Tharga.Team.Service` and confirm no callers broke. The new `scopeOverrides` parameter is optional so existing call sites should compile unchanged.
- After step 3, build `Tharga.Team.Blazor`. Open the sample app locally (`Tharga.Platform.Sample`) and smoke-test on `/apikey` and `/developer/apikey` if either is set up with `ShowScopeOverrides`.
- After step 4, run the focused service+blazor test projects to confirm new assertions pass before running the full suite in step 5.

## Open questions

(none — three design choices locked via `AskUserQuestion` during planning: per-component `[Parameter]`, dialog + create-card multi-select, both `Create+scopeOverrides` and `SetScopeOverridesAsync`)

## Last session
2026-05-27 — All implementation steps complete. 7 new tests (5 service-layer + 2 Blazor smoke); 325 total green. READMEs and implementation guide updated. The `AuditingApiKeyServiceDecorator` did exist (correction from initial scoping) — its `CreateKeyAsync` was updated and a new `SetScopeOverridesAsync` audit-logging wrapper added. Ready for commit + push + user verification.
