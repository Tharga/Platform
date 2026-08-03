# Feature: Access simulation — see the app as a less privileged user

**Branch:** `feature/access-simulation` (off `master`)
**Started:** 2026-08-03
**Release:** **minor on `3.10`.** New opt-in surface; nothing changes for a host that does not enable it.

Issue [#189](https://github.com/Tharga/Team/issues/189), filed by Eplicta/FortDocs 2026-08-03, and
re-scoped with the user 2026-08-03.

**Tier 2** by `mission.md` — an Eplicta ask. Not tier 1: it grants no access, it only takes it away.

## The purpose, which decides the design

**To make it easy for an administrator to set the correct access on a user** (user, 2026-08-03).

Not "to test the UI". That difference matters at every decision below: the question being answered is
*"if I give this person this role, what will they get?"*, and the feature is wrong if it answers that
question wrongly in the permissive direction.

## One mechanism, four ways to name the target

The ask covers simulating an access level, a role, explicit scopes, and **another user**. These are not
four features. Each names a **target scope set**, and the simulation is always the same operation:

> **Keep what the target has and I also have. Remove the rest.**

| Naming the target | Where the set comes from |
|---|---|
| A **user** | `TeamGrantResolver.ResolveAsync(null, userKey, teamKey, …)` — their real grant in this team |
| A **role** | `IScopeRegistry.GetEffectiveScopes(level, [role], [])` / `ITenantRoleService` for a runtime role |
| **Explicit scopes** | the list, as given |
| An **access level** | `IScopeRegistry.GetScopesForAccessLevel(level)` |

This is what the user meant by *"apply would in this case remove all scopes except the ones that are
customly added by a role or directly"* — applying a role is not additive, it **replaces** the effective
set with that role's scopes, which is removal once intersected with what the caller already holds.

**Everything stays subtractive, so de-escalation is a property of the code rather than of a calculation
being right.** A bug can only remove too much: the caller sees less than they should, notices, and turns
it off. The filter type has no code path that adds a claim.

That is also what makes an untrusted carrier acceptable — the active simulation rides in a cookie like
`selected_team_id`, so a user can forge one, and the worst a forged one does is reduce their own access
further.

## The warning is load-bearing, not a nicety

The user asked for *"a check to warn if there are scopes difference after the removal"*. Given the
purpose above, **this is the difference between a useful tool and a misleading one.**

If Alice simulates Bob and Bob holds scopes Alice does not, Alice sees `Bob ∩ Alice` — **less than Bob
actually sees**. Without a warning she concludes *"Bob cannot reach the billing page"* when he can. For a
feature whose entire job is getting access right, that error points toward **granting more than
necessary** — the exact opposite of what it exists to do.

So the simulation always reports `target \ real`: what the target has that the caller does not, and
therefore what the simulation could not show. Two sources of that gap:

1. **Scopes the caller does not hold.** Computable exactly, and shown.
2. **The target's system scopes.** `ISystemRoleRegistry.GetScopesForRoles` maps *app roles*, which come
   from the identity provider and are **not stored by the toolkit** — so another user's system scopes
   cannot be computed. The honest response is to drop all of the caller's own system scopes when
   simulating a user, and say that the system half is not being reproduced.

## What can be simulated faithfully, and what cannot

`TeamGrantResolver` needs only `userKey` and `teamKey` for a **member** — the `principal` argument is
used solely on the consent path. So:

- **A member of the current team**: faithful for team scopes, and free — the existing resolver, unchanged.
- **A non-member reaching the team through consent**: needs their app roles. Not supported; offer only
  members.
- **System scopes**: not computable for anyone else (above).

## Access level

Now a smaller question than it looked. Simulating a level is just another target set via
`GetScopesForAccessLevel`, so the ordinal comparison mostly disappears.

What remains is the `TeamClaimTypes.AccessLevel` **claim** itself, which `[RequireAccessLevel]` reads
directly and which is a single value rather than a set. Replacing it is not subtractive, so it needs its
own clamp — and the trap is real: `Owner=0, Administrator=1, User=2, Viewer=3`, so **less privilege is a
larger ordinal and the obvious `Math.Min` is backwards**. `Custom=4` is not a rank at all; it grants no
base scopes, which makes it the floor.

**Decision: include it, clamped, with `Custom` handled as the floor rather than as rank 4.** One tested
function. Without it a simulation cannot lower access level, which in this codebase is half a feature.

## Users only — and what that means for REST and MCP

**Agreed: there is no MCP or REST case worth building.** Those callers are API keys, and **an API key's
scopes are directly editable** — a host can create a key with exactly the scopes it wants and call the
endpoint. The friction this feature removes for users (throwaway accounts, editing your own roles and
remembering to put them back) does not exist for keys, so the same feature there would earn nothing.

**But the mechanism reaches a simulating user's own REST calls whether or not we want it to.** Simulation
lives in the claims, and a host may authenticate its controllers with the cookie scheme — the sample does
exactly that. Excluding REST would mean the claim set differing by surface, which is the "two answers to
the same question" problem invariant I5 exists to prevent, and it would cost machinery to build.

**So: do not special-case it.** A simulating user's own API calls are de-escalated too. It is consistent,
it is fail-safe, and the indicator explains why. This is a deliberate decision rather than an oversight,
recorded here so it is not "fixed" later.

## Where the filter applies — two places, deliberately

Claims are issued twice: **`TeamServerClaimsTransformation`** (HTTP) and **`TeamClaimRevalidator`**
(in-circuit, on `ClaimRevalidation.Interval`). They already share `TeamMembershipClaimsBuilder`, but
**the filter cannot live there** — system scopes and app roles are added outside it, and those are exactly
what a simulation drops.

So it is applied at the end of both, **and a test asserts each path applies it**. That is the #175 lesson
repeated on purpose: there too the rule was right and one of five callers skipped it. Here, a revalidator
that skipped it would silently restore full access on the next interval.

`TeamClaimRevalidator` runs where *"no cookie or HttpContext exists"*, so the simulation is read from the
cookie once at the HTTP boundary and carried on the principal as a marker claim — exactly how the
selected team already works.

## Applying and clearing needs a page reload

A circuit cannot set a cookie, so toggling navigates with `forceLoad: true`. **This is why #189 does not
depend on #127** — a forced reload re-issues claims through the ordinary HTTP path, so stale-circuit
claims never enter into it.

## Scope

1. `AccessSimulation` — the resolved target scope set, the simulated access level, and what it was named
   from (for the indicator).
2. The four target resolvers: user, role, explicit scopes, access level.
3. **`AccessSimulationFilter`** — subtractive only.
4. Applied at both claim-issuance paths, with a test that each applies it.
5. **The difference report** — `target \ real`, plus "system scopes not reproduced" when simulating a user.
6. UI: pick a target, see the warning *before* applying, a persistent indicator while active, one-click
   return.
7. Audit: the real user is the actor; an enricher records that simulation was active and what it was.
8. Opt-in registration, off by default.

## Not in scope

- **Impersonation.** This never acts *as* anyone: audit names the real user, and nothing the caller does
  not already hold is ever granted. Stated so it is not "improved" into impersonation later.
- **Simulating a non-member**, or anyone's system scopes (not computable — above).
- **API keys.**

## Acceptance criteria

1. A target can be named as a user, a role, explicit scopes, or an access level, and applying it leaves
   only the scopes the target and the caller both hold.
2. **The effective set is always a subset of the real one** — asserted directly, and again against a
   forged cookie naming scopes the caller does not hold.
3. **The caller is told what the simulation could not show** — scopes the target holds and they do not,
   and that a simulated user's system scopes are not reproduced.
4. Both claim-issuance paths apply the filter; the revalidator does not restore dropped access.
5. Access level is lowered, never raised, with `Custom` as the floor.
6. A visible indicator while active; one click returns.
7. Simulation does not survive sign-out and never writes to stored roles.
8. Audit entries made while simulating name the real user and record the simulation.
9. Off by default; a host that does not opt in is unaffected.
10. Full suite green; no new warnings.

## Done condition

All ten met, docs on both surfaces, `plan/` removed in the close-out commit, PR open against `master`.
