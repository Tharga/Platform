# Plan: Suspend instead of destroy

Legend: `[ ]` todo · `[~]` in progress · `[x]` done

---

## 1. Package updates — mandatory first step

- [x] **`dotnet outdated` run 2026-08-02, before the branch was cut.** Unchanged: `SixLabors.ImageSharp`
      3.1.12 → 4.0.0 held for the paid-licence reason. Everything else current.

---

## 2. `teams:manage` — cross-team rename and icon — DONE 2026-08-02

- [x] `SystemTeamScopes.Manage`, auto-registered beside `Delete`, `Read` and `AssignOwner`.
- [x] `RequirePresentationManageAsync` in the decorator — **system grant first, then the in-team check**,
      mirroring `RequireDeleteAsync`. Rename, set-icon and clear-icon all route through it.
- [x] `UserAdminGate.CanManageTeams` + Rename and Set icon on the Teams tab.
- [x] 12 tests.

**The boundary is what the tests are for, not the happy path.** In-team `team:manage` covers rename,
icon, consent *and* custom roles. The system grant covers only the first two — consent is a team's
statement about what it exposes inbound and custom roles decide what a member may do, both authorization;
rename and icon change how a team looks. Two tests assert the oversight caller is **refused** consent and
custom roles, because nothing in the type system can express "these two members of that scope but not
those two", and the erosion would be a one-line change that reads like consistency.

Also pinned: a *team* grant literally named `teams:manage` does not authorize another team. The claim
types carry provenance precisely so an in-team scope cannot be spent cross-team.

**Two plan assumptions were wrong, both stale rather than mistaken:**
- **`TeamIconDialog` does not assume the caller's own team.** It takes `TeamKey` as a parameter and uses
  `ITeamManagementService`. The backlog note predates the gated-service migration.
- **`TeamDialog` is the same** — already parameterised by team key. Both dialogs were reused as-is, so
  the Teams tab gained two actions without a new dialog between them.

## 3. Disable and enable an API key — service side DONE 2026-08-02

- [x] `DisabledAt` + `DisabledBy` on `IApiKey` and `ApiKeyEntity`, not a bool — *when* and *who* are what
      an operator needs after a security action.
- [x] `ApiKeyAuthenticationHandler` refuses a disabled key and **records the refusal as an auth failure**.
      A disabled key still gets used — by a scheduled job nobody remembered, or by whoever the disabling
      was aimed at — and those attempts are the point of the audit trail.
- [x] `SetKeyDisabledAsync` / `SetSystemKeyDisabledAsync` on `IApiKeyAdministrationService`, reusing
      `apikey:manage` (settled 2026-07-31).
- [x] Audited under **distinct actions**: `disable` is a containment, `enable` is a decision to trust the
      key again. One entry keyed on a boolean would make "who re-enabled this" a query rather than a
      reading.
- [x] `ApiKeyLifecycleDecorator` passes both through **without a lifecycle signal** — disabling neither
      mints nor destroys a secret, so a handler capturing tokens has nothing to capture.
- [x] 6 tests.

### The trap was real, and it was already set

`RefreshKeyAsync` rebuilds the entity through `BuildKey(...)` from a fixed list of fields. **`DisabledAt`
was not on that list**, so the first working version silently re-enabled any disabled key the moment
someone refreshed it — which is precisely the response to a suspected leak. The same defect existed in
`BuildSystemKey`.

Both now carry it forward, and `RefreshingADisabledKey_LeavesItDisabled` is the test that says so.
**Verified by removing the carry-forward and watching it fail** — the other five stayed green, so nothing
else would have caught it.

**Worth remembering beyond this feature:** a rebuild-from-fields constructor silently drops any state
added later. Every future field on `ApiKeyEntity` has this hazard, and the compiler will never mention
it.

### Remaining for step 3

- [ ] UI on `ApiKeyView` and `SystemApiKeyView`: disabled rows visibly distinct, and distinguishable from
      expired.

## 4. Disable and enable a user

**Decisions settled 2026-08-02 (user):**
- **Scope: reuse `users:manage`.** It already authorizes the strictly more destructive delete, so a
  separate grant would guard the lesser act more carefully than the greater one.
- **A disabled user stays visible** in the admin lists. An invisible disabled user is hard to re-enable.
- **No cascading to their API keys.** A key is not a session: it is an independent credential with its
  own lifecycle, and disabling a person should not silently retire integrations they happen to have
  minted. Where a compromise means both must stop, that is two deliberate acts — which is also what makes
  each one reversible on its own.

- [ ] State on the user entity: `DisabledAt` + `DisabledBy`, same shape as the key.
- [ ] **Eviction, not just refusal.** A signed-in user holds a circuit with claims already issued;
      `TeamClaimRevalidator` is the mechanism that already handles membership removal and access
      downgrade. A disabled user must be evicted within `ClaimRevalidation.Interval`.
- [ ] Enable/disable on `IUserManagementService`, audited both ways.
- [ ] **Do not reuse `DirectoryUserStatus.Disabled`.** That means disabled *in Entra*. Application-level
      disable is a different thing with a different blast radius, and the UI must show them separately or
      an operator will read one as the other.
- [ ] UI on `UsersListView`, distinct from the directory badge.

---

## 5. Documentation

- [ ] Correct the `LockKeyAsync` remarks — *"there is no disable yet"* stops being true.
- [ ] `docs/articles/user-management.md`: disabling a user, and the local-vs-directory distinction.
- [ ] The scope matrix gains `teams:manage`, with the boundary stated: rename and icon, **not** consent.
- [ ] Separate `docs:` commit before close-out.

---

## 6. Close-out (only when the user confirms the feature is done)

- [ ] Re-run `dotnet outdated`.
- [ ] Full suite green, no new warnings.
- [ ] **`MAJOR_MINOR` → `3.11`.**
- [ ] Archive to `$DOC_ROOT/.../done/suspend-instead-of-destroy.md`
- [ ] `git rm -r plan`, final commit, push, PR.

---

## Last session

**2026-08-02 (setup).** Branch cut off `master` after 3.10.0 merged. Package check unchanged. Plan
written, **awaiting confirmation and the three #11 decisions before any code changes.**

**Found during the survey:** item #6, the `LockKeyAsync` doc, is **already fixed** — it now says the lock
does not disable and that there is no disable yet. That last clause becomes untrue with this feature, so
it changes here rather than being a separate task.

**Also confirmed:** rename and both icon operations call `RequireTeamScopeAsync` with **no system
fallback**, so the server refuses an oversight role whatever the UI offers. Adding buttons alone would
reproduce the defect PR #126 fixed — controls that throw when clicked.

**Next:** confirm the plan and settle the #11 decisions, then step 2.
