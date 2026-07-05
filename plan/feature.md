# Feature: Runtime-defined (dynamic) tenant roles per team (GitHub #117)

## Goal
Let a team administrator create / update / delete their **own** custom roles at runtime (each with a
chosen set of allowed scopes), persisted in MongoDB, surfaced alongside the code-registered roles so
existing consumers (role pickers, scope resolution, member assignment) pick them up uniformly — with
scopes constrained to app-registered scopes (no privilege escalation) and no host code deploy.

Driver: FortDocs' `/access/groups` placeholder becomes a real role admin; reference roles
(Registrar/CaseOfficer/Reader/Archivist) no longer need to be hard-coded in `Program.cs`.

## Design (confirmed 2026-07-05)

**Storage type:** reuse `TenantRoleDefinition (Name, Scopes, Description)` for both the persisted and
surfaced shape — custom roles are literally the same type the picker already renders.

**Store (embedded, mirrors `ConsentedRoles`):** `CustomRoles` on `ITeam` (default interface member
`=> null`, non-breaking) + `TeamEntityBase<T>` (`[BsonIgnoreIfNull]`). Persist via `SetCustomRolesAsync`
on `ITeamRepository`/`TeamRepository` (`Set(x => x.CustomRoles, roles)`, mirrors `SetConsentAsync`).

**Write path (audited + authorized):** whole-array `SetTeamCustomRolesAsync(teamKey, roles)` on
`ITeamService` / `TeamServiceBase` (+ abstract bridge `SetTeamCustomRolesInternalAsync`) /
`ITeamManagementService` (`[RequireScope(TeamScopes.Manage)]`). *Defining* roles = `team:manage`;
*assigning* them to members stays `member:manage` (unchanged). The UI provides create/edit/delete
affordances that build the new array and call the setter.

**Authorization + validation** in `AuthorizationTeamServiceDecorator.SetTeamCustomRolesAsync` (the
registered `ITeamService`; protects REST + UI; injects `IScopeRegistry`/`ITenantRoleRegistry` — no
`TeamServiceBase` ctor ripple):
- requires `team:manage` on the target team (`RequireTeamScopeAsync`),
- **privilege-escalation guard:** every role scope must be in `IScopeRegistry.All`,
- structural validation: non-empty trimmed names, unique within the set, no collision with code-role names.
Audited as `set-custom-roles` in `AuditingTeamServiceDecorator`.

**Read/resolution (the team-context fix):** new parallel `ITenantRoleService` (async, team-aware),
composing code roles (`ITenantRoleRegistry`) + this team's custom roles
(`ITeamService.GetTeamCustomRolesAsync`) + `IScopeRegistry`:
- `GetRolesAsync(teamKey)` → merged code+custom list (for pickers),
- `GetEffectiveScopesAsync(teamKey, accessLevel, roleNames, overrides)` → single team-aware resolver.
The two claims paths (`TeamServerClaimsTransformation`, `TeamClaimsAuthenticationStateProvider`) and
`ApiKeyAuthenticationHandler` switch to `GetEffectiveScopesAsync`, with null-tolerant fallback to the
existing `ScopeRegistry.GetEffectiveScopes` when the feature is off.

**Feature gate:** `ThargaPlatformOptions.EnableDynamicRoles` (default false → zero behavior change,
non-breaking). When on, registers `ITenantRoleService` + the management surface. Role *visibility* keeps
honoring the existing `ITenantRoleVisibilityProvider` (already `teamKey`-aware).

**UI (full management component):** reusable Blazor `<TenantRoleManager />` — list/create/edit/delete a
team's custom roles, scope multiselect from `IScopeRegistry.All`, `team:manage`-gated, calls
`ITeamManagementService`. Plus custom roles appear in the existing role picker (`TeamComponent`/
`RoleEditor`) alongside code roles via `ITenantRoleService.GetRolesAsync(team.Key)`.

## Acceptance criteria
- [ ] Custom roles persist per team in Mongo (`CustomRoles` on the Team document).
- [ ] `SetTeamCustomRolesAsync` requires `team:manage` on the team; rejects unregistered scopes,
      duplicate names, and code-role-name collisions.
- [ ] A member assigned a custom role receives that role's scopes as claims for that team (server + WASM
      + API-key paths).
- [ ] Code-registered roles still work unchanged; feature off (default) = no behavior change.
- [ ] Custom roles appear in the role picker alongside code roles (respecting the visibility provider).
- [ ] `<TenantRoleManager />` supports create/edit/delete with scope selection, gated by `team:manage`.
- [ ] README + implementation-guide document the feature and the component.
- [ ] `dotnet build -c Release` + `dotnet test -c Release` green; end-to-end verified via the sample app.

## Done condition
All acceptance criteria met, docs updated, PR opened to `master` with a release-note-quality description.
