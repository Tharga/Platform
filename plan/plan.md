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

### Step 3 UI — DONE 2026-08-02

- [x] A **Disabled badge** on the name column of both `ApiKeyView` and `SystemApiKeyView`, plus a
      Disable/Enable row action that flips with the row's state.
- [x] `SetKeyDisabledAsync` added to the *gated* `IApiKeyManagementService` and
      `ISystemApiKeyManagementService` as well — the views inject those, not the administration service,
      so the feature was unreachable from the UI without them. The team-facing one routes through
      `EnsureCanMutateAsync`, so a private key still cannot be disabled by a non-owner.

**Distinguishable from expired by device, not by colour.** Expiry is red text and a warning icon on the
Last-used column; disabled is a filled badge beside the name. Two red things in one grid is how an
operator concludes a contained key merely lapsed.

Only disabling is confirmed. Enabling is the reversible direction, and a confirmation on it teaches
operators to click through the dialog that matters.

## 4. Disable and enable a user

**Decisions settled 2026-08-02 (user):**
- **Scope: reuse `users:manage`.** It already authorizes the strictly more destructive delete, so a
  separate grant would guard the lesser act more carefully than the greater one.
- **A disabled user stays visible** in the admin lists. An invisible disabled user is hard to re-enable.
- **No cascading to their API keys.** A key is not a session: it is an independent credential with its
  own lifecycle, and disabling a person should not silently retire integrations they happen to have
  minted. Where a compromise means both must stop, that is two deliberate acts — which is also what makes
  each one reversible on its own.

- [x] State on the user entity: `DisabledAt` + `DisabledBy` on `IUser` as **default interface members
      returning null** — the shape-based opt-in already used by `Icon`, `DirectoryId` and `LastSeen`. A
      host declares the properties on its entity to persist them; declaring nothing keeps compiling.
- [x] **Eviction, and not through `TeamClaimRevalidator` after all.** That class refreshes *team* claims
      and returns early when no team is selected, so it can neither express "sign this person out" nor
      even run for a user with no team. The mechanism is one level up:
      `TeamRevalidatingAuthenticationStateProvider.ValidateAuthenticationStateAsync` returning **false**,
      which is Blazor's own eviction. Checked *before* the claim refresh — there is no point bringing team
      access up to date for someone being signed out.
- [x] `SetUserDisabledAsync` on `IUserManagementService`, gated on `users:manage`, audited under
      **distinct `disable`/`enable` actions** for the same reason the key is.
- [x] **`DirectoryUserStatus.Disabled` untouched.** The two render as separate badges on the same row —
      independent states that can disagree.
- [x] **A user cannot disable themselves** (user, 2026-08-02), enforced in `UserManagementService`,
      repeated in `UsersListView` to turn the throw into an explanation, and gated in `UserAdminGate`.
- [x] UI on `UsersListView`: Disabled badge, Disable/Enable action, self-row disabled with the reason in
      the label (`RadzenSplitButtonItem` carries no tooltip).
- [x] 23 tests across three files.

### Three things this step got wrong first

**`TeamClaimRevalidator` was the wrong mechanism**, named in `feature.md` and in this plan. Reading it
settled it: it is documented *fail-open* precisely so a transient error never signs anybody out, and it
returns early when no team is selected. Both properties are correct for team claims and wrong for
eviction. The provider above it returns a bool that Blazor acts on, which is the actual seam.

**The authorization decorator would have swallowed the call.** `SetUserDisabledAsync` is a *default*
interface member, so `AuthorizationUserServiceDecorator` — which does not override what it does not know
about — compiled fine and would have dispatched into the throwing default instead of the host's store.
Found by enumerating every `IUserService` implementer rather than following callers. The regression test
calls through `IUserService`, not the concrete type: through the concrete type an omitted override is a
compile error in the test itself, which proves nothing about what a host hits at runtime. **Verified by
removing the override — two tests go red with the `NotSupportedException` a host would see.**

**Nine new CS1573 warnings** from step 3's `<param>` block on `BuildKey`: documenting two parameters of an
eleven-parameter private helper makes the compiler demand the other nine. Converted to a plain comment;
warning baseline restored to 8 distinct.

## 5. Suspend a team member

**Decisions settled 2026-08-02 (user):** a team Owner/Administrator can suspend a member. The member
**still sees the team in the selector**, marked `Suspended`, and selecting it lands on an explanation
rather than the team UI. Folded into this branch as the third sibling of key-disable and user-disable, so
all three share one vocabulary.

### `MembershipState.Suspended` is the wrong mechanism — and would have done the opposite

