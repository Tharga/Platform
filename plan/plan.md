# Plan: Access simulation — see the app as a less privileged user

Legend: `[ ]` todo · `[~]` in progress · `[x]` done

---

## 1. Package updates — mandatory first step

- [x] **`dotnet outdated` run 2026-08-03.** Only `SixLabors.ImageSharp` 3.1.12 → 4.0.0, held because 4.0+
      enforces a paid build-time licence. Nothing to apply.

## 2. Version

- [ ] **Minor on `3.10`** — new opt-in surface. Confirm before the close-out commit.

---

## 3. The simulation itself

- [ ] `AccessSimulation` record — the scopes and roles kept, and the simulated access level if that
      decision lands. Serializes compactly; it rides in a cookie.
- [ ] **`AccessSimulationFilter` — subtractive only.** Takes a `ClaimsIdentity` and an `AccessSimulation`,
      removes scope and role claims, adds nothing. **The type must have no code path that adds a claim**,
      so the de-escalation guarantee is a property of the code rather than of a correct calculation.
- [ ] Identity claims (name, subject, email, member key, team key) are never touched — the audit actor
      stays real by construction rather than by a rule someone has to remember.
- [ ] Cookie read and written in one place, named beside `Constants.SelectedTeamKeyCookie` so the two
      session-scoped cookies are visibly the same kind of thing.

## 4. Reaching both claim-issuance paths

- [ ] `TeamServerClaimsTransformation`: read the cookie, stamp the marker claim, apply the filter last.
- [ ] `TeamClaimRevalidator`: read the marker claim from the principal, apply the filter last.
- [ ] **A test asserting *each* path applies it.** Not a test of the filter — a test that it is reached.
      #175 was exactly this shape: the rule was right and one of five callers skipped it. Here, a
      revalidator that skipped it would silently restore full access on the next interval.

## 5. Access level — settle first, then build

- [ ] **Decide:** simulate access level, or scopes and roles only. `feature.md` recommends including it.
- [ ] If included: a clamp so the simulated level is never more privileged than the real one. Note the
      trap — `Owner=0 … Viewer=3`, so **less privilege is a larger ordinal** and `Math.Min` is backwards.
- [ ] `Custom` is the floor, not rank 4: it grants no base scopes and is not a comparable rank. Tested as
      its own case, not folded into the ordering.

## 6. UI

- [ ] A picker: choose which of your own scopes/roles to keep. Sourced from the caller's *real* claims, so
      it cannot offer something they do not hold.
- [ ] **A persistent indicator while simulation is active**, with one-click return. Not decoration — a
      user who forgets they are simulating files bugs against their own session.
- [ ] Toggling navigates with `forceLoad: true`; a circuit cannot set a cookie.
- [ ] Dialog button order per the shared convention: cancel is rightmost.

## 7. Audit

- [ ] An `IAuditEnricher` adding "produced under simulation" and what was dropped.
- [ ] New `AuditMetadataKeys` constants — the vocabulary is part of the audit record's public contract.
- [ ] A test that the entry names the **real** user, not the simulated shape.

## 8. Registration

- [ ] Opt-in on `ThargaBlazorOptions`. Off by default.
- [ ] Container validates with `ValidateOnBuild` + `ValidateScopes`, plus the self-check that a captive
      dependency really would fail it.

## 9. Tests

- [ ] The effective set is a subset of the real set — the central claim, asserted directly.
- [ ] **A forged cookie naming scopes the user does not hold grants nothing.** The cookie is
      client-controlled; this is the test that says why that is acceptable.
- [ ] A stale simulation (roles changed since it was created) cannot elevate.
- [ ] Both issuance paths apply the filter (§4).
- [ ] Simulation is absent after sign-out and never written to stored roles.
- [ ] Access-level clamp, including `Custom`, if §5 includes it.
- [ ] Audit names the real user.
- [ ] A host that does not opt in is unaffected.
- [ ] Mutation-check each guard.

## 10. Documentation

- [ ] README and `docs/` — both surfaces, per the workflow.
- [ ] State plainly that the cookie is untrusted and why that is safe.
- [ ] Separate `docs:` commit.

## 11. Close-out (only when the user confirms the feature is done)

- [ ] Re-run `dotnet outdated`.
- [ ] Full suite green, no new warnings.
- [ ] Reply on #189.
- [ ] Archive `feature.md`, `git rm -r plan`, final commit, push, PR.

---

## Open, and worth deciding before it costs anything

- **Access level** — §5. The one design question with a wrong answer that compiles.
- **Does simulation apply to REST and MCP calls by the same user?** It follows the claims, so today it
  would. That is consistent with *"the same access whether reached by REST or MCP"* and probably right —
  but it means a simulating user's API calls are also de-escalated, which may surprise. Confirm.
- **#127 is not a dependency** (see `feature.md`). Recorded because the issue suggests it might be.

## Last session

**2026-08-03 (planning).** Branch cut off `master` at 2430786. Package check unchanged.

**The seam is settled and it is not the one the issue assumed.** Simulation is a subtractive filter over
issued claims, applied at both claim-issuance paths, carried on the principal the way the selected team
already is. De-escalation is then a property of the mechanism rather than of a calculation being right.

**Next:** confirm the plan, settle §5, then §3.
