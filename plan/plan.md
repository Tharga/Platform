# Plan: Access simulation — see the app as a less privileged user

Legend: `[ ]` todo · `[~]` in progress · `[x]` done

---

## 1. Package updates — mandatory first step

- [x] **`dotnet outdated` run 2026-08-03.** Only `SixLabors.ImageSharp` 3.1.12 → 4.0.0, held because 4.0+
      enforces a paid build-time licence. Nothing to apply.

## 2. Version

- [ ] **Minor on `3.10`** — new opt-in surface. Confirm before close-out.

---

## 3. Naming the target

One resolver per way of naming a target scope set. All four return the same shape, so §4 has one input.

- [ ] **A user** — `TeamGrantResolver.ResolveAsync(null, userKey, teamKey, …)`. The `principal` argument
      is unused on the member path, so this works with the resolver **unchanged**.
- [ ] **A role** — `ITenantRoleService.GetEffectiveScopesAsync` when runtime roles are on, else
      `IScopeRegistry.GetEffectiveScopes`. Same two-way choice `TeamGrantResolver` already makes; reuse
      rather than restate it.
- [ ] **Explicit scopes** — as given.
- [ ] **An access level** — `IScopeRegistry.GetScopesForAccessLevel`.
- [ ] Only **members of the current team** are offerable. A non-member reaching through consent needs
      their app roles, which the toolkit does not store.

## 3b. Who may simulate

- [ ] **A registered scope at `AccessLevel.Administrator`.** Owner and Administrator hold every registered
      scope, so this yields exactly "team owner/admin" by default while letting a host widen or withhold
      it without a toolkit change.
- [ ] **The exit is never gated.** A simulation can remove the gating scope; "return to normal" only ever
      restores what the caller really holds, so there is nothing to authorize.
- [ ] **The picker is gated on the real grant, re-resolved server-side.** Not on the filtered principal —
      a caller who simulated away the scope must still be able to change or inspect the simulation.
- [ ] **No shadow claims.** Do not park the removed scopes on the principal under another claim type to
      consult later; an inert claim listing scopes is what a future reader misreads as a grant.

## 4. The filter

- [ ] **`AccessSimulationFilter` — subtractive only.** Removes scope and role claims; **no code path adds
      a claim.** The de-escalation guarantee is then a property of the type, not of a calculation.
- [ ] Identity claims (name, subject, email, member key, team key) untouched — the audit actor stays real
      by construction rather than by a rule someone must remember.
- [ ] **Simulating a user drops all system scopes.** They cannot be computed for someone else
      (`ISystemRoleRegistry` maps app roles, which come from the identity provider). Dropping is the safe
      direction; §5 makes it visible.
- [ ] Access level: replace, clamped so the simulated level is never more privileged.
      **`Owner=0 … Viewer=3`, so less privilege is a larger ordinal and `Math.Min` is backwards.**
      `Custom=4` is the floor, not rank 4 — tested as its own case.

## 5. The difference report — build it with the filter, not after

- [ ] `target \ real`: what the target holds that the caller does not, and which the simulation therefore
      **cannot show**.
- [ ] **Under the Owner/Administrator restriction this narrows sharply, which changes its presentation
      rather than its necessity.** Owner/Administrator hold every *registered* team scope, so the only
      surviving team-scope gap is a member `ScopeOverride` (or runtime tenant role) naming an
      **unregistered** scope — `GetEffectiveScopes` unions overrides in without validating them.
      Because it is rare it can be **prominent**; rare is when a silent gap does the most damage, since
      nobody has built intuition for it.
- [ ] Plus an explicit "system scopes are not reproduced" when the target is a user.
- [ ] **Shown before applying, not only after.** The failure this prevents is an administrator concluding
      *"they cannot do X"* from a simulation that was never able to show X — an error that points toward
      granting more access than needed, which is the opposite of the feature's purpose.
- [ ] Pure and directly tested. It is a set difference; it should not need a rendered component to assert.

## 6. Reaching both claim-issuance paths

- [ ] `TeamServerClaimsTransformation`: read the cookie, stamp the marker claim, apply the filter last.
- [ ] `TeamClaimRevalidator`: read the marker claim from the principal, apply the filter last.
- [ ] **A test asserting *each* path applies it** — not a test of the filter, a test that it is reached.
      #175 was this exact shape. A revalidator that skipped it would restore full access on the next
      interval, silently, up to 30 minutes later.

## 7. Cookie and lifetime

- [ ] Named beside `Constants.SelectedTeamKeyCookie` so the two session-scoped cookies are visibly the
      same kind of thing.
- [ ] Session cookie — gone at sign-out, never written to stored roles.
- [ ] Read at the HTTP boundary only; carried on the principal thereafter.

## 8. UI

