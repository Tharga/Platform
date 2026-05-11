# Feature: Resilient member lookup

GitHub issue: [Tharga/Platform#64](https://github.com/Tharga/Platform/issues/64)

## Goal

Drive `ThrowMoreThanOneMatchException` to zero across the Tharga.Platform stack when a `Team.Members` (or `ApiKey`) collection happens to contain duplicate rows for the same key. Replace every brittle `.Single(predicate)` / `.SingleOrDefault(predicate)` site with a resilient lookup that picks the first match and logs a warning carrying enough context (team key, member key, call site) to find and clean the duplicates out of band.

This is the comprehensive follow-up to [#59](https://github.com/Tharga/Platform/issues/59), which patched only `TeamComponent.RemoveUserFromTeam`. Verified on Quilt4Net.Server v4.2.17.0 that #59 landed cleanly but moved the throw inside `TeamServiceBase.RemoveMemberAsync` (3 hits in 24h post-deploy) and exposed `TeamComponent.CopyInvitationLink` (1 hit).

## Scope

All Platform projects (per user direction — broader than the issue's stated scope of Tharga.Team + Tharga.Team.Blazor + Tharga.Team.Service.Audit). Sites identified:

- `Tharga.Team.MongoDB/TeamRepository.cs` — 6 `.Single` sites
  - `SetLastSeenAsync`, `SetMemberRoleAsync`, `SetMemberTenantRolesAsync`, `SetMemberScopeOverridesAsync`, `SetMemberNameAsync`, `SetInvitationResponseAsync`
  - Compounder: each site is followed by `Where(x => x.Key != userKey)` (or by `InviteKey`) which strips sibling rows with the same key. Fix the strip pattern at the same time so duplicates are preserved, not silently dropped.
- `Tharga.Team/TeamServiceBase.cs` — 4 sites
  - `RemoveMemberAsync` (line 123), `TransferOwnershipAsync` (lines 225, 229), `AssureAccessLevel` helper (line 270)
- `Tharga.Team.Blazor/Features/Team/TeamComponent.razor` — 1 site (`CopyInvitationLink`, line 611)
- `Tharga.Team.Blazor/Features/Team/TeamInviteView.razor` — 1 site (invitation-code lookup, line 43)
- `Tharga.Team.Service/ApiKeyAdministrationService.cs` — 2 sites (api-key-hash collision lookup, lines 37, 43)

Excluded (intentional `.Single` semantics, not duplicate-data-related):
- `TeamStateService.cs:66` — `teams.Single()` guarded by a `Count() == 1` precondition (auto-select-when-exactly-one).
- xUnit `Assert.Single(...)` in test projects.
- Reflection `.Single(t => t.Namespace == ...)` in tests for type lookups.

## Behaviour

Per user direction:
- **Resilient read + warn.** No self-heal in this pass — duplicates are cleaned out-of-band.
- Warning payload includes `teamKey`, the lookup key (e.g. `userKey` or `inviteKey`), the call site, and the match count.
- Server-side strip-siblings fix on `TeamRepository`: rebuild the member list via `ReferenceEquals` substitution instead of `Where(x => x.Key != userKey)` so duplicate rows are preserved on save.

## Acceptance criteria

1. Every `.Single(predicate)` / `.SingleOrDefault(predicate)` site listed above is replaced with a resilient lookup.
2. Duplicates trigger a `Warning`-level log entry naming the team key, the lookup key, and the call site.
3. `TeamRepository` writes preserve sibling rows with duplicate keys (no silent strip on save).
4. New unit tests cover each replaced site with a duplicate-row fixture: assertions are (a) the call does not throw, (b) the operation completes, (c) a warning is logged.
5. New unit tests for the `TeamRepository` strip-siblings fix: a member edit on a team containing a duplicate-keyed sibling preserves both rows in the persisted document.
6. `dotnet build -c Release` and `dotnet test -c Release` both green.
7. README.md updated to mention #64 / #59 follow-up in the release notes section.

## Done condition

PR opened from `feature/resilient-member-lookup` → `master`, all CI checks green, user has confirmed.

## Out of scope

- Self-healing (deleting duplicate rows during read). Filed as future work if symptoms persist.
- `Tharga.Team.MongoDB.UserRepository` race fix (issue [#65](https://github.com/Tharga/Platform/issues/65)) — separate feature.
- Framework package bumps (Microsoft.AspNetCore.* 9.0.15 → 10.0.7) — deferred, separate PR.
