# Feature: Audit consent for keys, and one audit gate

**Branch:** `feature/audit-consent-for-keys` (off `master`)
**Started:** 2026-08-02
**Release:** **`MAJOR_MINOR` stays `3.10`** — releases as `3.10.3`.

Stop 8, phase 3, part A. The spec is
`$DOC_ROOT/Tharga/plans/Toolkit/Platform/planned/06-audit-access-verification.md`; phases 1 and 2 are
done and invariant **I2** is already proven. This settles the decision that blocked phase 3, so the full
matrix can be built afterwards with nothing open in it.

## The decision that was blocking C6 — settled 2026-08-02 (user)

> *Does a system API key inherit a team's consented access level, and if so at what level?*

**Yes, at exactly the consented level** — the same rule as a user holding that role, and no higher. A
team that consents `Administrator` to `Support` admits a `Support` key at `Administrator`, which carries
`audit:read`; a team consenting `Viewer` admits nothing, because `audit:read` sits at `Administrator`.

The argument against was real and was weighed: consent is a team owner's decision about **roles**, made
thinking about colleagues rather than machines, so admitting keys widens what that decision meant. It
was rejected in favour of one rule for one grant — a role means the same thing whoever holds it, and a
second rule for keys is a second thing to get wrong.

## A second finding, and it is already on master

**The three surfaces do not share an audit gate, which is invariant I5's whole subject.**

| Surface | Gate today |
|---|---|
| UI | `AuditAccess.CanRead` |
| REST | `AuditAccess.CanRead` — the same function |
| MCP | **`IMcpContext.IsDeveloper`** — a different rule entirely |

`AuditAccess` exists precisely so *"every surface asks the same question of the same code"*, and its own
remarks say the rule restated per surface is the rule that drifts. MCP restates it. A caller holding
system `audit:read` but not the Developer role is refused on MCP and admitted on REST; the reverse is
also true. Neither is what either surface intends.

**This is not caused by the consent decision** — it predates it, and would still be wrong if the answer
above had gone the other way.

## Scope

1. **Consent for key callers.** `AuditAccess` learns to fall back to the team's consented level when the
   caller holds no team scope. Resolution goes through `TeamGrantResolver` — the single copy of that rule,
   which already serves the Blazor claims builder and MCP team selection.
2. **One gate on all three surfaces.** MCP's audit resource moves onto `AuditAccess`.
3. **The consent rows of the matrix**, at every consent level rather than consent/no-consent: I4a, I4b,
   I4c for both C4 and C6.

## The shape problem to solve

`AuditAccess.CanRead` is a **synchronous pure function over claims**, which is why all three surfaces
could share it cheaply. Consent cannot be answered from claims alone — it needs the team, and a lookup.

The target team is a *parameter of the request* on every surface (the REST query, the MCP resource, the
view's selected team), so the lookup is possible; it just cannot be free. The sync overload must survive
for callers that have a team scope, or every surface pays for a database read on a question the claims
already answer.

## Not in scope

**The full 108-cell matrix.** Separate feature, once this leaves nothing open in it.

## Acceptance criteria

1. A system key with a consented role reads that team's audit, at the consented level and no higher.
2. A consent level below `Administrator` grants no audit access — proven at **each** level, not just
   consent/no-consent.
3. No consent, no access — for a key and for a user alike.
4. UI, REST and MCP give the **same answer for the same caller** (I5), asserted by running one set of
   expectations against all three.
5. A caller whose claims already answer the question does not trigger a consent lookup.
6. Full suite green; no new warnings (baseline 8).

## Done condition

All six met, docs on both surfaces, the spec's open-decision section rewritten as decided, `MAJOR_MINOR`
still `3.10`, `plan/` removed in the close-out commit, PR open against `master`.
