# Feature: Access simulation — see the app as a less privileged user

**Branch:** `feature/access-simulation` (off `master`)
**Started:** 2026-08-03
**Release:** **minor on `3.10`.** New opt-in surface; nothing changes for a host that does not enable it.

Issue [#189](https://github.com/Tharga/Team/issues/189), filed by Eplicta/FortDocs 2026-08-03.
**Tier 2** by `mission.md` — an Eplicta ask. Not tier 1: it grants no access, it only takes it away.

## The ask

Let a signed-in user temporarily drop some of the roles and scopes they genuinely hold, see the app as a
less privileged user would, and click once to come back. Today the alternatives are a throwaway user per
role combination, or editing your own roles and remembering to put them back — *"you can leave yourself
de-privileged, or worse, forget to remove something you granted."*

**De-escalation only. Never elevation.** That is the whole security requirement, and everything below is
shaped by it.

## The design decision that carries the safety

**Simulation is expressed as claim removal, not as a computed set.**

The issue asks for `simulated ∩ real`. An intersection is safe *if computed correctly*; a removal is safe
**because of what it is**. A bug in an intersection can widen. A bug in a removal pass can only remove too
much — the caller sees less than they should, notices, and turns simulation off.

So the mechanism is a subtractive filter over the finished `ClaimsIdentity`: it deletes scope and role
claims and adds nothing, ever. The intersection the issue asks for falls out of that — you cannot remove
your way to a claim you never had.

**This is also what makes an untrusted carrier acceptable.** The active simulation travels in a cookie,
like the existing `selected_team_id`. A cookie is client-controlled, so a user can forge one — and the
worst a forged one can do is reduce their own access further. That property has to be stated, tested, and
never quietly traded away.

## Where it applies — and why there are two places, not one

Claims are issued twice, by design:

- **`TeamServerClaimsTransformation`** (`IClaimsTransformation`) — the HTTP path. Adds system scopes from
  app roles, then team claims.
- **`TeamClaimRevalidator`** — the in-circuit path, re-running on `ClaimRevalidation.Interval`.

They already share `TeamMembershipClaimsBuilder` so the team half cannot drift. **The filter cannot live
in that builder**, because system scopes and app roles are added outside it — and those are exactly what a
simulation needs to drop.

So the filter is applied at the end of both, and **a test asserts both paths apply it**. That is the #175
lesson repeated deliberately: the rule was right there too, and four of five callers applied it. A filter
missing from the revalidator would restore full access on the next interval — silently, up to 30 minutes
after the user started simulating.

## What travels where

`TeamClaimRevalidator` runs inside a SignalR circuit where *"no cookie or HttpContext exists"*. The
selected team already solves this: the transformation reads the cookie once and stamps it onto the
identity as a marker claim, which the revalidator then reads. **The simulation follows exactly that
pattern** — cookie read once at the HTTP boundary, carried on the principal thereafter.

## Applying and clearing it needs a page reload

A Blazor Server circuit cannot set a cookie, so toggling simulation navigates with `forceLoad: true`. The
request re-runs the transformation, the new claims are issued, and the circuit restarts holding them.

**This is why #189 does not depend on #127.** The issue hoped the two might share a claims-refresh path.
They need not: a forced reload re-issues claims through the ordinary HTTP path, so stale-circuit claims
are irrelevant here. Worth saying out loud, because building this on #127 would block it behind a harder
problem for no gain.

## Scope

1. **`AccessSimulation`** — the active simulation: which scopes and roles are kept. Serialized into a
   cookie, carried as a marker claim.
2. **The subtractive filter**, applied at the end of both claim-issuance paths, with a test that each
   applies it.
3. **UI** — a picker to choose what to drop, a persistent indicator that simulation is active, and
   one-click return. The indicator is not decoration: a user who forgets they are simulating will file
   bugs against their own session.
4. **Audit** — an `IAuditEnricher` recording that an entry was produced under simulation and what was
   dropped. The actor stays the real user, which happens by construction: identity claims are never
   touched.
5. **Opt-in registration.** Off unless the host asks for it.

## Open — to settle while planning, not during

**Can access level be simulated, and what does "lower" mean?** `AccessLevel` is ordered
`Owner=0, Administrator=1, User=2, Viewer=3, Custom=4`, so **less privilege is a larger ordinal** and the
obvious `Math.Min` guard would be backwards. `Custom` sits at the end and is not a rank at all — it grants
no base scopes, which makes it the natural floor (#74, referenced by the issue) but not a comparable
value.

Dropping the `Team{AccessLevel}` role claim gets much of the effect without the ordering question, but
`[RequireAccessLevel]` reads `TeamClaimTypes.AccessLevel` directly, so it would not be honoured there.

**Recommendation: include it, as a replacement clamped so the simulated level is never more privileged
than the real one, with `Custom` treated as the floor rather than as rank 4.** The clamp is one tested
function. Leaving it out ships a simulation that cannot lower access level, which in this codebase is half
a feature.

## Not in scope

- **API keys.** A key is not a person and has no session; its scopes are already editable directly.
- **Simulating *another* user.** Impersonation is a different feature with a different blast radius, and
  nothing in the issue asks for it.
- **Persisting a simulation across sign-out.** The issue explicitly wants it gone.

## Acceptance criteria

1. A user can drop a subset of their own scopes and roles, and see the app as that reduced set.
2. **The effective set is always a subset of the real one** — asserted directly, and asserted again
   against a forged cookie naming scopes the user does not hold.
3. Both claim-issuance paths apply the filter; the revalidator does not restore dropped access.
4. A visible indicator while active, and one click returns to normal.
5. Simulation does not survive sign-out and never writes to the user's stored roles.
6. Audit entries made under simulation name the real user and record what was dropped.
7. Off by default; a host that does not opt in is unaffected.
8. Full suite green; no new warnings.

## Done condition

All eight met, docs on both surfaces, `plan/` removed in the close-out commit, PR open against `master`.
