# Feature: The team comes from the credential, not from a parameter

**Branch:** `feature/rest-team-context` (off `master`)
**Started:** 2026-08-02
**Release:** **`MAJOR_MINOR` stays `3.10`.** Breaking for the audit endpoint's query string, but nothing has
adopted 3.10.x yet, so consumers meet it in the same jump as everything else since 3.8.3.

## The rule

**An external caller never names a team.** It comes from the credential, or — for a system key acting on
behalf of a team — from a header.

| Caller | Team | Reads |
|---|---|---|
| **Team API key** | the key itself; it cannot be anything else | that team |
| **System API key**, no header | nothing to imply | **system audit** — every team, narrowed by filters |
| **System API key** + team header | the header | that team, **if the team consented** |

## Why a parameter is wrong, not merely redundant

**A team key plus a `teamKey` parameter is two sources of truth for one question.** They can disagree, and
an API whose shape invites that is wrong even when the disagreement is refused — which it is:
`TeamKeyConfinementTests` proves a team key naming another team is rejected, invariant **I2**. The check
is right; the parameter should never have been there to need it.

**Scope: the REST and MCP surfaces only.** Service interfaces keep `string teamKey` — 65 methods carry it,
`ScopeProxy` resolves its authorization target from those arguments, and some callers legitimately name a
team they hold no credential for (an oversight holder renaming another team). The surfaces resolve the
team from context and pass it inward; that is the layer the rule belongs to.

## The mechanism that makes it uniform

When a system key presents the team header, resolve the grant and **issue `TeamKey` and `Scope` claims for
that team**. Every existing `[RequireScope]` check then works unchanged — no per-endpoint logic, and a
host's own controllers get it for free. REST and MCP become the same by construction rather than by
agreement, which is the property the audit work spent a whole feature establishing.

## Filter versus authorization

For the system-audit case a **team filter** is fine and wanted — the component already has one. It narrows
data the caller is *already* authorized for. That is a different thing from a parameter that selects what
to authorize against, and conflating the two is what made the first version of `QueryAllAsync` wrong.

**With a header present the team is fixed**, so a conflicting team filter must not silently widen or
narrow past it.

## Open decision — blocking the header path

> *"then that team would have to have given consent for the call to work"*

Consent is `ConsentedRoles` (named roles) plus `ConsentAccessLevel`, and **a system key holds no roles**.
So "has consented" means one of:

- **(i) The team has consent configured at all** — any consenting team admits any system key, at
  `ConsentAccessLevel`. No new concepts, but enabling consent for support staff also admits every system
  key in the installation.
- **(ii) The key matches a consented role** — precise, and needs roles on system keys: a creation-API
  parameter, storage, `SystemApiKeyView`, and role claims.

**Proceeding with (ii)'s behaviour as the conservative default**, since it is what `TeamGrantResolver`
already does: no roles, no match, refused. If (i) is wanted it is a small change in one place. Building the
mechanism either way is not wasted.

## Acceptance criteria

1. `GET api/audit` takes no `teamKey` parameter.
2. A team API key reads its own team, and has no way to ask for another.
3. A system API key with no header reads across all teams, narrowed by filters including team.
4. A system API key with a team header acts on that team, subject to the team's consent.
5. A team header presented by a **team** key is refused, not silently ignored — it is a contradiction,
   and ignoring it would leave the caller believing they asked for something they did not get.
6. The header issues claims, so a host's own controllers are covered without per-endpoint work.
7. REST and MCP use the same header name and the same resolution.
8. Full suite green; no new warnings (baseline 8).

## Done condition

All eight met, docs on both surfaces (the endpoint's shape changed — the guide documents the old query
string), `MAJOR_MINOR` still `3.10`, `plan/` removed in the close-out commit, PR open against `master`.
