# Plan: Per-team tenant-role visibility hook (#113)

## Steps

- [x] 1. **NuGet updates (up front, bundled into this PR).** Applied solution-wide:
      `Microsoft.Identity.Web` 4.11.0→4.12.0, `Tharga.MongoDB` 2.11.2→2.11.3. Build + full test
      suite green (493 tests passed). Committed.
- [x] 2. Added `ITenantRoleVisibilityProvider` interface in `Tharga.Team`.
- [x] 3. Added `AllRolesVisibleTenantRoleVisibilityProvider` (identity); registered via
      `TryAddSingleton` in `AddThargaTenantRoles` — overridable, non-breaking.
- [x] 4. `TeamComponent` resolves the provider, builds `_visibleRolesByTeam` in `ReloadTeams` via
      `BuildVisibleRolesAsync`, and renders each row's `RoleEditor` with `GetVisibleRoles(team.Key)`.
- [x] 5. `RoleEditor` preserves assigned-but-hidden roles via `RoleSelectionResolver.Split/Merge`.
- [x] 6. Tests (no bUnit in this project → logic extracted to testable `RoleSelectionResolver`):
      6 resolver tests (split/merge/preservation) + 3 Service tests (default visibility, default
      registration, consumer override). Full suite green: 502 passed.
- [x] 7. Docs: updated `Tharga.Team` README (authorization list), `Tharga.Team.Blazor` README (new
      "Per-team role visibility" subsection), and docs-site `implementation-guide.md` Step 7
      ("Hiding roles per team"). `dotnet outdated` re-run deferred to finalization (step 8).
- [~] 8. Close-out (after user tests + confirms): re-run `dotnet outdated`, archive feature.md to
      Plan dir `done/`, `git rm -r plan`, final commit, open PR develop → master.

## Notes / decisions
- Branch base: local `develop` (already contains all of master); PR target `master` (per user).
- Hidden is a UI-editor concern only — scope resolution unchanged; a hidden-but-assigned role still
  grants scopes at runtime.

## Last session
Feature branch `feature/tenant-role-visibility` created off develop. Plan drafted. Next: step 1
(NuGet updates).
