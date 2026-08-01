# Feature: Suspend instead of destroy

**Branch:** `feature/suspend-instead-of-destroy` (off `master`)
**Started:** 2026-08-02
**Release:** minor — new system scope, new state on two entities, new service members.
**`MAJOR_MINOR` → `3.11`.**

## Goal

Today the only way to stop a person or a key being used is to **delete** it. That is far too final for
the ordinary cases: someone on leave, an account suspected of compromise, a partner integration paused, a
contractor between engagements, a key parked while an incident is investigated.

Deleting a user removes them from every team and drops the record. Deleting a key loses its name, scopes,
roles, tags and audit trail. Neither is reversible.

## Scope

| # | Item | Source |
|---|---|---|
| 9 | `teams:manage` — a system scope for cross-team rename and icon | Internal (user, 2026-07-31) |
| 10 | Disable and enable an API key | Internal (user, 2026-07-31) |
| 11 | Disable and enable a user | Internal (user, 2026-07-31) |

**#6, the `LockKeyAsync` doc, is already fixed.** It now states plainly *"This does not disable the key.
A locked key still authenticates — locking only makes the value unrecoverable… To stop a key working,
delete it; there is no disable yet."* That last clause is what this feature makes untrue, so it changes
with the code.

## Why 10 and 11 ship together

Same shape, same decisions, and shipping them apart is how the two drift into different vocabularies for
the same idea. Both need: a state on the entity, a refusal at the authentication or revalidation point,
enable/disable on a service, audit on both directions, and a UI that makes *disabled* visibly distinct
from *expired* or *deleted*.

## The hard part of each

**#9 — the boundary, not the code.** Mirroring `teams:delete` end to end is cheap. The decision is what
the system scope must **not** reach: in-team `team:manage` also covers **consent** and **custom roles**,
and an operator overriding consent is a much larger claim than fixing a typo in a name. Rename and icon
are presentational; consent is authorization. **The system scope covers rename and icon only.**

**#10 — refresh must not re-enable.** Refreshing mints a new secret. A key disabled because it might have
leaked stays disabled until someone explicitly enables it, or the remedy for a compromise silently undoes
the containment.

**#11 — live sessions.** An API key is checked on every request, so disabling one takes effect
immediately. A signed-in user holds a Blazor circuit with claims already issued and keeps working until
something re-evaluates. `TeamClaimRevalidator` and `ClaimRevalidationOptions.Interval` exist for exactly
this class of problem — membership removal, access downgrade, consent revocation — and are the mechanism.

## Naming collision to avoid

`DirectoryUserStatus.Disabled` already exists and means **disabled in Entra** (`accountEnabled == false`,
surfaced by Verify). An application-level disable is a different thing with a different blast radius —
the same local-vs-directory split that already exists for delete. The UI must not let an operator confuse
*blocked from this app* with *blocked from the organization*, and the two states have to be separately
visible.

## Open decisions — settle before coding #11

- **Which scope?** `users:manage` by the rule this codebase already applies: a new scope is warranted when
  an operation is irreversible or crosses a tenant boundary. Disable is neither, and `users:manage`
  already authorizes the strictly more destructive delete.
- **Is a disabled user still visible in the admin lists and team grids, or filtered out?** Visible,
  presumably — an invisible disabled user is hard to re-enable.
- **Does disabling a user cascade to that user's API keys?**

## Acceptance criteria

1. A disabled API key is refused at authentication, and the refusal is **recorded as an auth failure** so
   the attempt is visible in the audit log rather than silently rejected.
2. **Refreshing a disabled key does not enable it.**
3. A disabled user is evicted within the existing `ClaimRevalidation` window, not left running on issued
   claims.
4. **Disabled is visibly distinct from expired** for a key, and from directory-disabled for a user. Both
   record *when* and *by whom*.
5. `teams:manage` authorizes cross-team rename and icon — **and nothing else**. Consent and custom roles
   still require in-team `team:manage`.
6. Both directions of both operations are audited with the actor.
7. Full suite green; no new warnings.

## Done condition

All seven criteria met, docs on both surfaces, `MAJOR_MINOR` at `3.11`, `plan/` removed in the close-out
commit, PR open against `master`.
