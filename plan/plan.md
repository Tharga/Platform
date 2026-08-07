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

## Steps — pluggable cache (added after the multi-instance finding)

- [x] 8. `ITeamCache` + `CachedValue<T>` in `Tharga.Team`. One port, all three lookups, domain-named
      removals. Async throughout — a remote adapter cannot be synchronous, and every call site was already
      in an async method.

- [x] 9. `InMemoryTeamCache` — the built-in. Instance fields rather than `static`, with a `Shared` instance
      used as the fallback for a service constructed without one, so behaviour is unchanged for hosts that
      have not forwarded it yet.

- [x] 10. Routed `UserServiceBase` (user lookup) and `TeamServiceBase` (membership + custom roles) through
      the port; deleted all three private dictionaries. Kept `InvalidateUserCache` and
      `InvalidateUserByKey` synchronous for compatibility, delegating to the async path, and added
      `InvalidateUserByKeyAsync` to `IUserCacheInvalidator` as a **default interface method** so existing
      implementations keep compiling. `CacheInvalidatingUserServiceDecorator` now awaits the async form.

- [x] 11. `TeamServiceRepositoryBase` and `UserServiceRepositoryBase` take and forward `ITeamCache`.

- [x] 12. Registered `TryAddSingleton<ITeamCache, InMemoryTeamCache>` in `AddThargaTeamBlazor` — the
      granular path, so the facade gets it too. Registering only in the facade is the exact shape of #157
      and #176, and there is now a test for it.

- [x] 13. Tests — 11 new, all green.
      `TeamCacheSubstitutionTests` (7): each lookup reads through the supplied cache, each write invalidates
      through it, a cache holding nothing is still correct, and two services sharing a cache share entries.
      `TeamCacheRegistrationTests` (4): built-in is a singleton, resolves, a host registration wins, and the
      granular path registers it.
      `TeamCustomRolesCacheTests` now injects a fresh cache per test instead of relying on unique keys.

- [x] 14. Build + full suite: **1762 passed, 0 failed** (was 1751 before this branch).

- [x] 15. Docs — `Tharga.Team/README.md` gained a "Caching and multi-instance deployments" section
      (what goes stale, how to replace, how to write an adapter, what is not cached); the implementation
      guide gained **Step 7a: Claims-path caching** with a warning callout, the forwarding example for both
      services, and an adapter requirements table.

## Last session

All steps complete. The claims path performs zero store reads per request on a warm cache, and the cache is
now a seam a multi-instance host can point at a shared store. Build and the full suite (1762 tests) green;
both doc surfaces updated.

**Nothing is pushed.** Next: user testing from the branch. No PR until the user confirms, and the close-out
commit (archive `feature.md`, `git rm -r plan`) must be the last commit before it.

**Carry forward:**
- The residual forwarding risk in `feature.md` → the caching-decorator refactor is the recommended
  follow-up. Worth a GitHub issue so it is not lost.
- The multi-instance staleness should have a GitHub issue too — it is a shipped tier-1 defect that
  consumers on 3.8.x are exposed to, and they need to know to upgrade and forward the cache.
- Pre-existing, unfixed: `TeamStateService.SetSelectedTeamAsync` calls `SetMemberLastSeenAsync` before its
  same-key early return, so re-selecting the current team still writes.
- Still untouched: GitHub issues #175 (tier 1, authorization), #176, #177 — the Eplicta defect sweep.
