# Plan: cache the team custom-roles read

## Steps

- [x] 1. NuGet package check (mandatory, up front).
      `dotnet outdated` across the solution reports exactly one update: `SixLabors.ImageSharp`
      3.1.12 → 4.0.0 in `Tharga.Team.Images`. **Deliberately not applied** — ImageSharp 4.0+ requires a
      paid Six Labors build-time license. This is a standing exception, not a deferral. Nothing else is
      outdated, so there is no upgrade step to verify.

- [x] 2. Add the custom-roles cache to `TeamServiceBase`.
      `_customRolesCache`, a static `ConcurrentDictionary<string, IReadOnlyList<TenantRoleDefinition>>`
      keyed by team key. An unusable (null/empty) key bypasses the cache in both directions rather than
      being stored under — `ConcurrentDictionary` rejects a null key outright.

- [x] 3. Invalidate on every write that can change the answer.
      One private `InvalidateCustomRolesCache` called from `SetTeamCustomRolesAsync` (the only writer),
      `DeleteTeamAsync<TMember>` and `CreateTeamAsync`. The last two bracket the key-recycling case:
      `GetRandomUnsusedTeamKey` can hand out a deleted team's key again.

- [x] 4. Add `GetTeamCallCount` to `TestTeamService`, plus a `SeedCustomRoles` helper that writes store
      state without going through the invalidating public writer. `GetTeamAsync` also had to tolerate a
      null key, which the previous `_teams.TryGetValue` did not.

- [x] 5. Tests — `Tharga.Team.Service.Tests/TeamCustomRolesCacheTests.cs`, 7 cases, keys unique per test.
      Repeat read hits the cache; an empty answer caches too (the common case, and the whole point);
      write invalidates; delete invalidates; null and empty keys bypass; and the real claims-path call
      through `TenantRoleService.GetEffectiveScopesAsync` reads the store once across three calls.
      **The `CreateTeamAsync` invalidation is not directly tested** — the public create path generates
      its own key and offers no way to ask for one, so the entry cannot be primed first. Recorded in the
      test class remarks rather than left as an unexplained gap; `DeletingTheTeam_DropsTheEntry` covers
      the path that actually frees a key.

- [x] 6. Build and full suite green — `dotnet build -c Release` (0 errors, 11 pre-existing warnings),
      `dotnet test -c Release`: **1751 passed, 0 failed**. New class verified in isolation (7/7) so it
      is not silently absent.

- [x] 7. Documentation updated — both surfaces, per the workflow.
      `docs/articles/implementation-guide.md` gained a "Cached per process" bullet in the dynamic tenant
      roles section (Step 7). `Tharga.Team/README.md` gained a "The team caches" section beside the
      existing "The user cache", covering both team caches, why writing around the service goes stale,
      and why the team document itself is deliberately **not** cached.

## Notes / decisions

- **Scope: custom roles only, not the full team document.** Confirmed with the user. Reasoning recorded
  in `feature.md` — the team document carries the roster that four authorization guards read fresh on
  purpose.
- **No TTL.** Confirmed with the user. Matches `_userCache` and `_teamMemberCache`, which have no
  expiry and rely on write-path invalidation.
- **Team selection changes need no invalidation.** Both caches are keyed by team, so switching the
  selected team consults different entries rather than stale ones. Verified while investigating.
- Noticed in passing, pre-existing and **not** fixed here: `TeamStateService.SetSelectedTeamAsync`
  calls `SetMemberLastSeenAsync` before its same-key early return, so re-selecting the already-selected
  team still writes and still drops that member's cache entry.

## Last session

All steps complete. The claims path now performs zero store reads per request in the
dynamic-tenant-roles configuration. Build and the full suite (1751 tests) are green, and both doc
surfaces are updated.

Next: user testing from the pushed branch. Not yet done, per the workflow — no PR is opened until the
user confirms the feature is done, and the close-out commit (archive `feature.md`, `git rm -r plan`)
must be the last commit before it.

Still outstanding and **not** part of this branch: GitHub issues #175 (tier 1, authorization),
#176 and #177 — the Eplicta defect sweep agreed earlier.
