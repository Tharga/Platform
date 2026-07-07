# Feature: Dynamic tenant role follow-ups (#120 + #121)

## Goal
Close the two remaining UI/config gaps in the runtime-defined (dynamic) tenant roles feature
(shipped in PR #119 / #117), so Eplicta FortDocs can complete its EP-4580 rollout. Both are
additive and non-breaking; dynamic roles stay off by default.

## Scope

### #121 — Make custom-role CRUD scope configurable (not only `team:manage`)
Custom-role create/edit/delete is currently hard-gated on `team:manage` in two places:
- `AuthorizationTeamServiceDecorator.SetTeamCustomRolesAsync` (service-layer enforcement)
- `TenantRoleManager.razor` (`_canManage` guard + warning text)

Make the required scope configurable, defaulting to `team:manage` for back-compat:
- New `DynamicTenantRoleOptions { ManageScope }` (default `TeamScopes.Manage`).
- `AddThargaDynamicTenantRoles(Action<DynamicTenantRoleOptions> configure = null)` overload.
- Surfaced through the facade: `AddThargaPlatform(o => { o.EnableDynamicRoles = true; o.DynamicRoleManageScope = "access:manage"; })`.
- Decorator + `TenantRoleManager` honor the configured scope. Validation (privilege-escalation
  guard, dupes, code-role collisions) is unchanged.

### #120 — `ApiKeyView` role picker doesn't offer team custom (dynamic) roles
`ApiKeyView.razor` fills its role picker from `ITenantRoleRegistry.All` (code roles only). With
dynamic roles enabled, custom roles can't be assigned to team API keys via the UI — even though
the backend already resolves them (`ApiKeyAuthenticationHandler` → `GetEffectiveScopesAsync`).
- Source the picker from `ITenantRoleService.GetRolesAsync(teamKey)` when the service is
  registered (dynamic roles on), falling back to `ITenantRoleRegistry.All` otherwise. Mirrors
  `TeamComponent.RolesForTeam(team)`.
- System keys unaffected (they use `SystemScopes`).

## Out of scope
- Cross-tenant team listing / other dynamic-role work.
- Any breaking change. `ManageScope` defaults preserve current behavior exactly.

## Acceptance criteria
- [ ] `AddThargaDynamicTenantRoles(o => o.ManageScope = "...")` compiles and gates custom-role CRUD
      on the configured scope; omitting it keeps `team:manage`.
- [ ] `AddThargaPlatform` surfaces the same via `o.DynamicRoleManageScope`.
- [ ] `AuthorizationTeamServiceDecorator.SetTeamCustomRolesAsync` denies callers without the
      configured scope and allows callers with it; default path unchanged.
- [ ] `TenantRoleManager` gates its editor and warning text on the configured scope.
- [ ] `ApiKeyView` role picker (create card + edit dialog) lists code roles ∪ the team's custom
      roles when dynamic roles are enabled; only code roles otherwise.
- [ ] New/updated unit + bUnit tests cover both; full `dotnet test -c Release` suite green.
- [ ] Consumer-facing docs (README + docs/articles) updated.

## Done condition
All acceptance criteria met, tests green, docs updated, `Requests.md` + GitHub issues #120/#121
updated on completion (after user confirmation), `plan/` removed in the close-out commit, PR opened
`feature/dynamic-role-followups` → `master`.