The obvious move is a fourth `MembershipState`. It is actively wrong here. Host stores list a user's
teams by filtering `State == MembershipState.Member` (`TeamRepository.GetTeamsByUserAsync`, and the
sample does the same), so a suspended member's team would **disappear from the selector** — precisely the
option the user rejected. Worse, the filter lives in *host* code, so the toolkit cannot fix it centrally.

So suspension is `SuspendedAt` + `SuspendedBy` on `ITeamMember`, as **default interface members** — the
same shape-based opt-in as `IUser.DisabledAt` and `IApiKey.DisabledAt`. The member's `State` stays
`Member`, so every existing store query keeps returning the team and the selector keeps showing it with
no host change at all.

### The enforcement point is the claims builder, not the listing

`TeamMembershipClaimsBuilder.BuildAsync` grants `Team{AccessLevel}` roles and the full effective scope
set the moment `GetTeamMemberAsync` returns anything — **it does not look at `State` today**. So the
listing is not, and never was, the enforcement point. A suspended member must be refused there.

**And must not fall through to consent.** The method's non-member path grants access when the team has
consented to one of the caller's global roles. Falling through would hand a suspended member access by
another route; suspension is the more specific and more recent decision, so it wins. Test, not comment.

### What the toolkit can and cannot guarantee

The host owns routing — there is no toolkit-owned shell to take over, and `<TeamSelector />` is placed by
the host in its own layout. So:

- **Guaranteed everywhere:** no team scopes, so every `[RequireScope]` refuses. Security does not depend
  on the host doing anything.
- **Guaranteed on toolkit surfaces:** `TeamComponent` renders the notice instead of the team UI.
- **Opt-in for the host:** a drop-in notice component for its own layout.

Stating this rather than implying a full-page takeover the toolkit cannot deliver.

- [x] `SuspendedAt`/`SuspendedBy` on `ITeamMember`; persistence hook on the store with the throwing
      default; member-cache invalidation on both directions.
- [x] `TeamMembershipClaimsBuilder` refuses a suspended member, and does not fall through to consent.
- [x] `SetMemberSuspendedAsync` gated on `member:manage` — it already authorizes *removing* a member,
      which is strictly more destructive.
- [x] **An Owner cannot be suspended** (mirroring "the owner cannot leave the team"), and **a member
      cannot suspend themselves**. Both in the service, not only the UI.
- [x] Selector: `Suspended` badge. Needs the caller's membership per listed team — `ITeam` carries no
      members — via the `GetTeamMemberAsync` cache.
- [x] `TeamComponent`: Suspend/Restore row action, badge on the member grid, notice when the caller's own
      membership is suspended.
- [x] 16 tests (5 claims-builder, 10 service, plus the architecture guard picking up the new component).

---

## 6. Documentation

- [x] Correct the `LockKeyAsync` remarks — *"there is no disable yet"* stops being true.
- [x] `docs/articles/user-management.md`: disabling a user, and the local-vs-directory distinction.
- [x] The scope matrix gains `teams:manage`, with the boundary stated: rename and icon, **not** consent.
- [x] Suspending a member: what it does, what it does not touch, and why it is not `MembershipState`.
- [x] Separate `docs:` commit before close-out.

---

## 7. Close-out (only when the user confirms the feature is done)

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

**2026-08-02 (steps 3 and 4).** Key-disable UI and the whole of user disable/enable. **1331 tests green**,
build clean at the 8-warning baseline.

Fail-open is deliberate in the eviction check and tested as such: treating a store failure as "disabled"
would sign out every signed-in user at once, turning a database blip into an outage. A genuinely disabled
user is still evicted, one interval later.

**2026-08-02 (step 5).** Suspend a team member, end to end. **1347 tests green**, 8-warning baseline.

Two mechanisms that looked obvious were rejected after reading the code, both because they would have
produced the opposite of the chosen design: a fourth `MembershipState` (host stores filter on it, so the
team would vanish from the selector) and gating visibility in `ITeamDirectoryService` (its `team:read`
filter recomputes scopes from access level and roles, which suspension deliberately does not touch — so
the team stays listed with no special case at all).

A drop-in `SuspendedTeamNotice` is provided for the host layout. The toolkit cannot impose a full-page
takeover because the host owns routing; what it does guarantee is that no scopes are granted, so every
`[RequireScope]` refuses whether or not the host places the notice.

**2026-08-02 (invited-member fix + step 6).** Field bug fixed and documented. **1352 green**, 8 warnings.

**A correction made while documenting the trap.** The first version of the XML doc said
`GetTeamMemberAsync` filters invited members out. It does not — *the host store does*. The test double
returns them, the MongoDB store does not, and the pinning test failed for exactly that reason, which is
how it was caught. The guidance is now the honest one: the answer is host-controlled, so nothing may
depend on it either way, and code that must tell the states apart reads `GetMembersAsync`.

**Next:** step 7, close-out.
