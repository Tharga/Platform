# Plan: fail startup when a custom ITeamCache is not wired through

## Steps

- [x] 1. NuGet check (mandatory, up front). Only `SixLabors.ImageSharp` 3.1.12 → 4.0.0, held for its paid
      build-time licence. Nothing to apply.

- [x] 2. Approach confirmed with the user **before** any code. The decorator I originally recommended was
      re-examined and set aside for 3.x; reasoning recorded in `feature.md`.

- [x] 3. `internal ITeamCache CacheInUse` on `TeamServiceBase` and `UserServiceBase` — lets the check read the
      outcome without reflecting into private state, and adds no public API.

- [x] 4. `Tharga.Team/TeamCacheWiring.cs` — `FindUnwired(registered, params services)` plus
      `DescribeFailure(unwired)`. Returns empty when the registered cache is null or an `InMemoryTeamCache`,
      which is the false-positive guard the whole design rests on.

- [x] 5. `Tharga.Team.Blazor/Framework/TeamCacheWiringCheck.cs` — `IHostedService`, throws. Resolves the
      **concrete** service types, not `ITeamService`/`IUserService`: those resolve to the decorator chain, and a
      decorator holds no cache. Registered beside the two existing completeness checks.

- [x] 6. Tests — 12 new.
      `TeamCacheWiringTests` (9, Service.Tests): not-forwarded is reported; forwarded is not; forwarding a
      *different* cache is still reported; the built-in is never reported; no cache registered is ignored; a
      non-toolkit service is ignored; nulls are ignored; every offending service is named; the message names the
      type, the fix and the consequence.
      `TeamCacheWiringCheckTests` (3, Blazor.Tests): the check is registered; **a default host starts**; and a
      host with a custom cache it never forwarded fails startup — the last two through a real container, since
      the check throws and a false positive would break every consumer's boot.

- [x] 7. Build + full suite: **1831 pass**, 0 errors, warnings at the **11** baseline.

- [x] 8. Documentation corrected. Both surfaces previously said *"nothing fails without it"* — true when
      written for 3.10.7, false now. Updated in `Tharga.Team/README.md` and the implementation guide, including
      that the check compares actual instances so it cannot misfire.

- [ ] 9. Close-out: archive `feature.md` to the Plan directory `done/`, `git rm -r plan`, final commit, push,
      open the PR. **Only when the user confirms.**

## Notes / decisions

- **Outcome, not signature.** Comparing held-vs-registered instances has no false positives; reflecting over
  constructor parameters would misreport a host that obtains the cache another way.
- **Throws rather than logs**, and deliberately has no opt-out — see `feature.md`. It cannot fire for a host
  that has not registered a custom cache.
- **Version note:** the docs now cite **3.10.8** as the release that adds the check. That assumes this lands as
  the next patch; if the release lands as something else, those two references need updating.

## Last session

Steps 1–8 complete. Nothing pushed, no PR. Next: close-out on confirmation.

Carried forward: the 4.0 shape (required parameter or decorator, which needs the user-cache identity problem
solved first). Still open: plan 01 §3b and §6b, GitHub #155 and #142, and the three stale release runs for
#198/#199/#200 that should be **cancelled** rather than approved — approving one would publish older code as a
higher version.
