# Plan: User lifecycle and host contracts

Legend: `[ ]` todo · `[~]` in progress · `[x]` done

---

## 1. Package updates — mandatory first step

- [x] **`dotnet outdated` run 2026-08-01, before the branch was cut.** One update available and
      deliberately NOT applied: `SixLabors.ImageSharp` 3.1.12 → 4.0.0 requires a **paid Six Labors
      build-time licence**. Everything else current. Unchanged since the previous feature.

---

## 2. Cache invalidation survives a host override — DONE 2026-08-01

- [x] `CacheInvalidatingUserServiceDecorator` over `IUserService`, invalidating after every mutating
      call. Registered inside `AuthorizationUserServiceDecorator` so it sits closest to the store —
      authorization decides first, then the write happens, then the entry is dropped.
- [x] 10 tests, including the exact PlutusWave case: a host that overrode persistence and never
      invalidates now reads back correctly.

**The plan said "decorator" but missed why that was not enough on its own.** `_userCache` is
`private static` and `InvalidateUserCache` is `protected`, so a decorator over `IUserService`
**cannot reach the cache at all**. It needed a way in:

- New public `IUserCacheInvalidator` with `InvalidateUserByKey`, implemented by `UserServiceBase`.
  A store written from scratch does not implement it and the decorator becomes a pass-through — tested.
- **Keyed by `IUser.Key`, not identity.** The cache is keyed by identity but every mutating member takes
  a key, so the implementation scans. One entry per signed-in user per process makes that cheaper than a
  second index which would itself need invalidating.

**Three judgement calls, all tested:**
- **`SetUserLastSeenAsync` is deliberately NOT invalidated.** It runs on every authenticated resolve
  (throttled), so invalidating there would empty the cache continuously and defeat the thing this class
  exists to keep correct. A cached `LastSeen` stays up to one resolve stale — the behaviour that already
  shipped.
- **A throwing write does not invalidate.** It changed nothing; dropping a valid entry would trade a
  stale read for a needless one.
- **The self-service icon members resolve the caller first** to learn which entry to drop. That read is a
  cache hit in the case that matters, so it costs a dictionary lookup rather than a round trip.

**Still 4.0 work:** the template method. Non-virtual `SetUserNameAsync` calling a `protected abstract`
hook makes this impossible to get wrong, and breaks every existing override — so it waits for the major.

---

## 3. Startup guard for un-overridden persistence extension points — DONE 2026-08-01

- [x] **Extension points enumerated from the code, not from the report.** `SetUserNameAsync`,
      `SeedUserNameAsync`, `SetUserIconReferenceAsync`, `SetUserDirectoryIdAsync`. `DeleteUserAsync`
      needs no guard — it already throws naming the type, which is the shape the others should have had.
- [x] `UserServiceCompleteness.Find(type, iconStoreRegistered, directoryRegistered)` — pure and
      testable, 8 tests.
- [x] `UserServiceCompletenessCheck : IHostedService`, registered inside `AddThargaTeamBlazor`. Runs at
      startup because reachability depends on registrations that may come after ours.
- [x] Reports **every** gap in one message, each with what is silently lost.

**Reflection over the concrete type, walking the base chain — not an interface map.**
`SetUserIconReferenceAsync` is `protected`, so it never appears in an interface map. A guard built on
one would have missed the very member that cost PlutusWave the most, while looking complete. Walking the
chain also means a host's own intermediate base counts as the override — tested.

**Reachability filtering.** An un-overridden member is only a defect if something can call it. No icon
store registered means the icon path is unreachable, and reporting it would be exactly the noise that
trains people to ignore startup output. Same rule as the Entra warning: report the mistake, not the
deliberate absence.

**Failure mode: logs an error, does not throw — with `o.Blazor.ThrowOnIncompleteUserService` to opt in.**
The plan asked whether to gate a throw on reachability. Reachability turned out to be the wrong axis:
the gap is **pre-existing** wherever it occurs, so a throw turns a routine upgrade into an outage over a
feature the host may never use, and gates it on a condition the host did not change. An error log is
loud, greppable and appears once; the strict reading is one option away for hosts that want it.

