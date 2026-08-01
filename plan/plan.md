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

## 4. Ownerless team — recovery and prevention  `[~]`

### 4a. The repair path — DONE 2026-08-01

- [x] `SystemTeamScopes.AssignOwner = "teams:assign-owner"`, auto-registered beside `Delete` and
      `users:manage`.
- [x] `TeamOwnership` — pure, 10 tests. `IsOwnerless(members)` and `CanAssign(members, candidate)`.
      **Both conditions are the whole safety argument**, so they live outside the service method where a
      unit test can reach them: the team must currently have no `Owner` (nobody to escalate past) and the
      candidate must already be a member (a repair cannot introduce an outsider).
- [x] `ITeamService.AssignOwnerAsync<TMember>` + `TeamServiceBase` implementation, refusing loudly with
      messages that name the alternative (`TransferOwnershipAsync` for a team that has an owner).
- [x] Enforced in `AuthorizationTeamServiceDecorator` on a **system** grant via `HasSystemScopeAsync`,
      with **no in-team fallback** — deliberately unlike `RequireDeleteAsync`, which has one. A member
      could not have produced this state, so there is no in-team case to accommodate.
- [x] Audited in `AuditingTeamServiceDecorator` as `assign-owner`, **including refusals** — an attempt to
      "repair" a team that is not broken is what taking one over would look like.

### 4b. Warn before the damage, not after — DONE 2026-08-01 (service side)

**The user redirected this, and the redirect is better than the plan.** The plan was to make
`RemoveUserFromAllTeamsAsync` report which teams it stranded — telling the operator after the fact, and
requiring a breaking return-type change to do it. Instead:

- [x] `ITeamService.GetTeamsForUserWithAccessLevelAsync(userKey, accessLevel)` — a plain read. Asking for
      `Owner` answers *"which teams will this delete strand?"* **before** the delete, so the operator can
      transfer ownership rather than be told afterwards that something is unrecoverable.
- [x] `RemoveUserFromAllTeamsAsync` is **untouched** — no breaking change, and nothing to obsolete.
- [x] `TeamServiceRepositoryBase` override filters the cross-team enumeration. Bounded by team count and
      runs once per delete confirmation, so a dedicated query would add a second place the membership
      shape is interpreted for no gain.
- [x] Enforced on `users:manage` in `AuthorizationTeamServiceDecorator`. **Not `teams:read`** — the
      caller already holds the right to remove this user from every one of these teams, so learning which
      they are is strictly less than they can do, and gating on a scope they may lack would hide the
      warning from exactly the person about to cause the damage.
- [x] Deliberately **not audited**: it runs when a dialog opens, so recording it would log an entry for
      looking at a confirmation that may then be cancelled. The delete it precedes is audited.

**Exact match, not minimum.** "Teams they own" is the question. A minimum-level parameter would read as
if `Owner` also meant every level above it — of which there are none, making the parameter look like it
does something it does not.

**The internal hook throws rather than returning empty.** An empty list is indistinguishable from "owns
nothing", and the caller uses that answer to decide whether deleting is safe — a silent default would
suppress the exact warning this exists to raise. Same reasoning as
`RemoveUserFromAllTeamsInternalAsync`.

**Still open:** whether deletion should *refuse* for a sole owner or proceed with the warning shown.
The read makes refusing possible; it does not decide it.

### 4c. UI — warning DONE, Teams-tab action REMAINING  `[~]`

- [x] **`IUserManagementService.GetOwnedTeamsAsync(userKey)`** — the dialog asks this, not `ITeamService`.
      The delete dialog already injects `IUserManagementService`, and injecting the internal team
      contract into a component is precisely what plan 07 exists to remove; adding one here would be
      creating work for it.
- [x] **The delete confirmation names the teams** the user owns and says ownership cannot be transferred
      afterwards — only a holder of `teams:assign-owner` can repair it.
- [x] **Best-effort and silent on failure.** A store that cannot answer must not block the delete behind
      an error: this improves a confirmation that already worked.
- [x] **Deletion proceeds — user's decision, 2026-08-01.** Refusing would block legitimate cases such as
      winding up a one-person team, and the state is now repairable. Recorded as a comment at the removal
      call so the next reader does not "fix" it into a refusal.
- [x] `UserAdminGate.CanAssignOwner(hasAssignOwnerScope, teamIsOwnerless)` — 4 tests. **Both conditions**:
      the service refuses on a team that already has an owner, so offering it there would be a control
      that throws when clicked.

- [ ] **Wire the action into `TeamsListView`** — a candidate picker over the team's existing members,
      calling `AssignOwnerAsync`. The gate and the service are done; this is the remaining piece.

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
