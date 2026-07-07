# Plan: Dynamic tenant role follow-ups (#120 + #121)

Branch: `feature/dynamic-role-followups` (from `master`, GitHub Actions CI).

## Status legend
`[ ]` todo · `[~]` in progress · `[x]` done

## Steps

### 0. Package updates (mandatory up-front)
- [x] `dotnet outdated` across the solution — **"No outdated dependencies were detected"** (checked at feature start, 2026-07-07). No-op; will re-check at close-out.

### 1. #121 — configurable custom-role CRUD scope
- [x] Add `DynamicTenantRoleOptions` (in `Tharga.Team`) with `string ManageScope` defaulting to `TeamScopes.Manage`.
- [x] Add overload `AddThargaDynamicTenantRoles(Action<DynamicTenantRoleOptions> configure = null)` — builds options, `TryAddSingleton` the options, keeps `ITenantRoleService`. Existing no-arg call unaffected.
- [x] Surface on the facade: `ThargaPlatformOptions.DynamicRoleManageScope`; `ThargaPlatformRegistration` passes it into `AddThargaDynamicTenantRoles` when `EnableDynamicRoles`.
- [x] Flow the scope into `AuthorizationTeamServiceDecorator` (new optional ctor param, default `team:manage`; resolved from `DynamicTenantRoleOptions` in the `DecorateWithAuthorization` factory). `SetTeamCustomRolesAsync` uses it.
- [x] `TenantRoleManager.razor`: resolves `DynamicTenantRoleOptions` (fallback `team:manage`); `_manageScope` drives the `_canManage` check and warning text.
- [x] Tests: extended `AuthorizationTeamServiceDecoratorCustomRolesTests` (custom scope gates + team:manage insufficient); facade + extension registration tests in `AddThargaPlatformTests`.

### 2. #120 — ApiKeyView offers team custom roles
- [x] Checked `RoleSelectionResolver` — it's for visible/hidden partitioning, not source selection. Extracted a new `ApiKeyRolePicker` Framework helper instead (testable, no bUnit needed), mirroring the service-based merge.
- [x] `ApiKeyView.razor`: resolves `ITenantRoleService`; `_rolesAvailable` + `_roleDefinitions` come from `ApiKeyRolePicker` (merged `GetRolesAsync` when service present, else `ITenantRoleRegistry.All`). Reordered to resolve after team load. Widened the 3 role-picker gates from `_tenantRoleRegistry != null` to `_rolesAvailable` so a custom-only team (no code roles) still gets a picker.
- [x] Tests: `ApiKeyRolePickerTests` (6 cases: source selection + gate). Merge itself already covered by `TenantRoleServiceTests`.

### 3. Verify
- [x] `dotnet build -c Release` — 0 errors.
- [x] `dotnet test -c Release` (full suite) — **550 passed, 0 failed, 0 skipped**.
- [ ] Live UI verification deferred to user testing on the pushed branch (per Feature Workflow — sample app needs Azure AD + Mongo + browser). DI wiring is exercised by `AddThargaPlatformTests` building the provider.

### 4. Docs — DONE (fold into feature commit; `plan/` still on branch)
- [x] Updated `Tharga.Team.Blazor/README.md`, `Tharga.Team/README.md`, and `docs/articles/implementation-guide.md`: document `DynamicRoleManageScope`/`ManageScope` and API-key custom-role assignment.

### 5. Close-out (only after user confirms the feature is done)
- [ ] Re-run `dotnet outdated`; apply + include any new updates.
- [ ] Version bump — **patch** bump (user directive: no minor bump), e.g. next `3.1.x`; confirm exact number against current.
- [ ] Update `Requests.md`: mark #120/#121 done + add `## Follow-up` consumer entry for FortDocs.
- [ ] Update GitHub issues #120/#121 (comment + close on merge).
- [ ] Archive `plan/feature.md` → Plan directory `done/dynamic-role-followups.md`; `git rm -r plan`.
- [ ] Final commit `feat: dynamic-role-followups complete`; push; open PR → `master`.

## Notes / decisions
- Both changes are additive and non-breaking; `ManageScope` default = `team:manage` preserves current behavior.
- Decorator resolves the scope at runtime via the DI factory, so `AddThargaDynamicTenantRoles` being registered after `AddThargaTeamBlazor` is fine.
- **Version bump: patch, not minor** (user directive 2026-07-07), despite the additive API surface.

## Last session
2026-07-07 — Implemented both #121 and #120 end-to-end. #121: configurable custom-role manage scope via `DynamicTenantRoleOptions.ManageScope` / facade `DynamicRoleManageScope`, flowed into the decorator + `TenantRoleManager`. #120: `ApiKeyView` role picker sources the per-team merged set via new `ApiKeyRolePicker` helper + widened gates. Docs updated (README ×2 + implementation-guide). Build clean; full suite 550 green. **Next:** commit code + docs + plan; push branch; ask user to test the two UI flows. Close-out (version bump, Requests.md/issue updates, plan removal) only after user confirms.
