# Feature: fail startup when a custom ITeamCache is not wired through

## Goal

Make the `ITeamCache` forwarding hazard impossible to miss, so a host that registers a shared cache for a
multi-instance deployment cannot silently keep using the process-local one.

## The hazard

3.10.7 shipped `ITeamCache` as an **optional** constructor parameter on `TeamServiceBase` and
`UserServiceBase`. A host service whose own constructor does not forward it compiles, starts, passes its tests
and quietly uses `InMemoryTeamCache.Shared`. The released documentation told consumers to forward it and
admitted **nothing fails if you don't** — so a host that registered Redis would believe it was in effect while
a suspended member kept their team scopes and a disabled user kept their session on every instance that did not
handle the write.

Tier 1 under `mission.md`, and a footgun this repo introduced last release rather than inherited.

## Approach, and why not the decorator I first proposed

I recommended moving the caches into a decorator over `ITeamService`/`IUserService`, which removes the
constructor dependency entirely. Planning it surfaced two problems:

1. **The user cache does not decorate cleanly.** `GetCurrentUserAsync(ClaimsPrincipal = null)` is keyed by
   identity, and with a null argument the identity comes from `GetClaims` — a `protected virtual` on the base
   that a host may override. A decorator caching that read must resolve the caller itself, putting a **second
   copy of "who is the caller"** outside the base where a host override can diverge from it. Two copies of a
   rule is how the `team:read` hole happened.
2. **`shared-instructions.md` prescribes something cheaper for 3.x.** *"Fail loudly when the wiring is
   incomplete. A startup check that names the missing interface turns the next occurrence from an unexplained
   render-time failure into a one-line diagnostic."* And for the analogous case: *"Prefer the startup check for
   3.x and the abstract members for 4.0."*

So: startup check now, the stronger shape at 4.0. Confirmed with the user before any code was written.

## Scope

- `TeamCacheWiring.FindUnwired` in `Tharga.Team` — compares the cache each live service **actually holds**
  against the registered one. Inspecting the outcome rather than constructor signatures means it cannot produce
  a false positive on a host that obtains the cache some other way.
- An `internal ITeamCache CacheInUse` on both bases so the comparison needs no reflection into private state.
  Internal, so it adds no public API.
- `TeamCacheWiringCheck` (`IHostedService`) in `Tharga.Team.Blazor`, registered unconditionally alongside the
  two existing completeness checks.

**It throws, unlike `UserServiceCompletenessCheck`.** That one reports pre-existing gaps a host may never have
noticed, so failing a routine upgrade would be the worse trade. This one can only fire when a host has
**deliberately registered** a custom cache — so firing means they configured something that is not happening,
and what is not happening is authorization freshness. It cannot fire for a host that has not opted in, and for
one that has, booting with the wrong cache is worse than not booting. No opt-out option was added.

## The false-positive trap, and why the built-in is exempt

In a default setup the container's `TryAddSingleton` built-in and the bases' fallback are **two different
`InMemoryTeamCache` instances**, so a naive identity comparison fires on every host that configured nothing —
and this check throws. Both are process-local with no expiry, so not forwarding the built-in changes nothing.
`FindUnwired` therefore returns empty whenever the registered cache is an `InMemoryTeamCache`, and two tests
pin that.

## Acceptance criteria

- [x] A custom cache registered but not forwarded fails startup, naming every offending type and the fix.
- [x] Forwarding the registered cache passes; forwarding a *different* one still fails.
- [x] A default host starts — asserted through a real container, not just the helper.
- [x] No cache registered, a non-toolkit service, and nulls among the services are all ignored.
- [x] The failure message names the type, the constructor fix, and the consequence.
- [x] No public API added; no behaviour change for any existing consumer.
- [x] Full suite passes with no new warnings.

## Done condition

Registering a shared cache and forgetting to forward it stops being a silent authorization defect and becomes a
one-line startup diagnostic.

## Carried forward

**The 4.0 shape:** make the parameter required, or move the caches behind a decorator, so the mistake is
impossible rather than merely loud. Breaking, so it waits for 4.0 — and the user-cache decorator problem above
has to be solved first (most likely by having the base own identity resolution and the decorator cache by an
identity the base hands it).

## Documentation corrected

Both surfaces said *"nothing fails without it"*, which this makes false. `Tharga.Team/README.md` and
`docs/articles/implementation-guide.md` now say the wiring is checked at startup, and that the check cannot
misfire on a host that registered nothing custom.
