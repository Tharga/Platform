# Feature: User lifecycle and host contracts

**Branch:** `feature/user-lifecycle-and-host-contracts` (off `master`)
**Started:** 2026-08-01
**Release:** minor — adds a system scope and public API. **`MAJOR_MINOR` must move to `3.9`.**

## Goal

Four PlutusWave requests that hang together around one theme: **what the toolkit does when a host
extends it, and what it does when a user is removed.** Three of the four are cases where the toolkit
silently drops something — a cache invalidation, a write, a team's last owner.

## Scope

| # | Item | Source | Why here |
|---|---|---|---|
| 1 | `UserServiceBase` stops invalidating its own cache once a host overrides persistence | Request, PlutusWave 2026-08-01 | Priority High. The toolkit dropping a responsibility it owns |
| 2 | Guard every silently-defaulting persistence extension point | Request, PlutusWave 2026-08-01 | Three known occurrences. **PlutusWave asked for this over any individual fix** |
| 3 | Recover a team left with no owner, and stop creating the state silently | Request, PlutusWave 2026-07-31 | Priority Medium-High. No recovery path exists today |
| 4 | `IUserDirectoryService` has no write path — add a display-name update | Request, PlutusWave 2026-07-31 | Priority Medium |

Items 1 and 2 are done together: one change addresses both for `SetUserNameAsync`.

### Deliberately not in scope

- **The template-method refactor of `SetUserNameAsync`.** PlutusWave suggested it and it is the right
  4.0 shape, but making the method non-virtual **breaks every host that overrides it** — PlutusWave
  included. See "Item 1" below for what ships instead.
- **`teams:assign-owner` on any surface other than the Teams tab.** The MCP and REST surfaces stay out
  until plan 07 moves reads onto gated services; adding a second surface now means gating it twice.

## Acceptance criteria

1. **The cache invalidates whoever implemented persistence.** A host overriding `SetUserNameAsync`
   gets correct reads with no extra call. **Non-breaking** — an existing override keeps compiling and
   the now-redundant `InvalidateUserCache` call in it stays harmless.
2. **A host that has not overridden a persistence extension point is told, loudly, at startup.** The
   check must see `protected` members: `SetUserIconReferenceAsync` is `protected virtual`, so an
   interface-map guard cannot see it, and that is precisely the one that cost PlutusWave a day.
   Reports every missing override in one message, not the first.
3. **An ownerless team can be given an owner**, gated on the new `teams:assign-owner` system scope,
   choosing only from that team's existing members, and only when the team currently has no member at
   `AccessLevel.Owner`. Audited, with actor and target.
4. **`RemoveUserFromAllTeamsAsync` reports which teams it left ownerless** rather than returning only a
   count, so callers can warn.
5. **`IUserDirectoryService.SetUserNameAsync`** exists and is **opt-in, defaulting to off** — never an
   automatic side effect of the local rename. A directory write failing must not roll back the local
   write; report it instead.
6. Full suite green. `MAJOR_MINOR` moved to `3.9`.

## Open question to settle before item 3 is coded

**Does assigning an owner require the caller to be a member of the team?** No — that is the point; an
ownerless team's members may all be below Administrator. But it means `teams:assign-owner` is a
cross-tenant grant, so it must be a **system** scope resolved with `TeamScopeGate.HasSystemScope`,
never a bare `HasClaim`, exactly as `teams:delete` is.

## Done condition

All six acceptance criteria met, full suite green, `README.md` and `docs/` updated on both surfaces,
`plan/` removed in the close-out commit, PR open against `master`.
