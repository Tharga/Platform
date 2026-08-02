# Feature: Select a team on an MCP call

**Branch:** `feature/mcp-team-selection` (off `master`)
**Started:** 2026-08-02
**Release:** **held at `MAJOR_MINOR` 3.10** (user, 2026-08-02) — 3.9/3.10/3.11 all landed without any
consumer adopting one, so the chain describes upgrade events that never happened. This feature and the
registration fix both land as 3.10.x. The pending 3.11.0 release job is left unapproved.

## Goal

MCP has no way to say *which team a call is about*. Everything is derived from the caller's
`TeamClaimTypes.TeamKey` claim, so a call can only ever address the team the caller is already anchored
to — and a **system key is anchored to none**, which is how the toolkit's own system-scope callers end up
seeing an empty team surface.

The consequence is worse than an inconvenience: the consent rule settled at stop 0 — *a team grants
access to holders of a global role at the level the team consented to* — is **unimplementable on MCP**,
because there is no team whose consented level could be resolved. The Blazor surface has done this since
3.2.0. This is parity, not new semantics.

## The mechanism: per call, not per session

`ModelContextProtocol` 2.0.0 is **stateless by default** — there is no session to hold a selection in.
For an HTTP-transported MCP call, per-call and per-request are the same thing, which makes an **HTTP
header** the natural channel: `HttpContextMcpContextAccessor` already builds `IMcpContext` from
`HttpContext` on demand, so the selection arrives where the context is already assembled.

**Rejected: a tool argument.** It would have to be threaded through every `IMcpResourceProvider` and
`IMcpToolProvider` signature, including the host's own, and a provider that forgot it would silently
address the wrong team. The header keeps the selection in one place — the accessor — and leaves every
provider reading `context.TeamId` exactly as it does today.

## What selecting a team must grant

**Exactly what the caller would have had in that team, and never more.** Three cases:

| Caller | Gets |
|---|---|
| A member of the named team | Their membership scopes in it |
| Not a member, but holds a global role the team consented to | The team's consent-level scopes |
| Neither | Nothing — the team is not selectable |

**Intersected with what the caller already holds.** A system API key naming a team must not thereby
acquire scopes beyond its own grant; selection narrows, it never widens. This is the property the whole
feature turns on, and the one worth attacking in review.

## Open decisions

- **Header name.** `X-Team-Key` reads consistently with `Constants.SelectedTeamKeyCookie` on the Blazor
  side. Confirm before wiring, since it becomes consumer-visible surface immediately.
- **Naming a team the caller cannot reach**: silently see nothing, or refuse the call? Leaning **refuse**
  — the caller asked a specific question and an empty answer reads as "the team is empty" rather than
  "you cannot see it". This differs from the no-selection case, where seeing nothing is correct.

## Acceptance criteria

1. A call naming a team the caller belongs to addresses that team, with the caller's membership scopes.
2. A **system key** naming a team addresses it — the case that is impossible today.
3. A caller holding a consented global role addresses a team they are **not** a member of, at the team's
   consented level, and no higher.
4. Selection **never widens**: naming a team grants nothing the caller did not already hold.
5. Naming an unreachable or unknown team is refused, distinguishably from an empty team.
6. A call with no selection behaves exactly as it does today — this is additive.
7. Full suite green; no new warnings.

## Done condition

All seven met, docs on both surfaces (`README.md` + `docs/`), `MAJOR_MINOR` still `3.10`, `plan/` removed
in the close-out commit, PR open against `master`.
