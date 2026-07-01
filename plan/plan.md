# Plan: Per-team tenant-role visibility hook (#113)

## Steps

- [x] 1. **NuGet updates (up front, bundled into this PR).** Applied solution-wide:
      `Microsoft.Identity.Web` 4.11.0→4.12.0, `Tharga.MongoDB` 2.11.2→2.11.3. Build + full test
      suite green (493 tests passed). Committed.
- [~] 2. Add `ITenantRoleVisibilityProvider` interface in `Tharga.Team` (next to `ITenantRoleRegistry`).
- [ ] 3. Add default identity implementation (all roles visible); register via `TryAddSingleton`
      in the tenant-role service-collection extension so it's overridable and non-breaking.
- [ ] 4. `TeamComponent`: resolve the provider, build a per-team visible-role map during team load,
      pass the team's visible list to each row's `RoleEditor`.
- [ ] 5. `RoleEditor`: preserve assigned-but-hidden roles on change (union hidden-selected into the
      emitted value) so hidden assignments are never pruned.
- [ ] 6. Tests: default provider visibility; overridable registration; `TeamComponent` per-team
      filtering (bUnit); hidden-assigned preservation.
- [ ] 7. Docs: update `Tharga.Team` + `Tharga.Team.Blazor` README and the docs site article for the
      new hook. Re-run `dotnet outdated` at finalization.
- [ ] 8. Close-out: archive feature.md to Plan dir `done/`, `git rm -r plan`, final commit, open PR
      develop → master.

## Notes / decisions
- Branch base: local `develop` (already contains all of master); PR target `master` (per user).
- Hidden is a UI-editor concern only — scope resolution unchanged; a hidden-but-assigned role still
  grants scopes at runtime.

## Last session
Feature branch `feature/tenant-role-visibility` created off develop. Plan drafted. Next: step 1
(NuGet updates).