- [ ] Pick a target: user / role / scopes / access level.
- [ ] The §5 warning shown **in the picker**, before applying.
- [ ] A persistent indicator while active, with one-click return. Not decoration — a user who forgets they
      are simulating files bugs against their own session.
- [ ] Toggling navigates with `forceLoad: true`; a circuit cannot set a cookie.
- [ ] Cancel rightmost, per the shared dialog convention.
- [ ] Gate the picker: choosing another **user** as the target reads their access, so it needs a scope
      rather than being open to any member. Decide which — likely the same one that already lists members.

## 9. Audit

- [ ] An `IAuditEnricher` recording that simulation was active and what the target was.
- [ ] New `AuditMetadataKeys` constants — the vocabulary is part of the audit record's public contract.
- [ ] A test that the entry names the **real** user.

## 10. Registration

- [ ] Opt-in on `ThargaBlazorOptions`; off by default.
- [ ] Container validates with `ValidateOnBuild` + `ValidateScopes`, plus the self-check that a captive
      dependency really would fail it.

## 11. Tests

- [ ] **The effective set is a subset of the real set** — the central claim, for all four target kinds.
- [ ] **A forged cookie naming scopes the caller does not hold grants nothing.** This is the test that
      says why an untrusted carrier is acceptable.
- [ ] A stale simulation (roles changed since it was created) cannot elevate.
- [ ] Applying a role **replaces** rather than adds — the user's own framing, and the case that would look
      like a bug if it were additive.
- [ ] Both issuance paths apply the filter (§6).
- [ ] The difference report names exactly the scopes the target has and the caller lacks.
- [ ] **The `ScopeOverride` case**: a member holding an unregistered scope is reported as a gap even
      against an Owner. This is the one team-scope gap the restriction does not close, so it is the one
      worth a named test.
- [ ] A caller without the simulation scope cannot start one; **a caller who simulated the scope away can
      still return to normal**, and can still reach the picker.
- [ ] Access-level clamp, including `Custom`.
- [ ] Simulation absent after sign-out; stored roles never written.
- [ ] Audit names the real user.
- [ ] A host that does not opt in is unaffected.
- [ ] Mutation-check each guard.

## 12. Documentation

- [ ] README and `docs/` — both surfaces, per the workflow.
- [ ] State plainly: the cookie is untrusted and why that is safe; what a simulation **cannot** show; and
      that a simulating user's own REST calls are de-escalated too.
- [ ] Separate `docs:` commit.

## 13. Close-out (only when the user confirms the feature is done)

- [ ] Re-run `dotnet outdated`.
- [ ] Full suite green, no new warnings.
- [ ] Reply on #189.
- [ ] Archive `feature.md`, `git rm -r plan`, final commit, push, PR.

---

## Settled 2026-08-03 (user)

- **Not just access level** — role, explicit scopes and another user are all targets. Unified in §3: each
  names a scope set, and the operation is always "keep the intersection, remove the rest".
- **Applying a role replaces**, it does not add. Removal once intersected.
- **The difference warning is required**, and §5 explains why it is load-bearing rather than cosmetic.
- **Users only.** No MCP or REST case — an API key's scopes are directly editable, so the friction this
  removes for users does not exist there.
- **A simulating user's own REST calls are de-escalated anyway**, because simulation lives in the claims.
  Deliberate: excluding them would make the claim set differ by surface, which is what invariant I5 exists
  to prevent. Recorded so it is not "fixed" later.
- **#127 is not a dependency** — a forced reload re-issues claims through the HTTP path.

- **Restricted to team Owner / Administrator** (user, 2026-08-03), via a scope registered at
  Administrator level rather than a hard-coded access-level check — see §3b. This makes the team-scope
  gap structurally impossible rather than merely rare, with the single `ScopeOverrides` exception.

## Still open

- **Nested simulation** — simulate, then simulate again from inside. Subtractive composition is safe, but
  "return to normal" must go all the way back rather than one step. Recommended: **replace, never stack**,
  which also keeps the indicator honest about what is active.

## Last session

**2026-08-03 (planning, re-scoped).** Branch cut off `master` at 2430786. Package check unchanged.

**The re-scope simplified it.** Four asks turned out to be one mechanism with four ways of naming a target
set, and the `TeamGrantResolver` needed for the hardest of them (another user) works unchanged, because
its `principal` argument is unused on the member path.

**The sharp edge is not the removal, it is the reporting.** A simulation that silently shows less than the
target really sees would push an administrator toward over-granting — failing at exactly the job the
feature exists to do.

**The Owner/Administrator restriction is better founded than "they hold almost everything".** They hold
*every registered team scope*, by an explicit rule in `ScopeRegistry`. The gap is closed structurally,
except for unregistered `ScopeOverrides` — and it is not closed at all for system scopes, which is where
the reporting still has to work.

**Next:** confirm, settle nested simulation, then §3.
