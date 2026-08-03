# Plan: Access simulation — see the app as a less privileged user

Legend: `[ ]` todo · `[~]` in progress · `[x]` done

---

## 1. Package updates — mandatory first step

- [x] **`dotnet outdated` run 2026-08-03.** Only `SixLabors.ImageSharp` 3.1.12 → 4.0.0, held because 4.0+
      enforces a paid build-time licence. Nothing to apply.

## 2. Version

- [ ] **Minor on `3.10`** — new opt-in surface. Confirm before close-out.

---

## 3. Naming the target — DONE

One resolver per way of naming a target scope set. All four return the same shape, so §4 has one input.

- [x] **A user** — `TeamGrantResolver.ResolveAsync(null, userKey, teamKey, …)`. The `principal` argument
      is unused on the member path, so this works with the resolver **unchanged**.
- [x] **A role** — `ITenantRoleService.GetEffectiveScopesAsync` when runtime roles are on, else
      `IScopeRegistry.GetEffectiveScopes`. Same two-way choice `TeamGrantResolver` already makes; reuse
      rather than restate it.
- [x] **Explicit scopes** — as given.
- [x] **An access level** — `IScopeRegistry.GetScopesForAccessLevel`.
- [x] Only **members of the current team** are offerable, and never yourself. A non-member reaching through consent needs
      their app roles, which the toolkit does not store.

## 3b. Who may simulate — DONE

- [x] **`simulation:use`, registered at `AccessLevel.Administrator`.** Owner and Administrator hold every registered
      scope, so this yields exactly "team owner/admin" by default while letting a host widen or withhold
      it without a toolkit change.
- [x] **The exit is never gated.** `StopAsync` has no check, deliberately and with the reason recorded. A simulation can remove the gating scope; "return to normal" only ever
      restores what the caller really holds, so there is nothing to authorize.
- [x] **The picker is gated on the real grant, re-resolved by `AccessSimulationState`.**
- [x] **No shadow claims.** The removed scopes are nowhere on the principal.

## 4. The filter — DONE

- [x] **`AccessSimulationFilter`. Scopes are strictly subtractive — no code path adds a scope claim**,
      team or system.
- [x] **Correction to the plan as written:** it said *no code path adds a claim at all*. That is not
      achievable. `[RequireAccessLevel]` reads the `AccessLevel` claim and `AuthorizeView Roles="Team…"`
      reads the matching role, so a simulation that only *removed* them would show a caller with **no**
      level rather than a lower one. The honest statement is narrower and still strong: **scopes are
      absolutely subtractive; the access level is a clamped replacement**, and both claim and role move
      together so nothing can read one and disagree with the other.
- [x] Identity claims (name, subject, email, member key, team key) untouched — the audit actor stays real
      by construction.
- [x] **Every identity on the principal is filtered, not only the primary one.** Authorization reads the
      union across identities, so a scope left on a secondary identity would still be honoured — the
      simulation would appear to work while granting what it claimed to remove. Tested.
- [x] `TryRemoveClaim` returning false **throws** rather than continuing. A claim that survives a removal
      it reported is the one failure this type exists to prevent.
- [x] **Simulating a user drops system scopes and app roles.** Neither can be computed for someone else;
      dropping is the safe direction and §5 makes it visible.
- [x] `AccessLevelPrivilege` — its own type, because the obvious implementation is wrong in a way that
      compiles: `Owner=0 … Viewer=3`, so `Math.Min` picks the *more* privileged. `Custom` is the floor,
      handled explicitly rather than by its ordinal happening to work.

## 5. The difference report — DONE (report), UI pending

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

## 6. Reaching both claim-issuance paths — DONE

- [x] `TeamServerClaimsTransformation`: reads the cookie, stamps the marker claim, applies the filter last.
- [x] `TeamClaimRevalidator`: reads the marker claim from the principal, applies the filter last.
- [x] **A test asserting *each* path applies it**, each with a self-check that the same setup issues the
      full set when no simulation is active — otherwise "still one scope" could mean the builder was
      never reached. Both call sites mutation-checked.
