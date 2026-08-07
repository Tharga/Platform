# Feature: a pluggable cache for the claims path

## Goal

Let a consumer choose where the claims-path cache lives, so a multi-instance deployment stops serving stale
authorization — and remove the per-request team read that started this.

## Background

`TeamServerClaimsTransformation` is an `IClaimsTransformation`, so it runs once per authenticating HTTP
request and performs three lookups: the caller, their membership in the selected team, and that team's
custom roles.

Two were cached in process-wide `static ConcurrentDictionary` fields; the third was uncached, so with
`AddThargaDynamicTenantRoles()` registered every request read the whole team document. That was the
original defect.

Investigating it surfaced a larger one. **At least one consumer runs multi-instance**, and the caches have
no expiry and no cross-instance invalidation, so a change made through one instance never reaches the
others:

| Changed on instance A | Instance B, until it restarts |
|---|---|
| Access level, tenant roles, scope overrides | issues the old claims |
| **Member suspended** | grants their full team scopes |
| **User disabled** | keeps their session alive |
| Team custom roles | serves the old role-to-scope mapping |

Claim revalidation does not correct it: `TeamClaimRevalidator` recomputes through the same
`TeamMembershipClaimsBuilder`, so it reads the same stale entry and reports no change. Suspension and
user-disable are authorization, which makes this tier 1 under `mission.md`.

## Scope

- **`ITeamCache`** in `Tharga.Team` — one port covering all three lookups, named in domain terms. A port,
  not a package: `architecture-v4.md` explicitly rejects a `Team.Ports` package.
- **`InMemoryTeamCache`** — the built-in, reproducing today's behaviour, registered `TryAdd` singleton so
  nothing existing changes and a host substitution wins.
- Both service bases and both MongoDB bases route through the port and forward it.

Deliberately **not** in scope, and both recorded in the docs:

- **Caching the team document.** It carries the member roster, and four paths read it precisely because
  they need current state to decide access (`TeamServiceBase.cs:248-251` says so). A cache there fronts an
  authorization check.
- **The consent-teams query.** Keyed by role set rather than by team, so it does not fit the port's shape.
- **A TTL.** Would make staleness quieter without making it shorter in the case that matters, since the
  entries that go stale are the ones the local instance never writes to.

## Acceptance criteria

- [x] All three lookups read and invalidate through the injected `ITeamCache`.
- [x] The built-in is registered as a singleton on both the facade and the granular path, and a host
      registration wins.
- [x] Two services sharing one cache share its entries.
- [x] A cache that holds nothing is still correct.
- [x] Misses and cached nulls stay distinct.
- [x] No existing consumer changes any code to keep working.
- [x] Full solution builds and the whole suite passes.
- [x] Multi-instance requirement documented in the package README and the implementation guide.

## Done condition

A consumer can point the claims-path cache at a shared store, the built-in remains the zero-config default,
and the multi-instance hazard is written down where a consumer will meet it.

## Known residual risk

**A host service that does not forward `ITeamCache` silently falls back to the process-local cache.** The
parameter is optional so nothing breaks at compile time, which means substitution can appear configured
while not being in effect. Mitigated by documentation on both bases and in both doc surfaces. The version
that makes it impossible to get wrong is a caching **decorator** over `ITeamService`/`IUserService` — DI
would inject the cache, and no host constructor would be involved. That is the recommended follow-up; it is
a larger refactor than this branch, and the repo already has the decorator pattern to copy
(`CacheInvalidatingUserServiceDecorator`, `AuthorizationTeamServiceDecorator`).

## Out of scope

GitHub issues #175, #176 and #177 — the Eplicta defect sweep, which is its own branch.
