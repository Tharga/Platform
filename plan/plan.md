# Plan: Authorization defects

Branch: `feature/authorization-defects` (from `master`)
Spec: `planned/02-authorization-defects.md`

## Steps

- [x] 1. NuGet package pass (mandatory feature-start step)
  - `Tharga.MongoDB` 2.14.1 → 2.14.2 applied (carried from the abandoned `wasm-safe-blazor` branch).
  - `SixLabors.ImageSharp` 3.1.12 → 4.0.0 deliberately held — 4.0 requires a paid Six Labors build-time
    licence. Pre-existing, intentional.

- [ ] 2. Baseline build + test, recorded before any change

- [ ] 3. Failing tests for defect 1
  - Escalation: a caller sets a member to `Owner` via `SetMemberRoleAsync` → must be refused.
  - Demotion: a caller sets the current `Owner` to a lower level → must be refused.
  - Both must fail against current code before the fix.

- [ ] 4. Guard `SetMemberRoleAsync` in `TeamServiceBase`
  - The invariant is a domain rule, so it belongs in the domain, not the authorization decorator (rule 2).
  - Refuse `accessLevel == Owner`; refuse when the target member is currently `Owner`.
  - Message should point at `TransferOwnershipAsync`, matching `RemoveMemberAsync`'s existing wording.
  - Verify `TransferOwnershipAsync` (which calls the protected `SetTeamMemberRoleAsync`) and team creation
    are unaffected.

- [ ] 5. **DECISION NEEDED** — provenance approach
  - (a) Distinct claim type for system scopes, e.g. `TeamClaimTypes.SystemScope`. Fixes the class.
        Breaking for consumers reading `Scope` claims directly.
  - (b) Distinct scope names for system-wide variants (`audit:read:all`). Cheaper; relies on nobody
        registering the wide name at an access level.
  - (c) Require a pin on `AuditLogView`. Narrowest; leaves the ambiguity.
  - Preference: (a).

- [ ] 6. Implement the chosen approach
  - Emission: `TeamServerClaimsTransformation` (system roles) vs `TeamMembershipClaimsBuilder` (access level).
  - Consumption: `TeamScopePolicy` and `TeamScopeGate` are the only two places the distinction is read, so
    the blast radius inside the library is small.
  - `AuditLogView` and `ApiKeyView` gate on the correct one.

- [ ] 7. Tests for defect 2
  - A team-level grant does not satisfy a system-scope check.
  - A system-role grant does not satisfy a team-scope check for a team the caller has no membership in.
  - Unpinned `AuditLogView` requires a system-level grant.

- [ ] 8. Sample configuration
  - Developer role keeps only system scopes. `ApiKeyScopes.Manage` and `audit:read` already removed from the
    map locally — confirm and make consistent with whichever approach step 5 picks.
  - `/system-api-keys` must still work for a Developer.

- [ ] 9. Full suite, then docs review
  - `docs/articles/user-management.md` and `implementation-guide.md` describe the scope model; the
    team-vs-system distinction becomes load-bearing and needs stating.

- [ ] 10. Close out
  - Re-run `dotnet outdated`, archive `plan/feature.md` to `done/`, remove `plan/`, final commit, push, PR
    with the migration spelled out.

## Notes

- Master is 2 commits ahead of origin (`998be59` mission.md, `0a3a9da` cleanup). Those will appear in the PR
  unless master is pushed first — worth doing before opening it.
- `Tharga.Team.Mcp`'s own `IMcpScopeChecker` is a second enforcement point but not a live hole; it is
  deliberately left to plan 05.

## Last session

2026-07-27 — Branch created, package pass done. Both defects confirmed by reading: `SetMemberRoleAsync` has no
guard in either direction, and `TransferOwnershipAsync` uses the protected method so the fix is safe. Plan
written; step 5 needs a decision before step 6.