- [x] **Found while wiring: the revalidator compared *unfiltered* fresh claims against the caller's
      *filtered* current ones**, so with a simulation active every interval looked like a claims change
      and the circuit would re-render on a timer forever. The comparison now happens after filtering,
      against the same objects the filter mutated. Tested by `AnUnchangedSimulation_IsNotReportedAsAChange`.

## 7. Cookie and lifetime — DONE (write path pending with the UI)

- [x] `access_simulation`, named beside `selected_team_id`.
- [x] Read at the HTTP boundary only; the raw value is carried on the principal as a marker claim, since
      the revalidator has no `HttpContext`. Kept raw rather than parsed so it is obviously untrusted
      wherever it is read.
- [x] Parsing never throws — malformed, truncated or hand-edited means "no simulation", which returns the
      caller to real access. The safe direction, and the same outcome as clearing the cookie.
- [x] Session cookie set/cleared by the UI, then a forced reload.

## 8. UI — DONE (host wiring pending)

- [x] `AccessSimulationDialog` — member / role / access level, with the §5 warning **in the picker**
      before applying, and a positive "this will be an exact view" when there is no gap.
- [x] `AccessSimulationBar` — persistent indicator with one-click return.
- [x] `forceLoad: true` after writing the cookie.
- [x] Cancel rightmost, via the shared `CancelButton`. `DialogButtonOrderTests` scans the new dialog and
      passes.
- [x] Gated on `simulation:use`.
- [ ] The host has to place `<AccessSimulationBar />` and open the dialog. Document in §12.

## 9. Audit — DONE

- [x] `AccessSimulationAuditEnricher`, registered only when the feature is on.
- [x] `AccessSimulationMetadataKeys` — `simulation.active`, `simulation.kind`, `simulation.target`.
- [x] A test that the entry names the **real** user; the actor is untouched by construction, since
      simulation never removes identity claims.
- [x] A malformed cookie adds nothing and does not throw — the enricher records, it does not gate.

## 10. Registration — DONE

- [x] `o.Blazor.Simulation.Enabled`, off by default.
- [x] `AccessSimulationState` scoped, alongside the other Blazor services.
- [x] The scope is granted to Owner and Administrator and to nobody else — tested, with a self-check that
      the registry really does grant by level (otherwise "not granted to Viewer" could mean it grants
      nothing to anyone).

## 11. Tests

**66 simulation tests so far. Whole suite 1653 green, warnings unchanged at 11.**

- [x] **The effective set is a subset of the real set** — asserted directly and as a property over
      several shapes.
- [x] **A forged simulation naming scopes the caller does not hold grants nothing.**
- [x] A stale simulation cannot elevate.
- [x] Applying a role **replaces** rather than adds.
- [x] Both issuance paths apply the filter (§6).
- [x] The difference report names exactly the scopes the target has and the caller lacks, including the
      unregistered-`ScopeOverride` case.
- [ ] **The `ScopeOverride` case**: a member holding an unregistered scope is reported as a gap even
      against an Owner. This is the one team-scope gap the restriction does not close, so it is the one
      worth a named test.
- [ ] A caller without the simulation scope cannot start one; **a caller who simulated the scope away can
      still return to normal**, and can still reach the picker.
- [x] Access-level clamp, including `Custom`, exhaustive over all 25 pairs.
- [ ] Simulation absent after sign-out; stored roles never written.
- [x] Audit names the real user.
- [x] A host that does not opt in is unaffected — the enricher is not even registered.
- [~] **Mutation-checked: 15/16 caught.** Script at `scratchpad/mutate189.py`.
      **One survivor, reported rather than papered over:** replacing `Rank`'s `Custom` special case with a
      plain `(int)level` cast stays green. It has to — `Custom` is ordinal 4 and every ranked level is
      0–3, so the two agree for all five values and no test can separate them without changing the enum.
      The line stays because they agree by *accident*: `Custom` is the floor for granting no base scopes,
      not for sorting last. `EveryAccessLevelIsAccountedFor` is the trip-wire if the enum changes shape.
      Contorting the code or the test to reach 16/16 would buy nothing real.
- [x] **A mutation run also improved the design.** The `Custom` handling was originally two guard clauses
      in the comparison — entirely dead code, deleted without a single test noticing.

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