---

## 4. Ownerless team — recovery and prevention

Covers acceptance criteria 3 and 4.

- [ ] `SystemTeamScopes.AssignOwner = "teams:assign-owner"`, auto-registered like `Delete` and `Read`.
- [ ] Service operation: assign an owner, **only** when the team has no member at `AccessLevel.Owner`,
      **only** from that team's existing members. Both constraints matter — with no sitting owner there
      is nobody to escalate past, and restricting to members keeps this a repair rather than a way to
      inject an outsider.
- [ ] Enforce in `AuthorizationTeamServiceDecorator` with a **system** grant, resolved via
      `TeamScopeGate.HasSystemScope` — never a bare `HasClaim`, so an in-team scope of the same name
      cannot satisfy it.
- [ ] **Audit it.** This is privilege escalation by construction even when legitimate.
- [ ] `RemoveUserFromAllTeamsAsync` returns which teams were left ownerless, not just a count.
      **Check whether this is a breaking signature change** and pick an additive shape if so.
- [ ] UI on the Teams tab of `TeamsListView`, beside the existing `CanDeleteTeams` action, gated on the
      new scope through a pure `UserAdminGate` function with tests.

---

## 5. Directory display-name write-back

Covers acceptance criteria 5.

- [ ] `IUserDirectoryService.SetUserNameAsync(directoryId, name, ct)` as a **default interface member**
      that throws `NotSupportedException` — matching how `IsConfigured` was added without breaking
      custom implementations.
- [ ] Implement in `EntraUserDirectoryService` via Graph `PATCH /users/{id}`. No new Graph permission —
      `User.ReadWrite.All` already covers it and is already required for delete.
- [ ] **Opt-in, default off.** A host federating from a corporate directory wants the directory
      authoritative and would be alarmed to find the application overwriting it. Never an automatic side
      effect of the local rename.
- [ ] **Report a directory-write failure without rolling back the local write.** They fail
      independently; coupling them makes a Graph outage block renames.
- [ ] Tests: the option off means no Graph call at all; on means one PATCH; a failed PATCH leaves the
      local write intact and surfaces the error.

---

## 6. Documentation

- [ ] `docs/articles/user-management.md` — the ownerless-team recovery, the new scope in the "which
      scope gates what" matrix, and the directory write-back option.
- [ ] `Tharga.Team.Entra/README.md` — the write path and why it is opt-in.
- [ ] `Tharga.Team.Service/README.md` or the implementation guide — the persistence extension points and
      what the startup guard now enforces. **This is the doc that would have saved PlutusWave a day.**
- [ ] Separate `docs:` commit before close-out.

---

## 7. Close-out (only when the user confirms the feature is done)

- [ ] Re-run `dotnet outdated`.
- [ ] Full suite green.
- [ ] **Move `MAJOR_MINOR` to `3.9`** in `.github/workflows/build.yml` — this feature adds a system
      scope and public API. Nothing in CI does this, and the last release deliberately did not move it.
- [ ] Archive `plan/feature.md` to `$DOC_ROOT/.../done/user-lifecycle-and-host-contracts.md`
- [ ] `git rm -r plan`, final commit `feat: user-lifecycle-and-host-contracts complete`
- [ ] Push, open the PR against `master`.

---

## Last session

**2026-08-01 (setup).** Branch cut off `master` at `cdfa760` (3.8.3 merged). Package check done — no
change, ImageSharp still held. Plan written, **awaiting confirmation before any code changes.**

**Two things to settle before coding:** the failure mode for the startup guard (step 3 — throw always,
or throw only when the feature is reachable), and whether `RemoveUserFromAllTeamsAsync`'s richer return
can be made additive (step 4).

**Next:** confirm the plan, then step 2.
